using UnityEngine;

/// <summary>
/// Boss 大型火球（Custom 技能状态）：复用 FireballStateData 全部逻辑，
/// 资产上配置独立的火球预制体（大号）+ 更高伤害/更远射程即可，无需额外代码。
/// Phase 2 由 BigFireballSkillSlot 解锁。
/// </summary>
[CreateAssetMenu(fileName = "BigFireballStateData", menuName = "Enemy/BigFireballStateData")]
public class BigFireballStateData : FireballStateData
{
    // 全部逻辑继承自 FireballStateData（发射/收招/定身/回池）
}
