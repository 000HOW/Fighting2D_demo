using UnityEngine;

/// <summary>
/// Boss 普通火球技能槽：中距 + 正面（可选）时施放（Phase>=1，Boss 专属）。
/// 释放后进入 FireballStateData（发射火球实体）。
/// CanUse 纯净约束：只读配置 + ctx（SO 被共享，勿写运行时状态）。
/// </summary>
[CreateAssetMenu(fileName = "FireballSkillSlot", menuName = "Enemy/FireballSkillSlot")]
public class FireballSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("施放最小距离（太近不用，留给冲刺/近身）")] public float useMinRange = 1.5f;
    [Tooltip("施放最大距离")] public float useMaxRange = 6f;
    [Tooltip("要求玩家在正前方")] public bool requireFacing = true;

    public override bool CanUse(in AISkillContext ctx)
    {
        if (ctx.brain is not BossBrain b || b.Phase < 1) return false;   // Boss 专属 + 阶段门
        return ctx.hasTarget
            && ctx.distToTarget >= useMinRange && ctx.distToTarget <= useMaxRange
            && (!requireFacing || ctx.isTargetInFront);
    }
}
