using System;
using UnityEngine;


public class KeyInputProvider : MonoBehaviour,IInput
{
    void Awake()
    {
        CharacterControler controler = GetComponent<CharacterControler>();
        if (controler !=null)
        {
            controler.InputSource = this;
        }
    }
    public  InputCommand GetInput()
    {

        return new InputCommand
        {
            inLeft = Input.GetKey(KeyCode.A),
            inRight = Input.GetKey(KeyCode.D),
            inJump = Input.GetKey(KeyCode.W),
            inDash = Input.GetKey(KeyCode.Space),
            inAattack = Input.GetKey(KeyCode.J),
            NormalizeAxis = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"))
        };
    }

}