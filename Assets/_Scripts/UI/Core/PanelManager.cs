using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Event;

/// <summary>
/// 面板堆栈管理器 —— 维护面板栈及其中间事件总线。
/// 事件冒泡链：面板子总线 → PanelManager 总线 → 全局总线
/// </summary>
public class PanelManager
{
    Stack<BasePanel> panels;
    UIManager uIManager;
    Transform uiRoot;

    /// <summary>
    /// 中间事件总线，父总线为 EventBus.Global。
    /// 所有面板的子事件线均以此总线为父总线，实现冒泡。
    /// </summary>
    public EventBus EventBus { get; private set; }

    public PanelManager() : this(null) { }

    /// <summary>
    /// 指定 UI 挂载根节点（跨场景 UI 层使用）。uiRoot 为 null 时回退到场景内 "Canvas"。
    /// </summary>
    public PanelManager(Transform uiRoot)
    {
        this.uiRoot = uiRoot;
        panels = new Stack<BasePanel>();
        uIManager = new UIManager();
        EventBus = new EventBus(EventBus.Global);
    }
    public void Push(BasePanel nextPanel)
    {
        if (nextPanel==null) return;
        if (panels.Count>0)
        {
            panels.Peek().OnPause();
        }
        panels.Push(nextPanel);
        GameObject new_gameOBJ = uiRoot != null
            ? uIManager.GetSingleUI(nextPanel.UItype, uiRoot)
            : uIManager.GetSingleUI(nextPanel.UItype);
        nextPanel.Initialize(new UItool(new_gameOBJ));
        nextPanel.Initialize(this);
        nextPanel.Initialize(uIManager);
        // 初始化面板子事件线（父总线 = 当前 PanelManager 的总线）
        nextPanel.InitializeEventBus(EventBus);
        nextPanel.OnEnter();
        // 触发面板管理器事件
        EventBus?.Fire(new PanelManagerEvent(nextPanel, PanelManagerEventType.Pushed));
    }
    public void Pop()
    {
        if (panels.Count<=0) return;
        var panel = panels.Peek();
        panel.Exit();
        panels.Pop();
        // 触发面板管理器事件
        EventBus?.Fire(new PanelManagerEvent(panel, PanelManagerEventType.Popped));
        if(panels.Count>0)
        panels.Peek().OnResume();
    }
    /// <summary>
    /// 清空栈中所有面板，依次调用 Exit()
    /// </summary>
    public void Clear()
    {
        while (panels.Count > 0)
        {
            panels.Peek().Exit();
            panels.Pop();
        }
        // 释放中间事件总线，清理所有订阅
        EventBus?.Dispose();
        EventBus = null;
    }
}
