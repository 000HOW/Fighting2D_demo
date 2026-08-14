using System;
using UnityEngine;

/// <summary>
/// 角色主界面面板 —— 统一调度各功能子模块
/// 仅负责生命周期编排，具体逻辑分别由以下模块负责：
///  - HPSliderUI      生命条
///  - SkillSlotUI     技能快捷槽
///  - BuffSlotManager Buff 图标槽
/// </summary>
public class PlayerPanel : BasePanel
{
    static readonly string path = "Prefab/UI/PlayerPanel";

    HPSliderUI hpSliderUI;
    SkillSlotUI skillSlotUI;
    BuffSlotManager buffSlotManager;
    ComboCounterUI comboCounterUI;

    public PlayerPanel(GameObject slotButton,SkillSender sender,
    CharacterControler _character,ref Action tick,GameObject _BuffSlotPrefab) : base(new UItype(path))
    {
        hpSliderUI = new HPSliderUI(_character);
        skillSlotUI = new SkillSlotUI(sender,slotButton, ref tick);
        buffSlotManager = new BuffSlotManager(_character, _BuffSlotPrefab, ref tick);
        comboCounterUI = new ComboCounterUI(_character);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        hpSliderUI.Initialize(uItool);
        skillSlotUI.Initialize(uItool,panelManager);
        buffSlotManager.Initialize(uItool);
        comboCounterUI.Initialize(uItool);
    }

    public override void OnResume()
    {
        base.OnResume();
        skillSlotUI.Refresh();
    }

    public override void Exit()
    {
        base.Exit();
        // 注销技能槽装备变更事件订阅，防止面板/UI 销毁后事件回调访问已销毁对象
        skillSlotUI.Dispose();
        hpSliderUI.Dispose();
        buffSlotManager.Dispose();
        comboCounterUI.Dispose();
    }
}