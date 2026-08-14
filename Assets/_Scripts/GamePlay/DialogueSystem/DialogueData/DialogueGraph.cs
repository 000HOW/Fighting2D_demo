using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue/DialogueGraph")]
public class DialogueGraph : ScriptableObject
{
    public List<DialogueNode> nodes = new List<DialogueNode>();
}


[System.Serializable]
public class DialogueNode
{
    public string speakerName;
    [TextArea(3,10)] public string line;
    public float TypeDelay = 0.2f;
    // 将来可扩展：选项、事件等
    public List<ChoicesContainer> choices;
    // 本结点作为结束点时发放的奖励（可留空）
    public RewardData reward;
}

[System.Serializable]
public class ChoicesContainer
{
    public string choice;
    public DialogueGraph jumpTo;
}