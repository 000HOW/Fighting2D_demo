using UnityEngine;

/// <summary>
/// Boss 被击晕技能槽：HP 落在 [stunMinHp, stunMaxHp] 区间时触发（Boss 专属）。
/// 释放后进入 BossStunStateData（眩晕动画 + 定身，超区间/超时退出）。
/// 建议资产：noRepeat=true + 较长冷却（防连续眩晕）。
/// </summary>
[CreateAssetMenu(fileName = "BossStunSkillSlot", menuName = "Enemy/BossStunSkillSlot")]
public class BossStunSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("触发眩晕的 HP 区间下界")] public float stunMinHp = 0f;
    [Tooltip("触发眩晕的 HP 区间上界")] public float stunMaxHp = 0.3f;

    public override bool CanUse(in AISkillContext ctx)
    {
        if (ctx.brain is not BossBrain b || b.Phase < 1) return false;   // Boss 专属 + 阶段门
        return ctx.hpRatio >= stunMinHp && ctx.hpRatio <= stunMaxHp;
    }
}
