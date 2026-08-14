using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePanel : BasePanel
{
    static readonly string path = "Prefab/UI/DialoguePanel";//地址的最后一个是要复制的文件名称
    public GameObject Newbutton;
    Text line;
    TypeSentenceExecutor typeSentence;
    public DialoguePanel(GameObject button) : base(new UItype(path))
    {
        Newbutton = button;
    }

    /// <summary>当前句是否仍在逐字打印（供 Dialogue 决定继续键行为）</summary>
    public bool IsTyping => typeSentence != null && typeSentence.IsTyping;

    /// <summary>跳过打印，立即显示完整文本（打印中按继续键时调用）</summary>
    public void SkipTyping() => typeSentence?.FinishInstantly();

    public override void OnEnter()
    {
        base.OnEnter();
        line = uItool.GetOrAddComponentInChildren<Text>("talk");
        typeSentence = uItool.GetOrAddComponent<TypeSentenceExecutor>();
        
        panelManager.EventBus.Subscribe<NewDialogueNode>(TalkLineManage);
        panelManager.EventBus.Subscribe<NewChoiceButton>(MakeNewChoiceButton);
        panelManager.EventBus.Subscribe<ClearChoices>(ClearButtons);
    }

    public override void Exit()
    {
        base.Exit();
        panelManager.EventBus.Unsubscribe<NewDialogueNode>(TalkLineManage);
        panelManager.EventBus.Unsubscribe<NewChoiceButton>(MakeNewChoiceButton);
        panelManager.EventBus.Unsubscribe<ClearChoices>(ClearButtons);
    }

    void MakeNewChoiceButton(NewChoiceButton newChoice)
    {
        if (typeSentence==null) return;
        typeSentence.FinishTyping+= ()=>
        {
            GameObject Choices = uItool.FindChildGameobj("Choices");
            if (!Choices) return;
            GameObject child = GameObject.Instantiate(Newbutton,Choices.transform);
            Button button = child.GetComponentInChildren<Button>();
            Text text = child.GetComponentInChildren<Text>();
            if (button!=null && text!=null)
            {
                text.text = newChoice.choice;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    panelManager.EventBus.Fire(new MakeChoice(newChoice.jumpFrom,newChoice.choiceIndex));
                });
            }
        };
    }


    void TalkLineManage(NewDialogueNode dialogue)
    {
        if (line==null) return;
        line.gameObject.SetActive(true);
        if (typeSentence!=null)
        {
            typeSentence.Actutor(line,dialogue.talk,dialogue.Typedelay);
        }
    }

    void ClearButtons(ClearChoices clearChoices)
    {
        GameObject Choices = uItool.FindChildGameobj("Choices");
        uItool.RemoveAllChildren(Choices);
        if (typeSentence!=null)
        typeSentence.FinishTyping = null;
    }
}
