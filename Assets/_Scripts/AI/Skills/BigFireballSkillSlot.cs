using UnityEngine;

/// <summary>
/// Boss 大型火球技能槽：远距时施放（Phase>=2 解锁，Boss 专属）。
/// 释放后进入 BigFireballStateData（发射大型火球实体，独立预制体/更高伤害）。
/// </summary>
[CreateAssetMenu(fileName = "BigFireballSkillSlot", menuName = "Enemy/BigFireballSkillSlot")]
public class BigFireballSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("施放最小距离（远距）")] public float useMinRange = 4f;

    public override bool CanUse(in AISkillContext ctx)
    {
        if (ctx.brain is not BossBrain b || b.Phase < 2) return false;   // Phase 2 解锁
        return ctx.hasTarget && ctx.distToTarget >= useMinRange;
    }
}
