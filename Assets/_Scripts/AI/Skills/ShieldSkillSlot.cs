using UnityEngine;

/// <summary>
/// 盾牌技能槽：目标进入范围且（可选）在正前方时触发举盾。
/// 释放后进入 customStateData（ShieldStateData）→ 举盾减伤；
/// 举盾期间的"收手"（目标脱离 / 超时进入攻击窗口）与收手重计冷却由 ShieldStateData 行为自管。
/// </summary>
[CreateAssetMenu(fileName = "ShieldSkillSlot", menuName = "Enemy/AISkillSlot/ShieldSkillSlot")]
public class ShieldSkillSlot : AISkillSlot
{
    [Header("触发条件")]
    [Tooltip("目标进入该距离才触发")] public float useRange = 5f;
    [Tooltip("额外要求目标在正前方")] public bool requireFacing = true;

    public override bool CanUse(in AISkillContext ctx)
        => ctx.distToTarget <= useRange && (!requireFacing || ctx.isTargetInFront);
}
