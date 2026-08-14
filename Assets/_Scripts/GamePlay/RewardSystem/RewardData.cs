using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 奖励数据 —— 可序列化，内嵌在对话结点 / 敌人 RewardDrop 上，由 RewardDispatcher 统一发放。
/// </summary>
[System.Serializable]
public class RewardData
{
    public List<PlayerSkillData> skills = new();
    public List<ModifierData> modifiers = new();
    public bool IsEmpty => (skills == null || skills.Count == 0) &&
                           (modifiers == null || modifiers.Count == 0);
}

/// <summary>
/// 任意实体死亡事件（由 DamageReceiver 广播）
/// </summary>
public struct EntityDiedEvent
{
    public readonly GameObject entity;   // 死亡者
    public readonly GameObject killer;   // 凶手（可为 null）
    public EntityDiedEvent(GameObject _entity, GameObject _killer)
    {
        entity = _entity;
        killer = _killer;
    }
}

/// <summary>
/// 对话到达某结点事件（由 Dialogue 到达带奖励的结点时广播）
/// </summary>
public struct DialogueNodeReachedEvent
{
    public readonly DialogueGraph graph;
    public readonly DialogueNode node;   // 到达的结点（内含 reward）
    public DialogueNodeReachedEvent(DialogueGraph _graph, DialogueNode _node)
    {
        graph = _graph;
        node = _node;
    }
}

/// <summary>
/// 奖励已发放事件（供 UI 弹"获得技能 XX"提示）
/// </summary>
public struct RewardGrantedEvent
{
    public readonly RewardData reward;
    public readonly string source;       // "Dialogue" / "Kill:Boss01"
    public RewardGrantedEvent(RewardData _reward, string _source)
    {
        reward = _reward;
        source = _source;
    }
}
