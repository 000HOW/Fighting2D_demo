using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;

public class Dialogue : MonoBehaviour
{
    public DialogueGraph curDialogueGraph;
    public GameObject ChoiceButtonPrefab;
    public GameObject TalkTarget;
    public GameObject virtualCamera;

    private int currentIndex = 0;
    private bool isDialogueActive = false;
    PanelManager panelManager;
    DialoguePanel curPanel;   // 当前对话面板（查询打字状态 / 跳过打字）

    // ===== 本场景运行期"一次性"数据（不持久化，换场景/重载即重置）=====
    // 已发放奖励的结点集合：保证"带奖励的结点只发一次"
    readonly HashSet<string> grantedNodes = new();
    // 进度续接：上次结束时的对话图名 + 结点下标
    string lastGraphName;
    int lastIndex;

    void Update()
    {
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.E))
                NextLine();
            else if (Input.GetKeyDown(KeyCode.Q))
                BackLine();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if ( collision.gameObject == TalkTarget)
        {
            StartDialogue();
            virtualCamera?.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
         if (collision.gameObject == TalkTarget)
        {
            EndDialogue();
            virtualCamera?.SetActive(false);
        }
    }

    public void StartDialogue()
    {
        if (isDialogueActive) return;   // 防止重复进入
        if (curDialogueGraph==null||curDialogueGraph.nodes==null||curDialogueGraph.nodes.Count==0)
        {
            isDialogueActive = false;
            return;
        }
        isDialogueActive = true;

        // 每次对话都是全新的 PanelManager 会话（Clear 后其 EventBus 会被置空，不可复用）
        panelManager = new PanelManager();
        curPanel = new DialoguePanel(ChoiceButtonPrefab);
        panelManager.Push(curPanel);
        panelManager.EventBus.Subscribe<MakeChoice>(ChangeDialogueGragh);

        // 从上次结束的结点继续（仅本场景运行期，同一对话图才恢复）
        currentIndex = lastGraphName == curDialogueGraph.name
            ? Mathf.Clamp(lastIndex, 0, curDialogueGraph.nodes.Count - 1)
            : 0;
        ShowLine();
    }

    void OnDestroy()
    {
        // 中途销毁时安全退订（EndDialogue 已 Clear 时 EventBus 为 null，?. 短路）
        panelManager?.EventBus?.Unsubscribe<MakeChoice>(ChangeDialogueGragh);
    }

    void NextLine()
    {
        // 打印中按"继续" → 只跳过打印，不进入下一句
        if (curPanel != null && curPanel.IsTyping)
        {
            curPanel.SkipTyping();
            return;
        }

        currentIndex++;
        if (currentIndex < curDialogueGraph.nodes.Count)
            ShowLine();
        else
            EndDialogue();
    }

    void BackLine()
    {
        if (currentIndex==0)
        return;
        currentIndex--;
        ShowLine();
    }

    void ShowLine()
    {
        panelManager.EventBus.Fire(new ClearChoices());
        DialogueNode curNode = curDialogueGraph.nodes[currentIndex];
        panelManager.EventBus.Fire(new NewDialogueNode(curNode.speakerName,curNode.TypeDelay,curNode.line));
        if (curNode.choices!=null&&curNode.choices.Count!=0)
        {
            for(int i=0;i<curNode.choices.Count;i++)
            {
                panelManager.EventBus.Fire(new NewChoiceButton(curNode.choices[i].choice,currentIndex,i));
            }
        }

        // 到达带奖励的结点 → 只发一次（本场景运行期内），通知 RewardDispatcher 发放
        TryGrantNodeReward(curNode);
    }

    /// <summary>
    /// 奖励在"到达该结点"时发放，且每个结点只发一次（本场景运行期内有效）。
    /// </summary>
    void TryGrantNodeReward(DialogueNode node)
    {
        if (node == null || node.reward == null || node.reward.IsEmpty) return;
        string grantKey = $"{curDialogueGraph.name}_{currentIndex}";
        if (!grantedNodes.Add(grantKey)) return;   // 已发放过，跳过
        EventBus.Global.Fire(new DialogueNodeReachedEvent(curDialogueGraph, node));
    }

    void EndDialogue()
    {
        if (!isDialogueActive && panelManager == null) return;
        isDialogueActive = false;

        // 记录进度（仅内存，本场景有效）：中途离开 → 下次从该结点继续；已全部看完 → 下次从头开始
        if (curDialogueGraph != null && curDialogueGraph.nodes != null && curDialogueGraph.nodes.Count > 0)
        {
            lastGraphName = curDialogueGraph.name;
            lastIndex = currentIndex < curDialogueGraph.nodes.Count ? currentIndex : 0;
        }

        panelManager.Clear();
        curPanel = null;
    }

    void ChangeDialogueGragh(MakeChoice change)
    {
        if (change.jumpFrom != currentIndex) return;

        // 防御性边界检查
        if (curDialogueGraph == null || curDialogueGraph.nodes == null ||
            currentIndex < 0 || currentIndex >= curDialogueGraph.nodes.Count) return;
        var node = curDialogueGraph.nodes[currentIndex];
        if (node.choices == null ||
            change.choiceIndex < 0 || change.choiceIndex >= node.choices.Count) return;

        var target = node.choices[change.choiceIndex].jumpTo;
        if (target == null) return;

        curDialogueGraph = target;
        currentIndex = 0;                       // 跳到新图第一句
        panelManager.EventBus.Fire(new ClearChoices()); // 让面板清掉旧按钮
        ShowLine(); 
    }
}
