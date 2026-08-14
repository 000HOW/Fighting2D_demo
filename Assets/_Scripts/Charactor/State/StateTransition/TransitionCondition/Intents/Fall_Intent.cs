using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Fall_Intent",menuName ="PlayerControler/BaseTransferTO/Fall_Intent")]
public class Fall_Intent : BaseTransferTO
{
    public readonly string Description = "current.inJump";
    public override bool IsTrue(Blackboard bb)
    {
        return bb.characterRunTimeData.verticalVelocity<0;
    }
}
