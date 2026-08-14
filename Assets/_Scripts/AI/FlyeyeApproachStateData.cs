using UnityEngine;

/// <summary>
/// Flyeye（飞行敌人）的"接近/移动"自定义状态（作为技能配置进 AIEnemyProfileSO.skills）。
/// 职责只有移动，绝不直接请求攻击：
///   - 移动目标由 AI 决策层（AI_EnomyBrain.GetMoveGoal）给出，优先级沿用通用决策：回家 > 追敌 > 巡逻 > 悬停
///   - 朝目标直线飞行（AI 的 MoveTo 只有水平分量，这里补上垂直分量）；无目标则原地悬停
///   - 贴近玩家到 playerStopDistance 后结束技能回到 Idle，攻击走通用 AI 的 inAattack → AttackManager 通道
///     （攻击盒由 AttackManager 从 CharacterSO.specialAttacks 读取，本状态不持有任何攻击配置）
/// 无 SO 运行时字段：施放计时存在 AISkillRuntime.castTimer（AI 决策层），本 SO 资产无状态、可安全共享。
/// </summary>
[CreateAssetMenu(fileName = "FlyeyeApproachStateData", menuName = "Enemy/FlyeyeApproachStateData")]
public class FlyeyeApproachStateData : BaseCustomStateData
{
    [Header("接近飞行")]
    [Tooltip("朝目标飞行的速度")] public float moveSpeed = 5f;
    [Tooltip("贴近玩家到此距离（米）即结束技能，交由通用 AI 出手；应 ≤ AIEnemyProfile.attackStopDistance")] public float playerStopDistance = 0.5f;
    [Tooltip("距任意移动目标小于此距离即制动停住（防超调；建议 ≈ patrolArriveDistance）")] public float arriveDistance = 0.3f;

    // ===== 无 SO 运行时字段（数据都在 AI 决策层） =====

    static AI_EnomyBrain GetBrain(Blackboard bb)
        => bb.characterRunTimeData.self != null
            ? bb.characterRunTimeData.self.GetComponent<AI_EnomyBrain>() : null;

    public override void OnEntryStart(Blackboard bb)
    {
        // 施放计时清零（记录在本技能行的 AISkillRuntime 上）
        var cur = GetBrain(bb)?.CurrentCast;
        if (cur != null) cur.castTimer = 0f;
    }

    public override void OnExitStart(Blackboard bb) { }
    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        var brain = GetBrain(bb);
        if (brain == null) return;

        // 仅当"当前移动目标就是玩家"且已贴到出手距离 → 结束技能回到 Idle，
        // 让通用 AI 决策层走 inAattack → AttackManager 出手（攻击盒由 AttackManager 从 specialAttacks 读取）。
        // 回家/巡逻等其它目标不在此结束，继续由本状态飞行执行。
        Transform target = brain.Target;
        Vector2? goal = brain.GetMoveGoal();
        bool goalIsPlayer = target != null && goal != null
            && ((Vector2)target.position - goal.Value).sqrMagnitude < 0.01f;

        if (goalIsPlayer && Vector2.Distance(rt.self.transform.position, target.position) <= playerStopDistance)
        {
            rt.self.GetComponent<CharacterControler>()
                .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData,
                    ignoreCancelTime: true);
        }
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        var rt = bb.characterRunTimeData;
        if (rt.self == null) return;

        var brain = GetBrain(bb);
        Transform selfT = rt.self.transform;
        Vector2? goal = brain != null ? brain.GetMoveGoal() : null;

        if (goal == null)
        {
            // 无可追目标（无玩家且无巡逻点 / 决策层 Idle）：原地悬停（X/Y 制动，保持当前高度）
            bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
            bb.readytoApply.exp_VerticalVelocity = Mathf.MoveTowards(rt.verticalVelocity, 0f, 20f * deltaTime);
            return;
        }

        Vector2 to = goal.Value - (Vector2)selfT.position;

        // 面向目标（保证后续通用攻击的冲刺方向）
        if (Mathf.Abs(to.x) > 0.01f) rt.facingDir = to.x > 0f ? 1 : -1;

        if (to.magnitude <= arriveDistance)
        {
            // 已到目标近旁：收速制动（防超调；巡逻到点由 AI 的 PatrolStep 判到达并推进路点）
            bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
            bb.readytoApply.exp_VerticalVelocity = Mathf.MoveTowards(rt.verticalVelocity, 0f, 20f * deltaTime);
        }
        else
        {
            // 未到位：朝目标直线飞行（X/Y 全自由，补上 AI 水平移动做不到的垂直分量）
            Vector2 dir = to.normalized;
            bb.readytoApply.exp_horizontalVelocity = dir.x * moveSpeed;
            bb.readytoApply.exp_VerticalVelocity = dir.y * moveSpeed;
        }
    }
}
