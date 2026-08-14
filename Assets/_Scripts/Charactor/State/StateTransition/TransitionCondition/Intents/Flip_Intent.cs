using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Flip_Intent",menuName ="PlayerControler/BaseTransferTO/Flip_Intent")]
public class Flip_Intent : BaseTransferTO
{
    public readonly string Description = "facingDir==1 && current.inLeft || facingDir==-1 &&current.inRight";
    public override bool IsTrue(Blackboard bb)
    {
        InputCommand current = bb.inputData.cur_inputComand;
        int facingDir = bb.characterRunTimeData.facingDir;

        return facingDir==1 && current.inLeft || facingDir==-1 &&current.inRight;
    }

}
