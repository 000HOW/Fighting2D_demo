using System;
using System.Text;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家面板 - 技能快捷槽管理
/// 负责技能槽的生成、刷新以及装备/卸下交互
/// </summary>
public class SkillSlotUI
{
    SkillSender skillSender;
    GameObject newSlot;
    Button bagButton;
    // 每个槽位的 UI 数据（缓存全部组件，仿 BuffSlotUI：主图/背景图/文本/默认色）
    SkillSlotUIElement[] slots = new SkillSlotUIElement[SkillSender.SLOTCOUNT];
    StringBuilder cdText = new StringBuilder(8);

    UItool uItool;
    PanelManager panelManager;
    // 装备变更事件订阅用委托实例（Unsubscribe 必须与 Subscribe 传同一实例）
    Action<SkillEquipmentChangedEvent> equipmentChangedHandler;

    public SkillSlotUI(SkillSender sender,GameObject slotButton,ref Action tick)
    {
        skillSender = sender;
        newSlot = slotButton;
        tick += CooldownTick;
    }

    public void Initialize(UItool tool,PanelManager _panelManager)
    {
        uItool = tool;
        panelManager = _panelManager;
        // 订阅装备变更事件（跨场景恢复 / 装备 / 卸下时重扫槽位）。
        // OnEnable 阶段订阅 → 先于 Start 阶段 RestoreFromSession 的广播，事件不会丢。
        equipmentChangedHandler = OnSkillEquipmentChanged;
        EventBus.Global.Subscribe(equipmentChangedHandler);
        SkillSlotInitialize();
    }

    /// <summary>
    /// 刷新所有技能槽显示（面板 Resume 时调用，事件驱动之外兜底）
    /// </summary>
    public void Refresh()
    {
        ScanSlot();
    }

    /// <summary>
    /// 装备变更事件回调：重扫所有技能槽（修复跨场景恢复后 UI 不显示）
    /// </summary>
    void OnSkillEquipmentChanged(SkillEquipmentChangedEvent e)
    {
        // 防御：UI 尚未初始化或已被销毁（场景切换中）时跳过
        if (slots == null || slots.Length == 0 || slots[0] == null || slots[0].MainImage == null) return;
        ScanSlot();
    }

    /// <summary>
    /// 注销装备变更事件订阅（面板退出 / 场景切换时调用，防止回调访问已销毁 UI）
    /// </summary>
    public void Dispose()
    {
        if (equipmentChangedHandler != null)
        {
            EventBus.Global.Unsubscribe(equipmentChangedHandler);
            equipmentChangedHandler = null;
        }
    }

    void SkillSlotInitialize()
    {
        GameObject bag = uItool.FindChildGameobj("bag");
        if (bag!=null)
        {
            bagButton = bag.GetComponent<Button>();
            bagButton.onClick.RemoveAllListeners();
            bagButton.onClick.AddListener(() =>
            {
               panelManager.Push(new SkillBagPanel(skillSender,newSlot));
            });
        }

        GameObject slotPivot = uItool.FindChildGameobj("SkillUseSlot");
        if (!slotPivot) return;
        uItool.RemoveAllChildren(slotPivot);
        for (int i=0;i<SkillSender.SLOTCOUNT;i++)
        {
            GameObject slot = GameObject.Instantiate(newSlot,slotPivot.transform);
            Button button = slot.GetComponent<Button>();
            Image image = slot.GetComponent<Image>();
            Text text = slot.GetComponentInChildren<Text>();
            image.type = Image.Type.Filled; // 主图设为 Filled，fillAmount 才能表现冷却

            // 从 newSlot 预制体一次性收集全部组件并缓存（仿 BuffSlotManager，null 安全）
            Image bg = uItool.FindChildGameobj(slot, "background")?.GetComponent<Image>();
            TextMeshProUGUI cdTextMesh = slot.GetComponentInChildren<TextMeshProUGUI>();

            SkillSlotUIElement element = new SkillSlotUIElement(slot,button,image,bg,text,cdTextMesh);
            element.MainDefaultColor = image.color;
            element.BackgroundDefaultColor = bg!=null ? bg.color : Color.white;
            slots[i] = element;
        }

        ScanSlot();
    }

    void ScanSlot()
    {
        for (int i=0;i<SkillSender.SLOTCOUNT;i++)
        {
            SkillSlotUIElement element = slots[i];
            if (element==null) continue;

            PlayerSkillData item = skillSender.equipmentSlot[i];

            // 空槽：槽内所有图片透明（避免 null sprite 的白图异常），清空文本与监听
            if (item==null)
            {
                element.MainImage.color = Transparent(element.MainDefaultColor);
                element.MainImage.sprite = null;
                if (element.BackgroundImage!=null)
                {
                    element.BackgroundImage.color = Transparent(element.BackgroundDefaultColor);
                    element.BackgroundImage.sprite = null;
                }
                if (element.DescriptionText!=null) element.DescriptionText.text = "";
                if (element.textmesh!=null) element.textmesh.SetText("");
                element.button.onClick.RemoveAllListeners();
                continue;
            }

            // 填充：恢复默认颜色与透明度，设置图标/描述/背景
            element.MainImage.color = element.MainDefaultColor;
            element.MainImage.sprite = item.icon;
            if (element.BackgroundImage!=null)
            {
                element.BackgroundImage.color = element.BackgroundDefaultColor;
                element.BackgroundImage.sprite = item.icon;
            }
            if (element.DescriptionText!=null) element.DescriptionText.text = item.Description;

            element.button.onClick.RemoveAllListeners();
            element.button.onClick.AddListener(() =>
            {
                if(skillSender.UnloadSkill(item))
                {
                    element.MainImage.sprite = null;
                    element.MainImage.color = Transparent(element.MainDefaultColor);
                    if (element.BackgroundImage!=null)
                    {
                        element.BackgroundImage.sprite = null;
                        element.BackgroundImage.color = Transparent(element.BackgroundDefaultColor);
                    }
                    if (element.DescriptionText!=null) element.DescriptionText.text = "";
                    if (element.textmesh!=null) element.textmesh.SetText("");
                    element.button.onClick.RemoveAllListeners();
                }
            });
        }
    }

    /// <summary>
    /// 保留 RGB，将 alpha 置 0（透明）——用于空槽隐藏图片
    /// </summary>
    static Color Transparent(Color c)
    {
        return new Color(c.r,c.g,c.b,0f);
    }

    /// <summary>
    /// 每帧驱动冷却表现（由 PlayerPanelManager 的 tick 调用）
    /// </summary>
    void CooldownTick()
    {
        for (int i=0;i<SkillSender.SLOTCOUNT;i++)
        {
            SkillSlotUIElement element = slots[i];
            if (element==null) continue;
            // 防御：UI 组件可能已被销毁（面板退出 / 场景切换 / Canvas 卸载），
            // Unity 的 == 对已销毁对象返回 true，直接跳过该槽避免 MissingReferenceException
            if (element.MainImage == null) continue;

            if (skillSender.IsOnCooldown(i))
            {
                PlayerSkillData skill = skillSender.equipmentSlot[i];
                element.MainImage.fillAmount = skillSender.GetCooldownFill(i);
                if (element.BackgroundImage!=null && skill!=null)
                    element.BackgroundImage.sprite = skill.icon;

                cdText.Clear();
                cdText.Append(skillSender.GetCooldownRemaining(i).ToString("F1"));
                cdText.Append('s');
                if (element.textmesh!=null)
                    element.textmesh.SetText(cdText);
            }
            else
            {
                element.MainImage.fillAmount = 1f;
                if (element.textmesh!=null)
                    element.textmesh.SetText("");
            }
        }
    }
}

/// <summary>
/// 技能槽冷却 UI 数据容器（仿 BuffSlotUI）
/// </summary>
public class SkillSlotUIElement
{
    public GameObject uiSlot;
    public Button button;
    public Image MainImage;
    public Image BackgroundImage;
    public Text DescriptionText;
    public TextMeshProUGUI textmesh;
    public Color MainDefaultColor;
    public Color BackgroundDefaultColor;

    public SkillSlotUIElement(GameObject _uiSlot,Button _button,Image _main,Image _background,Text _description,TextMeshProUGUI _mesh)
    {
        uiSlot = _uiSlot;
        button = _button;
        MainImage = _main;
        BackgroundImage = _background;
        DescriptionText = _description;
        textmesh = _mesh;
    }
}

/// <summary>
/// 技能冷却开始事件（由 SkillSender 释放技能时发出）
/// </summary>
public struct SkillCooldownStartEvent
{
    public readonly int SlotIndex;
    public readonly PlayerSkillData Skill;
    public readonly float CooldownTime;

    public SkillCooldownStartEvent(int slotIndex, PlayerSkillData skill, float cooldownTime)
    {
        SlotIndex = slotIndex;
        Skill = skill;
        CooldownTime = cooldownTime;
    }
}

/// <summary>
/// 技能冷却结束事件（由 SkillSender 冷却归零时发出）
/// </summary>
public struct SkillCooldownEndEvent
{
    public readonly int SlotIndex;

    public SkillCooldownEndEvent(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}

/// <summary>
/// 技能装备变更事件（装备 / 卸下 / 跨场景恢复时由 SkillSender 发出）
/// SlotIndex：变更的槽位；-1 表示全量恢复（跨场景）。
/// </summary>
public struct SkillEquipmentChangedEvent
{
    public readonly int SlotIndex;

    public SkillEquipmentChangedEvent(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}
