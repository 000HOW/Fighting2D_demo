using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="TransitionTable",menuName ="PlayerControler/BaseTransferTO/TransitionTable",order =0)]
public class TransitionTable : ScriptableObject
{
    /// <summary>本表代表的源状态（当前状态）</summary>
    public StateType origState;
    /// <summary>
    /// 本表代表状态（origState）自身的状态数据（内嵌可序列化 StateData）。
    /// 转移触发时目标状态的数据从「目标状态的转移表」获取。
    /// 攻击/受击/死亡等由 SpecialAttack/HitInfo/CharacterSO 显式传数据的状态，可不配此处。
    /// </summary>
    public StateData stateData;
    public List<BaseTransferTO> transitions;
    string Description;

    public void OnUpdate(Blackboard bb,StateTransitionArbiter arbiter,Dictionary<StateType,StateData> stateDataLookup)
    {
        // Debug.Log($"currentState: {bb.playerRunTimeData.currentstateType}");
        if (bb.characterRunTimeData.currentstateType != origState) return;
        foreach (var condition in transitions)
        {
            if (condition.IsTrue(bb))
            {
                // 目标状态的数据从「目标状态的转移表」获取；无表/未配 stateData 则跳过该条转移
                if (!stateDataLookup.TryGetValue(condition.targetState, out var targetData) || targetData == null)
                {
                    Debug.LogWarning($"[{this.name}] {origState}->{condition.targetState} 无目标状态数据（目标状态无转移表或未配 stateData），已跳过该转移");
                    continue;
                }

                // 未到当前状态可取消时间时申请失败，直接中断本表后续条件
                if (!arbiter.Request(condition.targetState,targetData,condition.switchStateType,condition.checkSame))
                {
                    // Debug.Log($"Request reject: from {origState} to {condition.targetState} 未到可取消时间");
                    break;
                }
            }
        }        
    }

    void OnValidate()
    {
        #if UNITY_EDITOR
        if (stateData!=null && stateData.mainClip!=null)
        Description = stateData.mainClip?.name;
        // 根据枚举值生成新的资产名称
        string newAssetName = $"{origState}_Count{transitions.Count}_{Description}_TransitTable";
        
        AssetNameUtility.UpdateAssetName(newAssetName,this);

        #endif
    }
}
