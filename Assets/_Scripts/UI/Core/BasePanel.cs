using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Event;

/// <summary>
/// 面板基类 —— 每个面板拥有独立的子事件线（EventBus），
/// 事件可自动冒泡到 PanelManager 总线 → 全局总线。
/// </summary>
public abstract class BasePanel
{
    public UItype UItype {get;private set;}
    public UItool uItool{get;private set;}
    public UIManager uIManager{get;private set;}
    public PanelManager panelManager{get;private set;}

    /// <summary>
    /// 子事件线：此面板的本地事件总线。
    /// 父总线为 PanelManager.EventBus，事件自动向上冒泡。
    /// </summary>
    public EventBus UIEventBus { get; private set; }

    public void Initialize(UItool tool)=>uItool = tool;
    public void Initialize(UIManager _uIManager)=>uIManager = _uIManager;
    public void Initialize(PanelManager _panelmanager)=>panelManager = _panelmanager;

    /// <summary>
    /// 初始化面板的子事件线，以 PanelManager 的总线为父总线。
    /// </summary>
    public void InitializeEventBus(EventBus parentBus)
    {
        UIEventBus = new EventBus(parentBus);
    }

    public BasePanel(UItype _uitype)
    {
        UItype = _uitype;
    }
    public virtual void OnEnter()
    {
        // 触发面板进入事件（自动冒泡到上层总线）
        FireEvent(new PanelLifecycleEvent(this, PanelLifecycleEventType.Entered));
    }
    public virtual void Exit()
    {
        // 触发面板退出事件
        FireEvent(new PanelLifecycleEvent(this, PanelLifecycleEventType.Exited));
        // 释放子事件线，清理所有订阅
        UIEventBus?.Dispose();
        UIEventBus = null;
        uIManager.DestroyUI(UItype);
    }
    /// <summary>
    /// ui暂停
    /// </summary>
    public virtual void OnPause()
    {
        uItool.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;
        FireEvent(new PanelLifecycleEvent(this, PanelLifecycleEventType.Paused));
    }
    /// <summary>
    /// ui继续
    /// </summary>
    public virtual void OnResume()
    {
        uItool.GetOrAddComponent<CanvasGroup>().blocksRaycasts = true;
        FireEvent(new PanelLifecycleEvent(this, PanelLifecycleEventType.Resumed));
    }

    /// <summary>
    /// 在此面板的事件线上触发事件，事件会自动冒泡到 PanelManager 总线 → 全局总线。
    /// </summary>
    /// <param name="bubble">是否允许冒泡到父总线，默认为 true。</param>
    protected void FireEvent<T>(T @event, bool bubble = true)
    {
        UIEventBus?.Fire(@event, bubble);
    }
}
