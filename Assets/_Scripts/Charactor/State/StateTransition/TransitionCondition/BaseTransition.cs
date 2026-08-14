using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTransferTO : ScriptableObject,BaseCondition
{
    public StateType targetState;
    public SwitchStateType switchStateType;
    public bool checkSame;
    public abstract bool IsTrue(Blackboard bb);
    protected virtual void OnValidate()
    {
        #if UNITY_EDITOR
        // 根据枚举值生成新的资产名称
        string newAssetName = $"To {targetState}_{this.GetType().Name}";
        
        AssetNameUtility.UpdateAssetName(newAssetName,this);

        #endif
    }
}
