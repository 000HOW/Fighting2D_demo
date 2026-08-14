using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="OnGround_Intent",menuName ="PlayerControler/BaseTransferTO/OnGround_Intent")]
public class OnGround_Intent : BaseTransferTO
{
    public readonly string Description = "Onground==true? bb.playerRunTimeData.isground : !bb.playerRunTimeData.isground";
    public bool Onground = true;
    public override bool IsTrue(Blackboard bb)
    {
        return Onground==true? bb.characterRunTimeData.isground : !bb.characterRunTimeData.isground;
    }

    protected override void OnValidate()
    {
        #if UNITY_EDITOR
        // 根据枚举值生成新的资产名称
        string newAssetName = $"To {targetState}_{Onground}_{this.GetType().Name}";
        
        AssetNameUtility.UpdateAssetName(newAssetName,this);

        #endif
    }
}
