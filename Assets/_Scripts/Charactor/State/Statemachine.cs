using System;
using UnityEngine;

/// <summary>
/// 状态机器提供更改当前状态和update当前状态的api
/// 注意：依赖注入，需要初始化，需要驱动
/// </summary>
public class Statemachine
{
    public StateType currentStateType{get;private set;}
    public StateData currentStateData{get;private set;}
    BaseCharacterstate currentstate;
    public BaseCharacterstate CurrentState
    {
        get
        {
            return currentstate;
        }
    }
    public StateType pendingStateType{get;private set;}
    public StateData pendingStateData{get;private set;}
    BaseCharacterstate pendingState;
    public StateProgress progress {get;private set;}
    StateRepository stateRepository;
    Blackboard bb;

    public Statemachine(StateRepository _stateRepository,Blackboard _blackboard)
    {
        stateRepository = _stateRepository;
        bb = _blackboard;
        Initialize();
    }
    public void Initialize()
    {
        currentStateType = bb.CharacterSO.defaultStateType;
        currentstate = stateRepository.GetState(currentStateType);
        if (currentstate==null)
        {
            Debug.LogError("Initialize no currentstate !!!");
            return;
        }
        currentStateData = bb.CharacterSO.defaultStateData;
        currentstate.stateData = bb.CharacterSO.defaultStateData;

        bb.characterRunTimeData.curStateData = currentStateData;
        bb.characterRunTimeData.currentstateType = currentStateType;
        bb.characterRunTimeData.currentstate = currentstate;
        
        bb.characterRunTimeData.curStateData = bb.CharacterSO.defaultStateData;

        currentstate.OnEntryStart(bb);
        progress = StateProgress.Entry;
        bb.characterRunTimeData.stateProgress = progress;
    }
    public void OnUpdate()
    {
        if (currentstate==null)
        {
            Debug.LogError("No currentstate in Update");
            return;
        }

        if (progress==StateProgress.Exit)
        {
            if(currentstate.OnExit(bb))
            {
                if (pendingState==null)
                {
                    Debug.LogError("No PendingState To Entry !!!");
                    return;
                }
                currentstate = pendingState;
                currentStateType = currentstate.stateType;
                currentStateData = pendingState.stateData;

                bb.characterRunTimeData.curStateData = currentStateData;
                bb.characterRunTimeData.currentstateType = currentStateType;
                bb.characterRunTimeData.currentstate = currentstate;


                pendingState = null;
                pendingStateData = null;
                pendingStateType = StateType.None;

                progress = StateProgress.Entry;
                currentstate.OnEntryStart(bb);
            }

        }
        
        else if (progress==StateProgress.Entry)
        {
            if (currentstate.OnEntry(bb))
            {
                progress = StateProgress.Main;
            }

        }

        else if (progress==StateProgress.Main)
        {
            currentstate.OnUpdate(bb);
            if (pendingState!=null)
            {
                progress = StateProgress.Exit;
                currentstate.OnExitStart(bb);
            }
        }
        bb.characterRunTimeData.stateProgress = progress;
       
    }
    public void UpdatePhysics()
    {
        if (currentstate==null)
        {
            Debug.LogError("No currentstate in Update");
            return;
        }
        currentstate.UpdatePhysics(bb,Time.fixedDeltaTime);
    }
    /// <summary>
    /// 普通转换状态
    /// </summary>
    /// <param name="newstateType"></param>
    /// <param name="newData">新状态的状态数据</param>
    /// <param name="checkSamme">是否过滤重复状态</param>
    /// <param name="customData">目标为 Custom 时的行为资产（技能用；纯数据/非自定义传 null）</param>
    public void SwitchState(StateType newstateType,StateData newData,bool checkSamme = true, BaseCustomStateData customData = null)
    {
        if (currentStateType==newstateType&&checkSamme) return;

        BaseCharacterstate result = stateRepository.GetState(newstateType);

        if (newData==null)
        {
            Debug.LogError("SwitchState need StateData !!!");
            return;
        }
        pendingState = result;
        pendingState.stateData = newData;
        pendingStateData = newData;
        pendingStateType = newstateType;

        // 自定义状态注入行为资产（技能用；纯数据/非自定义则忽略）
        if (result is Customstate cs)
            cs.customStateData = customData;

    }

    /// <summary>
    /// 刷新式状态转换：无视当前状态的Exit，直接切换
    /// </summary>
    /// <param name="newstateType">设置要刷新的状态，默认None代表刷新当前状态</param>
    /// <param name="newData">如果要刷新为新的状态必须注入状态数据</param>
    /// <param name="customData">目标为 Custom 时的行为资产（技能用；纯数据/非自定义传 null）</param>
    public void RefreshState(StateType newstateType = StateType.None,StateData newData = null, BaseCustomStateData customData = null)
    {
        if (newstateType==StateType.None)
        {
            // 自刷新分支同样要清，避免被旧 Exit 打断
            pendingState = null;
            pendingStateData = null;
            pendingStateType = StateType.None;
            progress = StateProgress.Entry;
            return;
        }

        BaseCharacterstate result = stateRepository.GetState(newstateType);
        if (newData==null)
        {
            Debug.LogError("Refresh NewState need StateData !!!");
            return;
        }
        currentstate = result;
        currentstate.stateData = newData;
        currentStateData = newData;
        currentStateType = currentstate.stateType;

        bb.characterRunTimeData.curStateData = currentStateData;
        bb.characterRunTimeData.currentstateType = currentStateType;
        bb.characterRunTimeData.currentstate = currentstate;

        //  修复：清掉残留的待切换状态
        pendingState = null;
        pendingStateData = null;
        pendingStateType = StateType.None;

        // 自定义状态注入行为资产（技能用；纯数据/非自定义则忽略）
        if (result is Customstate cs)
            cs.customStateData = customData;

        currentstate.OnEntryStart(bb);
        progress = StateProgress.Entry;
        bb.characterRunTimeData.stateProgress = progress;
        
    }

    public class StateChangedEventArgs : EventArgs
    {
        public StateType OldState;
        public StateType NewState;
        public StateData NewData;
        public SwitchStateType SwitchType;
    }

}


public enum SwitchStateType
{
    Normal,
    Refresh
}