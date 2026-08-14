using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(AI_EnomyBrain))]
public class EnomyInput : MonoBehaviour , IInput
{
    AI_EnomyBrain enomyBrain;
    void Start() 
    {
        enomyBrain = GetComponent<AI_EnomyBrain>();
    }
    public InputCommand GetInput()
    {
        return enomyBrain.ScanCheck();
    }
}
