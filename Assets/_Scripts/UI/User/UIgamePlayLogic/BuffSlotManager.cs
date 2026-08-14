using System;
using System.Collections.Generic;
using System.Text;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家面板 - Buff/Modifier 槽管理
/// 负责 Buff 图标的生成、刷新（倒计时 / 填充条）与销毁
/// 通过全局事件订阅 ModificationManager 发出的增删事件
/// </summary>
public class BuffSlotManager
{
    List<UsableModifier> modifiers = new();
    // 待添加的缓冲队列
    List<UsableModifier> pendingAdds = new();
    List<UsableModifier> pendingRemoves = new();
    Dictionary<UsableModifier,BuffSlotUI> ModifierDic = new();
    GameObject BuffSlotPrefab;
    GameObject buffPivot;
    UItool uItool;
    CharacterControler character; // 玩家引用：只响应玩家自己的修改器，忽略敌人
    StringBuilder buffText = new StringBuilder(8); // 面板字段

    public BuffSlotManager(CharacterControler _character, GameObject _BuffSlotPrefab,ref Action tick)
    {
        character = _character;
        BuffSlotPrefab = _BuffSlotPrefab;
        tick += ModifierTick;
    }

    public void Initialize(UItool tool)
    {
        uItool = tool;
        buffPivot = uItool.FindChildGameobj("Buffs");
        EventBus.Global.Subscribe<ModifierAddEvent>(AddBuffSlot);
        EventBus.Global.Subscribe<ModifierRemoveEvent>(RemoveBuffSlot);
    }

    public void Dispose()
    {
        EventBus.Global.Unsubscribe<ModifierAddEvent>(AddBuffSlot);
        EventBus.Global.Unsubscribe<ModifierRemoveEvent>(RemoveBuffSlot);
    }

    void ModifierTick()
    {
        // 1. 【关键】先把待添加的合并进来（放在开头，让新Buff本帧立即参与计算）
        if (pendingAdds.Count > 0)
        {
            modifiers.AddRange(pendingAdds);
            pendingAdds.Clear();
        }

        // 2. 更新所有 modifier（此时列表已包含最新的）
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            var modifier = modifiers[i];
            ModifierDic[modifier].MainImage.fillAmount = modifier.RemainingTime/modifier.modifierData.duration;
            ModifierDic[modifier].MainImage.sprite = modifier.modifierData.icon;
            ModifierDic[modifier].BackgroundImage.sprite = modifier.modifierData.icon;
            // 每帧：
            buffText.Clear();
            buffText.Append(modifier.RemainingTime.ToString("F1"));
            buffText.Append('s');
            ModifierDic[modifier].textmesh.SetText(buffText);
        }

        if (pendingRemoves.Count>0)
        {
            foreach(var modifierRemove in pendingRemoves)
            {
                if (!ModifierDic.TryGetValue(modifierRemove, out var slot))
                continue; // 未注册，安全忽略
                GameObject.Destroy(slot.uiSlot);
                modifiers.Remove(modifierRemove);
                ModifierDic.Remove(modifierRemove);
            }
            pendingRemoves.Clear();
        }
    }

    void AddBuffSlot(ModifierAddEvent modifierAdd)
    {
        // 只响应玩家自己的 buff：忽略敌人（如盾牌格挡）发来的修改器事件
        var self = character?.blackboard?.characterRunTimeData?.self;
        if (self == null || modifierAdd.owner != self) return;

        GameObject newSlot = GameObject.Instantiate(BuffSlotPrefab,buffPivot.transform);
        pendingAdds.Add(modifierAdd.usable);
        Image mainImage = newSlot.GetComponent<Image>();
        mainImage.type = Image.Type.Filled;
        ModifierDic[modifierAdd.usable] = new BuffSlotUI(mainImage,
        uItool.FindChildGameobj(newSlot,"background").GetComponent<Image>(),
        newSlot,newSlot.GetComponentInChildren<TextMeshProUGUI>());
    }

    void RemoveBuffSlot(ModifierRemoveEvent modifierRemove)
    {
        // 只响应玩家自己的 buff：忽略敌人发来的修改器事件
        var self = character?.blackboard?.characterRunTimeData?.self;
        if (self == null || modifierRemove.owner != self) return;
        pendingRemoves.Add(modifierRemove.usable);
    }
}

/// <summary>
/// Buff 槽 UI 数据容器
/// </summary>
public class BuffSlotUI
{
    public Image MainImage;
    public Image BackgroundImage;
    public GameObject uiSlot;
    public TextMeshProUGUI textmesh;

    public BuffSlotUI(Image _main,Image _background,GameObject _uiSlot,TextMeshProUGUI _mesh)
    {
        MainImage = _main;
        BackgroundImage = _background;
        uiSlot = _uiSlot;
        textmesh = _mesh;
    }
}

/// <summary>
/// Buff 添加事件（由 ModificationManager 发出）
/// </summary>
public struct ModifierAddEvent
{
    public readonly UsableModifier usable;
    /// <summary>修改器所属角色（玩家/敌人），供 UI/特效按引用过滤，避免跨角色误触发</summary>
    public readonly GameObject owner;
    public ModifierAddEvent(UsableModifier modifier, GameObject _owner)
    {
        usable = modifier;
        owner = _owner;
    }
}

/// <summary>
/// Buff 移除事件（由 ModificationManager 发出）
/// </summary>
public struct ModifierRemoveEvent
{
    public readonly UsableModifier usable;
    /// <summary>修改器所属角色（玩家/敌人），供 UI/特效按引用过滤，避免跨角色误触发</summary>
    public readonly GameObject owner;
    public ModifierRemoveEvent(UsableModifier modifier, GameObject _owner)
    {
        usable = modifier;
        owner = _owner;
    }
}
