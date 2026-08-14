using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="InAttack_Intent",menuName ="PlayerControler/BaseTransferTO/InAttack_Intent")]
public class InAttack_Intent : BaseTransferTO
{
    public override bool IsTrue(Blackboard bb)
    {
        return bb.inputData.cur_inputComand.inAattack;
    }

}
