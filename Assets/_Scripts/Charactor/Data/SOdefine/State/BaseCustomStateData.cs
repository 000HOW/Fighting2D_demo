using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自定义状态行为资产（技能 / AI 专用）：仍是 ScriptableObject 资产（含行为方法），
/// 但不再继承 StateData —— 动画/运动/打断/特效等数据改为组合一个可序列化 StateData（animData）。
/// 运行时：Statemachine 收到 Custom 请求时，把 animData 作为 stateData、本资产作为 customStateData 注入 Customstate。
/// </summary>
public abstract class BaseCustomStateData : ScriptableObject
{
    /// <summary>
    /// 本自定义状态的动画/运动/打断/特效数据（内嵌可序列化 StateData）
    /// </summary>
    public StateData animData;
    public bool useOrigPhyics = false;
    public abstract void OnEntryStart(Blackboard bb);
    public abstract void OnExitStart(Blackboard bb);
    public abstract void OnEntry(Blackboard bb);
    public abstract void OnExit(Blackboard bb);
    public abstract void OnUpdate(Blackboard bb);
    public abstract void UpdatePhysics(Blackboard bb,float deltalTime);
    protected virtual void OnValidate()
    {
        #if UNITY_EDITOR

        string clipName = animData == null || animData.mainClip == null ? "":animData.mainClip.name;
        // 根据枚举值生成新的资产名称
        string newAssetName = $"{clipName}_Custom_{this.GetType().Name}";
        
        AssetNameUtility.UpdateAssetName(newAssetName,this);

        #endif
    }
}
