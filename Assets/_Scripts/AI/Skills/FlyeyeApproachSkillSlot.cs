using UnityEngine;

/// <summary>
/// 飞行接近技能槽：两类触发场景，均可进入 customStateData（FlyeyeApproachStateData）→ 朝目标/路点直线飞行：
///   1. 追敌/攻击玩家：目标距离大于 approachRange 时接近飞行（过近直接交由通用 AI 的普攻通道，不再重复接近）。
///   2. 巡逻：飞往当前巡逻路点（空中巡逻目标；配合用户配置 gravityScale=0 + 空中巡逻点实现飞行巡航）。
/// （技能已全局化、任意状态最高优先级施放，故必须用 IsApproachingPlayer / IsPatrolling 限定触发场景：
///   否则无目标/回家时 distToTarget=MaxValue 或目标非玩家会误触发，且接近态永不结束卡死。
///   巡逻场景安全：Patrol 仅在配置了巡逻点时才进入，GetMoveGoal 必有非空路点，技能飞行到位后由 PatrolStep 推进下一路点。）
/// ⚠️ 无目标巡逻触发须在资产上关闭 requireTarget（否则 SelectSkill 的 requireTarget 门会跳过）。
/// </summary>
[CreateAssetMenu(fileName = "FlyeyeApproachSkillSlot", menuName = "Enemy/AISkillSlot/FlyeyeApproachSkillSlot")]
public class FlyeyeApproachSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("目标距离大于该值才接近（过近直接普攻）")] public float approachRange = 2f;

    public override bool CanUse(in AISkillContext ctx)
    {
        // 追敌/攻击玩家：距离够远才接近（过近直接普攻）
        if (ctx.brain.IsApproachingPlayer() && ctx.hasTarget && ctx.distToTarget > approachRange)
            return true;
        // 巡逻：飞往当前巡逻路点（空中/地面均可；不要求目标）
        if (ctx.brain.IsPatrolling())
            return true;
        return false;
    }
}
