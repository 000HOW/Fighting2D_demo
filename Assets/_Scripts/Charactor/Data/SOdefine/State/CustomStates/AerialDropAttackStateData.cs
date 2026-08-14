using UnityEngine;

/// <summary>
/// 玩家技能 - 下落攻击（Custom 态，实现 IAttackBoxScanEnabled）：
///  1. 释放后垂直起跳（跳起逻辑在 OnEntry / Entry 段，对应 startClip 跳起动画，不开启攻击盒）
///  2. 跳起后按攻击键(J) → 立即快速下落并开启攻击盒，对敌人造成伤害（下落逻辑在 OnUpdate / Main 段，对应 mainClip 下落动画）
///  3. 滞空等待超过 waitTimeout 仍未按攻击键 → 自动触发下落攻击
///  4. 落地 → 自动收招回 Idle
/// 物理：UpdatePhysics 只写 bb.readytoApply 期望速度，由 MotionActuator 统一应用。
/// 运行时阶段用资产内最小 [NonSerialized] 标志 + 已有 rt.customStateTimer（玩家技能资产不共享，安全）。
/// 注：跳起(上升)时长 = animData.startMotionTime（Entry 段时长，即 startClip 跳起动画长度）；
///     滞空等待窗口 = waitTimeout（进入 Main 段开始计时）；攻击盒扫描走 EnvironmentSensor 的
///     IAttackBoxScanEnabled 放宽门，不依赖动画窗口。
/// </summary>
[CreateAssetMenu(fileName = "AerialDropAttackStateData", menuName = "PlayerControler/CustomStateData/AerialDropAttackStateData")]
public class AerialDropAttackStateData : BaseCustomStateData, IAttackBoxScanEnabled
{
    [Header("下落攻击盒")]
    [Tooltip("下落攻击盒（窗口建议 start=0/end=1 覆盖全程下落）")] public AttackBox dropAttackBox;
    [Tooltip("下落命中伤害")] public float dropDamage = 15f;
    [Tooltip("下落伤害类型")] public DamageType dropDamageType = DamageType.Tap;

    [Header("跳起阶段（Entry 段 / startClip 跳起动画）")]
    [Tooltip("跳起初速度（米/秒）；跳起时长由 animData.startMotionTime 控制（Entry 段时长）")] public float riseSpeed = 8f;

    [Header("滞空等待 / 自动下落")]
    [Tooltip("滞空等待攻击输入的超时（秒），超时未按攻击键自动下落")] public float waitTimeout = 0.8f;
    [Tooltip("下落阶段垂直速度（米/秒，负值向下）")] public float diveSpeed = -12f;

    // 运行时阶段标志：是否已进入下落攻击（玩家技能资产不共享；进入状态时重置）
    [System.NonSerialized] bool diving;

    /// <summary>接口实现：仅下落阶段开启攻击盒扫描（上升/滞空阶段不伤敌）</summary>
    public bool attackScanEnabled => diving;

    public override void OnEntryStart(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        rt.customStateTimer = 0f;
        diving = false;

        // 攻击盒/伤害写入运行时，供 EnvironmentSensor 在 Custom 态扫描使用
        rt.curAttackBox = dropAttackBox;
        rt.curAttack = new AttackData { damageType = dropDamageType, baseValue = dropDamage };
    }

    public override void OnExitStart(Blackboard bb) { }

    /// <summary>
    /// 跳起阶段（Entry 段，对应 startClip 跳起动画）：
    /// 期间按攻击键(J) → 立即准备下落（进入 Main 段后马上转下落攻击）。
    /// 上升时长由 animData.startMotionTime 控制（Entry 段时长），不在此逐帧计时。
    /// </summary>
    public override void OnEntry(Blackboard bb)
    {
        if (ReadAttackInput(bb)) diving = true;
    }

    public override void OnExit(Blackboard bb) { }

    /// <summary>
    /// 下落阶段（Main 段，对应 mainClip 下落动画）：
    ///  - 已进入下落：落地即收招回 Idle
    ///  - 未进入下落（滞空等待）：按攻击键 → 下落；等待超过 waitTimeout → 自动下落
    /// </summary>
    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        if (diving)
        {
            if (rt.isground)
                Drop(bb);
            return;
        }

        // 滞空等待：按攻击键立即下落（消费命令，避免残留影响后续普攻）
        if (ReadAttackInput(bb))
        {
            diving = true;
            return;
        }

        // 滞空等待超时未按攻击键 → 自动触发下落攻击
        if (rt.customStateTimer >= waitTimeout)
        {
            diving = true;
            return;
        }

        rt.customStateTimer += Time.fixedDeltaTime;
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        var rt = bb.characterRunTimeData;
        if (rt.self == null) return;

        // 已进入下落：立即快速下落（优先于跳起阶段，响应"按攻击键立马下落"）
        if (diving)
        {
            bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
            bb.readytoApply.exp_VerticalVelocity = diveSpeed;
            return;
        }

        // 跳起阶段（Entry 段 / startClip 跳起动画）：向上推进
        if (rt.stateProgress == StateProgress.Entry)
        {
            bb.readytoApply.exp_VerticalVelocity = riseSpeed;
            return;
        }

        // Main 段滞空等待：双轴制动（轻微滞空）
        bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
        bb.readytoApply.exp_VerticalVelocity = Mathf.MoveTowards(rt.verticalVelocity, 0f, 20f * deltaTime);
    }

    /// <summary>
    /// 检测"攻击指令"：优先消费输入缓冲队头（按下瞬间入队），兜底当前帧攻击键状态（按住不放）
    /// </summary>
    bool ReadAttackInput(Blackboard bb)
    {
        if (bb.inputData.ReadBufferComand(0, out ECommand cmd) && cmd.eCommand == ECommandType.Attack)
        {
            bb.inputData.UseBufferCommand(1);
            return true;
        }
        return bb.inputData.cur_inputComand.inAattack;
    }

    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }
}
