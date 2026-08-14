using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="NoneHorizontal_Intent",menuName ="PlayerControler/BaseTransferTO/NoneHorizontal_Intent")]
public class NoneHorizontal_Intent : BaseTransferTO
{
    public readonly string Description = "!current.inLeft && !current.inRight";
    public override bool IsTrue(Blackboard bb)
    {
        InputCommand current = bb.inputData.cur_inputComand;
        return !current.inLeft && !current.inRight;
    }
}
