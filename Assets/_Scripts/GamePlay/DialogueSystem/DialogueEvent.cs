using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NewDialogueNode 
{
    public readonly float Typedelay;
    public readonly string speakName;
    public readonly string talk;
    public NewDialogueNode (string _speaker,float _delay = 0.2f,string _line = "")
    {
        speakName = _speaker;
        Typedelay = _delay;
        talk = _line;
    }
}

public struct NewChoiceButton
{
    public readonly string choice;
    public readonly int jumpFrom;
    public readonly int choiceIndex;
    public NewChoiceButton(string _choice,int from,int _index)
    {
        choice = _choice;
        jumpFrom = from;
        choiceIndex = _index;
    }
}

public struct MakeChoice
{
    public readonly int jumpFrom;
    public readonly int choiceIndex;
    public MakeChoice(int _jumpFrom,int _choiceIndex)
    {
        jumpFrom = _jumpFrom;
        choiceIndex = _choiceIndex;
    }
}

public struct ClearChoices
{
    
}