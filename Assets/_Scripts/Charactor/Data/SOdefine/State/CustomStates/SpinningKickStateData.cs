using UnityEngine;

/// <summary>
/// 玩家技能 - 旋风踢（Custom 态，实现 IAttackBoxScanEnabled）：
///  1. 释放后进入持续攻击状态（kickDuration 秒），全程开启攻击盒
///  2. 每 hitInterval 秒清空一次本招命中去重集，使攻击盒可对同一敌人多段命中（持续攻击）
///  3. 持续期间位移随方向键（A/D）：按左/右朝对应方向推进，无输入则原地制动
///  4. 到时自动收招回 Idle
/// 物理：UpdatePhysics 只写 bb.readytoApply 期望速度，由 MotionActuator 统一应用。
/// 垂直速度保留当前值（继续受重力），在地面释放时贴地旋转。
/// </summary>
[CreateAssetMenu(fileName = "SpinningKickStateData", menuName = "PlayerControler/CustomStateData/SpinningKickStateData")]
public class SpinningKickStateData : BaseCustomStateData, IAttackBoxScanEnabled
{
    [Header("旋风踢攻击盒")]
    [Tooltip("旋风踢攻击盒（窗口建议 start=0/end=1 覆盖全程）")] public AttackBox kickAttackBox;
    [Tooltip("每段命中伤害")] public float kickDamage = 12f;
    [Tooltip("伤害类型")] public DamageType kickDamageType = DamageType.Tap;

    [Header("持续攻击")]
    [Tooltip("持续攻击总时长（秒）")] public float kickDuration = 1.5f;
    [Tooltip("多段伤害间隔（秒）：每段清空命中去重集，允许再次命中同一敌人")] public float hitInterval = 0.4f;

    [Header("位移")]
    [Tooltip("按方向键时的推进速度（米/秒）")] public float moveSpeed = 5f;

    // 运行时累计（玩家技能资产不共享；进入状态时重置）
    [System.NonSerialized] float elapsed;
    [System.NonSerialized] float lastHitReset;

    /// <summary>接口实现：全程开启攻击盒扫描</summary>
    public bool attackScanEnabled => true;

    public override void OnEntryStart(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        rt.customStateTimer = 0f;
        elapsed = 0f;
        lastHitReset = 0f;

        // 攻击盒/伤害写入运行时，供 EnvironmentSensor 在 Custom 态扫描使用
        rt.curAttackBox = kickAttackBox;
        rt.curAttack = new AttackData { damageType = kickDamageType, baseValue = kickDamage };
    }

    public override void OnExitStart(Blackboard bb) { }
    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        elapsed += Time.fixedDeltaTime;

        // 多段伤害：每 hitInterval 清空一次本招命中去重集，使攻击盒可再次命中同一敌人
        if (elapsed - lastHitReset >= hitInterval)
        {
            lastHitReset = elapsed;
            bb.readytoApply.Enemies.Clear();
        }

        // 到时收招回 Idle
        if (elapsed >= kickDuration)
            Drop(bb);
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        var rt = bb.characterRunTimeData;
        if (rt.self == null) return;

        var input = bb.inputData.cur_inputComand;
        float dir = 0f;
        if (input.inLeft) dir = -1f;
        if (input.inRight) dir = 1f;

        // 位移随方向键；无输入则制动到 0
        bb.readytoApply.exp_horizontalVelocity = dir != 0f
            ? dir * moveSpeed
            : Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);

        // 垂直保留当前值（继续受重力），在地面释放时贴地旋转
        bb.readytoApply.exp_VerticalVelocity = rt.verticalVelocity;
    }

    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }
}
