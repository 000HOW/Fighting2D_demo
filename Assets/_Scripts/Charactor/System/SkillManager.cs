using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class SkillManager
{
    /// <summary>待释放的自定义状态技能（BaseCustomStateData 即技能核心：行为 + animData 动画/打断数据）</summary>
    BaseCustomStateData cur_SkillState;
    StateTransitionArbiter arbiter;
    Blackboard blackboard;
    public SkillManager(Blackboard _blackboard, StateTransitionArbiter _arbiter)
    {
        blackboard = _blackboard;
        arbiter = _arbiter;
    }

    /// <summary>
    /// 是否有待释放技能：UseSkill 入队后（cur_SkillState 非空）、真正切到 Custom 态前为 true；
    /// 申请成功清空后自动 false。供 AI 决策层防止"跨帧重复选中同一技能"（异步入队期间未进 Custom 态）。
    /// </summary>
    public bool HasPendingSkill => cur_SkillState != null;
    public void OnUpdate()
    {
        if (blackboard.characterRunTimeData.isDead) return;   // 死亡后不可释放技能
        if (cur_SkillState==null) return;
        {
            BaseCustomStateData skillState = cur_SkillState;
            if (skillState == null || skillState.animData == null)
            {
                Debug.LogError($"技能 {cur_SkillState.name} 缺少 animData 配置，已丢弃本次释放");
                cur_SkillState = null;
                return;
            }

            // Debug.Log($"use skill: {cur_SkillState.name}");
            // 只有申请成功才清空待释放技能；失败则保留，下一帧重试，避免"技能被吞"
            if (arbiter.Request(StateType.Custom, skillState.animData, SwitchStateType.Refresh,
                    ignoreCancelTime: skillState.animData.ignoreCancelTime, customData: skillState))
            {
                cur_SkillState = null;
            }
        }
    }

    public void UseSkill(BaseCustomStateData skillState)
    {
        cur_SkillState = skillState;
    }

    /// <summary>
    /// 唯一可取消判断权：角色当前是否允许释放指定技能。
    /// 技能自身勾了 ignoreCancelTime → 视为强制打断（同受击），不受当前状态 canceltime 限制
    /// </summary>
    public bool CanUseSkill(BaseCustomStateData skillState)
    {
        if (blackboard.characterRunTimeData.isDead) return false;   // 死亡后不可释放技能（必须挡，否则 ignoreCancelTime 技能会漏）
        if (skillState != null && skillState.animData != null && skillState.animData.ignoreCancelTime)
            return true;
        return arbiter.CanCancelCurrentState();
    }

}

public interface ISkill
{
    public void UseSkill(BaseCustomStateData skillState);

    /// <summary>当前角色是否可释放指定技能（可取消判断权唯一在此处）</summary>
    public bool CanUseSkill(BaseCustomStateData skillState);
}