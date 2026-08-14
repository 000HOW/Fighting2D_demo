using UnityEngine;

/// <summary>
/// 掉落奖励 —— 挂在敌人预制体上，作为"特定敌人身上的奖励配置位"。
/// 该敌人被玩家击杀后，RewardDispatcher 会读取此处的 RewardData 并发放。
/// </summary>
public class RewardDrop : MonoBehaviour
{
    public RewardData reward;
}
