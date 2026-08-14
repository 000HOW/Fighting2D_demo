using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 奖励发放器 —— 挂在玩家身上，是项目中唯一负责"发放奖励"的组件。
/// 订阅对话结束 / 实体死亡事件，过滤后把技能放入背包、把 Buff 加入修改器，
/// 需要时广播 RewardGrantedEvent 供 UI 反馈。
/// </summary>
[RequireComponent(typeof(SkillSender), typeof(CharacterControler))]
public class RewardDispatcher : MonoBehaviour
{
    SkillSender skillSender;
    CharacterControler character;
    // 已拥有技能去重（一次性奖励），顺带修掉 SkillAddInBag 无脑重复
    readonly HashSet<PlayerSkillData> owned = new();

    void Awake()
    {
        skillSender = GetComponent<SkillSender>();
        character = GetComponent<CharacterControler>();
    }

    void OnEnable()
    {
        EventBus.Global.Subscribe<DialogueNodeReachedEvent>(OnDialogueNodeReached);
        EventBus.Global.Subscribe<EntityDiedEvent>(OnEntityDied);
    }
    void OnDisable()   // 必须与 OnEnable 同一委托实例对称注销
    {
        EventBus.Global.Unsubscribe<DialogueNodeReachedEvent>(OnDialogueNodeReached);
        EventBus.Global.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    void OnDialogueNodeReached(DialogueNodeReachedEvent e)
    {
        if (e.node == null || e.node.reward == null || e.node.reward.IsEmpty) return;
        Grant(e.node.reward, "Dialogue:" + e.node.speakerName);
    }

    void OnEntityDied(EntityDiedEvent e)
    {
        if (e.killer != gameObject) return;                       // 只认玩家击杀
        if (!e.entity.TryGetComponent<RewardDrop>(out var drop)) return;
        Grant(drop.reward, "Kill:" + e.entity.name);
    }

    /// <summary>
    /// 统一发放入口：技能入包（去重），Buff 进修改器（自动触发 ModifierAddEvent 刷 UI）。
    /// </summary>
    public void Grant(RewardData reward, string source = "")
    {
        if (reward == null) return;
        bool changed = false;
        if (reward.skills != null)
        {
            foreach (var s in reward.skills)
            {
                if (s != null && owned.Add(s))
                {
                    skillSender.SkillAddInBag(s);
                    changed = true;
                }
            }
        }
        if (reward.modifiers != null)
        {
            foreach (var m in reward.modifiers)
            {
                if (m != null)
                {
                    character.AddModifier(m);
                    changed = true;
                }
            }
        }
        if (changed)
        {
            EventBus.Global.Fire(new RewardGrantedEvent(reward, source));
        }
    }
}
