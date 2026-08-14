using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Horizontal_Intent",menuName ="PlayerControler/BaseTransferTO/Horizontal_Intent")]
public class Horizontal_Intent : BaseTransferTO
{
    public readonly string Description = "current.inLeft||current.inRight";
    public override bool IsTrue(Blackboard bb)
    {
        InputCommand current = bb.inputData.cur_inputComand;
        return current.inLeft||current.inRight;
    }
}
