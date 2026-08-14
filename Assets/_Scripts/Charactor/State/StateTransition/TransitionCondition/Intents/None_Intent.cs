using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="None_Intent",menuName ="PlayerControler/BaseTransferTO/None_Intent")]
public class None_Intent : BaseTransferTO
{
    public readonly string Description = "true";
    public override bool IsTrue(Blackboard bb)
    {
        return true;
    }
}
