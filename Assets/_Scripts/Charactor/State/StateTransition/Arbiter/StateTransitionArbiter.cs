using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态转换仲裁器
/// 每帧收集所有转换请求，帧末按优先级执行最高者
/// </summary>
public class StateTransitionArbiter
{
    struct TransitionRequest
    {
        public StateType targetType;
        public StateData data;
        public BaseCustomStateData customData;
        public int priority;
        public SwitchStateType switchType;
        public bool checkSame;
    }

    List<TransitionRequest> requests = new();
    Statemachine statemachine;
    Blackboard blackboard;

    public StateTransitionArbiter(Statemachine sm, Blackboard bb)
    {
        statemachine = sm;
        blackboard = bb;
    }

    // —— 公开 API：任何系统调用这个方法来请求转换 ——

    /// <summary>
    /// 申请状态转换
    /// </summary>
    /// <param name="ignoreCancelTime">为true时无视当前状态的取消时间(canceltime)，
    /// 用于受击、技能等强制转换；为false时需当前状态已过 canceltime 才允许申请</param>
    /// <param name="customData">目标为 Custom 时的行为资产（技能用；纯数据/非自定义传 null）</param>
    /// <returns>是否申请成功。未到当前状态可取消时间时返回false，且不会加入待执行队列</returns>
    public bool Request(StateType type, StateData data,SwitchStateType switchType = SwitchStateType.Normal,bool checkSame = false,bool ignoreCancelTime = false, BaseCustomStateData customData = null)
    {
        // 已死亡：只放行「进入死亡」本身，其余一律拒绝（死亡是终态，不可被任何状态顶掉）
        if (blackboard.characterRunTimeData.isDead && type != StateType.Death)
            return false;

        // 统一在此处做「可取消时间」判断，申请失败则不入队
        if (!ignoreCancelTime && !CanCancelCurrentState())
        {
            // Debug.Log($"Request reject: {type} 未到当前状态可取消时间");
            return false;
        }

        int priority = TransitionPriority.Of(type);
        requests.Add(new TransitionRequest
        {
            targetType = type,
            data = data,
            customData = customData,
            priority = priority,
            switchType = switchType,
            checkSame = checkSame
        });
        return true;
    }

    // 带覆盖优先级的重载（预留，非常规使用）
    public bool Request(StateType type, StateData data, int overridePriority,SwitchStateType switchType = SwitchStateType.Normal,bool checkSame = false,bool ignoreCancelTime = false, BaseCustomStateData customData = null)
    {
        // 已死亡：只放行「进入死亡」本身，其余一律拒绝（死亡是终态）
        if (blackboard.characterRunTimeData.isDead && type != StateType.Death)
            return false;

        // 统一在此处做「可取消时间」判断，申请失败则不入队
        if (!ignoreCancelTime && !CanCancelCurrentState())
        {
            return false;
        }

        requests.Add(new TransitionRequest
        {
            targetType = type,
            data = data,
            customData = customData,
            priority = overridePriority,
            switchType = switchType,
            checkSame = checkSame
        });
        return true;
    }

    /// <summary>
    /// 统一判断：当前状态是否已过「可取消时间」（stateData.canceltime）
    /// updateStartTime==0 表示仍处于进入阶段（尚未进入主循环），不可取消
    /// </summary>
    public bool CanCancelCurrentState()
    {
        // 死亡后任何状态都不可取消（配合 Request 的 isDead 拦截，双保险）
        if (blackboard.characterRunTimeData.isDead) return false;

        BaseCharacterstate state = blackboard.characterRunTimeData.currentstate;
        if (state == null || state.stateData == null) return true;

        // 进入阶段未设置 updateStartTime，不可取消
        if (state.updateStartTime == 0) return false;

        // 已超过当前状态的可取消时间，允许取消
        return state.updateStartTime + state.stateData.canceltime <= Time.time;
    }

    // —— 帧末调用：执行仲裁 ——

    public void Execute()
    {
        if (requests.Count == 0) return;

        // 1. 按优先级降序排列
        requests.Sort((a, b) => b.priority.CompareTo(a.priority));
        var chosen = requests[0];

        
        // 2. 检查「当前状态是否可以被中断」
        if (CanInterruptCurrentState(chosen.priority))
        {
            // Debug.Log("Execute switch");
            if (chosen.switchType == SwitchStateType.Normal)
            statemachine.SwitchState(chosen.targetType, chosen.data, chosen.checkSame, chosen.customData);
            else 
            statemachine.RefreshState(chosen.targetType, chosen.data, chosen.customData);
        }

        requests.Clear();
    }

    bool CanInterruptCurrentState(int incomingPriority)
    {
        BaseCharacterstate current = statemachine.CurrentState;
        if (current == null) return true;

        // 当前状态的「最低打断优先级」
        // 比如某些霸体状态设一个很高的值，只有更高优先级才能打断
        int currentStateMinInterruptPriority = current.stateData != null 
            ?  current.stateData.MinInterruptPriority
            : 0;

        // Debug.Log($"chosen.priority: {incomingPriority}  currentPriority: {currentStateMinInterruptPriority}");

        return incomingPriority >= currentStateMinInterruptPriority;
    }
}
