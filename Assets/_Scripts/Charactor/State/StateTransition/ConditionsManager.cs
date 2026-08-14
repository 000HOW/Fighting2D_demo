using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态转换条件管理器
/// 注意：需要初始化，需要驱动
/// </summary>
public class ConditionsManager
{
    List<TransitionTable> tables;
    /// <summary>
    /// 目标状态 → 状态数据 查找表：由各转移表的 origState + stateData 构建（首匹配）。
    /// 转移触发时据此取目标状态数据（intent 不再持有 targetStateData）。
    /// </summary>
    Dictionary<StateType, StateData> stateDataLookup;
    Blackboard blackboard;
    StateTransitionArbiter arbiter;
    bool canWork = false;
    public void Initialize(Blackboard _blackborad,StateTransitionArbiter _arbiter)
    {
        blackboard = _blackborad;
        arbiter = _arbiter;
        tables = blackboard.CharacterSO.transitionTables;
        if (tables==null||tables.Count==0)
        {
            Debug.LogWarning("no tables!");
            canWork = false;
            return;
        }

        // 构建「目标状态 → 状态数据」查找表：取该状态的首个非空 stateData 表（origState 匹配）
        stateDataLookup = new Dictionary<StateType, StateData>();
        foreach (var table in tables)
        {
            if (table == null || table.stateData == null) continue;
            if (!stateDataLookup.ContainsKey(table.origState))
                stateDataLookup[table.origState] = table.stateData;
        }

        canWork = true;
    }

    public void OnUpdate()
    {
        if (blackboard.characterRunTimeData.isDead) return;   // 死亡后禁止条件表把角色转出死亡
        if (!canWork) return;
        foreach (TransitionTable table in tables)
        {
            table.OnUpdate(blackboard,arbiter,stateDataLookup);
        }
    }
}
