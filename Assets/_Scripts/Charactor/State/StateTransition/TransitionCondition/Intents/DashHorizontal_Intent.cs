using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Run_Intent",menuName ="PlayerControler/BaseTransferTO/Run_Intent")]
public class DashHorizontal_Intent : BaseTransferTO
{
    public readonly string Description = "current.inDash&&(current.inRight||current.inLeft)";
    public bool Dash;
    public override bool IsTrue(Blackboard bb)
    {
        InputCommand current = bb.inputData.cur_inputComand;
        bool a = (current.inRight&&bb.characterRunTimeData.facingDir==1)||(current.inLeft&&bb.characterRunTimeData.facingDir==-1);
        return Dash ? current.inDash && a : !current.inDash && a;
    }

    protected override void OnValidate()
    {
        #if UNITY_EDITOR
        // 根据枚举值生成新的资产名称
        string newAssetName = $"To {targetState}_{Dash}_{this.GetType().Name}";
        
        AssetNameUtility.UpdateAssetName(newAssetName,this);

        #endif
    }
}
