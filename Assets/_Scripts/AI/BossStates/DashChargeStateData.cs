using UnityEngine;

/// <summary>
/// Boss 飞行冲刺冲撞（Custom 技能，实现 IAttackBoxScanEnabled）：
///  - 起手锁定玩家位置（存入 brain.CurrentCast.castTarget），随后朝锁定点高速直飞（不追踪）
///  - OnEntryStart 写入 curAttackBox/curAttack → EnvironmentSensor 在 Custom 态持续扫描攻击盒
///    （Enemies 去重 = 同一冲刺每目标只结算一次；进入状态时 Enemies.Clear 重置）
///  - 到位 / 超时 / 目标死亡 → 回 Idle 收招
/// 无 SO 运行时字段：施放计时/锁定点走 brain.CurrentCast（AISkillRuntime）。
/// </summary>
[CreateAssetMenu(fileName = "DashChargeStateData", menuName = "Enemy/DashChargeStateData")]
public class DashChargeStateData : BaseCustomStateData, IAttackBoxScanEnabled
{
    [Header("冲刺攻击盒")]
    [Tooltip("冲刺攻击盒（窗口建议 start=0 / end=1 覆盖全程直冲）")] public AttackBox chargeAttackBox;
    [Tooltip("冲刺命中伤害")] public float chargeDamage = 20f;
    [Tooltip("冲刺伤害类型")] public DamageType chargeDamageType = DamageType.Tap;
    [Tooltip("是否允许 Custom 态扫描攻击盒（冲刺全程 true）")] [SerializeField] private bool attackScan = true;
    /// <summary>接口实现：是否允许扫描攻击盒（Inspector 可配 attackScan 开关）</summary>
    public bool attackScanEnabled => attackScan;

    [Header("冲刺运动")]
    [Tooltip("冲刺速度（米/秒）")] public float chargeSpeed = 8f;
    [Tooltip("距锁定点到位的判定距离（收招）")] public float arriveDistance = 0.4f;
    [Tooltip("冲刺超时（秒，兜底收招）")] public float chargeTimeout = 1.2f;

    public override void OnEntryStart(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        var brain = GetBrain(bb);
        var cur = brain?.CurrentCast;

        if (cur != null)
        {
            cur.castTimer = 0f;
            // 起手锁定玩家位置（固定直冲，不追踪）
            cur.castTarget = brain.Target != null && rt.self != null
                ? (Vector2)brain.Target.position
                : (Vector2)rt.self.transform.position;
        }

        // 冲刺攻击盒/伤害在 Custom 态由 EnvironmentSensor 扫描使用（AttackCheck 每帧从运行时刷新）
        rt.curAttackBox = chargeAttackBox;
        rt.curAttack = new AttackData { damageType = chargeDamageType, baseValue = chargeDamage };
    }

    public override void OnExitStart(Blackboard bb) { }
    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        var brain = GetBrain(bb);
        var cur = brain?.CurrentCast;
        if (brain == null || cur == null) return;

        // 结束条件：目标死亡 / 到位 / 超时
        bool targetGone = brain.Target == null;
        bool arrived = Vector2.Distance(rt.self.transform.position, cur.castTarget) <= arriveDistance;
        bool timedOut = cur.castTimer >= chargeTimeout;

        if (targetGone || arrived || timedOut)
        {
            Drop(bb);
            return;
        }
        cur.castTimer += Time.fixedDeltaTime;
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        var rt = bb.characterRunTimeData;
        if (rt.self == null) return;

        var cur = GetBrain(bb)?.CurrentCast;
        if (cur == null) return;

        Vector2 to = cur.castTarget - (Vector2)rt.self.transform.position;

        // 面向冲刺方向（保证后续受击/攻击方向正确）
        if (Mathf.Abs(to.x) > 0.01f) rt.facingDir = to.x > 0f ? 1 : -1;

        if (to.magnitude <= arriveDistance)
        {
            // 到位：收速制动（OnUpdate 负责收招回 Idle）
            bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
            bb.readytoApply.exp_VerticalVelocity = Mathf.MoveTowards(rt.verticalVelocity, 0f, 20f * deltaTime);
            return;
        }

        // 朝锁定点直线飞（X/Y 全自由，直冲不受重力影响）
        Vector2 dir = to.normalized;
        bb.readytoApply.exp_horizontalVelocity = dir.x * chargeSpeed;
        bb.readytoApply.exp_VerticalVelocity = dir.y * chargeSpeed;
    }

    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }

    static AI_EnomyBrain GetBrain(Blackboard bb)
        => bb.characterRunTimeData.self != null
            ? bb.characterRunTimeData.self.GetComponent<AI_EnomyBrain>() : null;
}
