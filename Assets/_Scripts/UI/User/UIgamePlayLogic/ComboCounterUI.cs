using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 连击数显示：totalCombo>0 时显示，归零时隐藏
/// 监听 ComboManager 发出的 OnComboChanged 事件
/// 面板预制体需包含：ComboRoot 节点（内含 ComboText 文本）
/// </summary>
public class ComboCounterUI
{
    UItool uItool;
    GameObject root;
    TextMeshProUGUI comboText;
    CharacterControler character;

    public ComboCounterUI(CharacterControler _character)
    {
        character = _character;
    }

    public void Initialize(UItool tool)
    {
        uItool = tool;
        root = uItool.FindChildGameobj("ComboRoot");
        if (root == null)
        {
            Debug.LogWarning("ComboCounterUI: 找不到 ComboRoot 节点，请在面板预制体添加");
            return;
        }
        comboText = uItool.GetOrAddComponentInChildren<TextMeshProUGUI>(root, "ComboText");
        root.SetActive(false);
        EventBus.Global.Subscribe<OnComboChanged>(OnComboChanged);
    }

    public void Dispose()
    {
        EventBus.Global.Unsubscribe<OnComboChanged>(OnComboChanged);
    }

    void OnComboChanged(OnComboChanged e)
    {
        // 只响应本角色（玩家）的连击事件，忽略敌人发来的事件
        if (character?.blackboard?.characterRunTimeData?.self == null) return;
        if (e.Character != character.blackboard.characterRunTimeData.self.name) return;

        if (root == null) return;
        bool show = e.Combo > 0;
        root.SetActive(show);
        if (show && comboText != null)
            comboText.text = $"{e.Combo} HITS";
    }
}

/// <summary>
/// 连击数变化事件（由 ComboManager 发出，Combo==0 表示归零/隐藏）
/// </summary>
public struct OnComboChanged
{
    public readonly int Combo;
    public string Character;
    public OnComboChanged(int combo, string name)
    {
        Combo = combo;
        Character = name;
    }
}
