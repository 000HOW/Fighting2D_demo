using UnityEngine;

/// <summary>
/// Boss 飞行冲刺冲撞技能槽：玩家在拉近距离内触发（Phase>=1，Boss 专属）。
/// 释放后进入 DashChargeStateData（起手锁定玩家位置→固定直冲→Custom 态攻击盒扫描）。
/// 建议资产：priority 高于火球（近距优先冲撞）、cooldown 较长、noRepeat=true。
/// </summary>
[CreateAssetMenu(fileName = "DashChargeSkillSlot", menuName = "Enemy/DashChargeSkillSlot")]
public class DashChargeSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("玩家在该距离内触发冲刺冲撞")] public float useRange = 3.5f;

    public override bool CanUse(in AISkillContext ctx)
    {
        if (ctx.brain is not BossBrain b || b.Phase < 1) return false;   // Boss 专属 + 阶段门
        return ctx.hasTarget && ctx.distToTarget <= useRange;
    }
}
