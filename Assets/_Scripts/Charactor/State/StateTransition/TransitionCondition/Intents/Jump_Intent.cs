using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Jump_Intent",menuName ="PlayerControler/BaseTransferTO/Jump_Intent")]
public class Jump_Intent : BaseTransferTO
{
    public readonly string Description = "current.inJump && bb.playerRunTimeData.isground";
    public override bool IsTrue(Blackboard bb)
    {
        InputCommand current = bb.inputData.cur_inputComand;
        return current.inJump ;
    }
}
