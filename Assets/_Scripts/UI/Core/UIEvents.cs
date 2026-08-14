using GameFramework.Event;

/// <summary>
/// UI 面板生命周期事件类型
/// </summary>
public enum PanelLifecycleEventType
{
    /// <summary>面板被打开（OnEnter）</summary>
    Entered,
    /// <summary>面板被关闭（Exit）</summary>
    Exited,
    /// <summary>面板被暂停（OnPause，被其他面板覆盖）</summary>
    Paused,
    /// <summary>面板被恢复（OnResume，覆盖它的面板被关闭）</summary>
    Resumed,
}

/// <summary>
/// UI 面板生命周期事件，由 BasePanel 在生命周期方法中自动触发。
/// 事件冒泡链：面板子总线 → PanelManager 总线 → EventBus.Global
/// 
/// 外部监听示例：
/// <code>
/// Events.Subscribe&lt;PanelLifecycleEvent&gt;(evt =>
/// {
///     Debug.Log($"{evt.Panel.GetType().Name}  {evt.EventType}");
/// });
/// </code>
/// </summary>
public readonly struct PanelLifecycleEvent
{
    /// <summary>触发事件的面板实例</summary>
    public readonly BasePanel Panel;
    /// <summary>生命周期事件类型</summary>
    public readonly PanelLifecycleEventType EventType;

    public PanelLifecycleEvent(BasePanel panel, PanelLifecycleEventType eventType)
    {
        Panel = panel;
        EventType = eventType;
    }
}

/// <summary>
/// 面板管理器事件类型
/// </summary>
public enum PanelManagerEventType
{
    /// <summary>有新面板被 Push 入栈</summary>
    Pushed,
    /// <summary>有面板被 Pop 出栈</summary>
    Popped,
    /// <summary>面板栈被清空</summary>
    Cleared,
}

/// <summary>
/// 面板管理器事件，由 PanelManager 在 Push/Pop/Clear 时自动触发。
/// 事件冒泡到 EventBus.Global，外部可直接通过 Events 静态类订阅。
/// 
/// 外部监听示例：
/// <code>
/// Events.Subscribe&lt;PanelManagerEvent&gt;(evt =>
/// {
///     Debug.Log($"面板管理器：{evt.EventType}");
/// });
/// </code>
/// </summary>
public readonly struct PanelManagerEvent
{
    /// <summary>关联的面板实例（Cleared 事件时为 null）</summary>
    public readonly BasePanel Panel;
    /// <summary>管理器事件类型</summary>
    public readonly PanelManagerEventType EventType;

    public PanelManagerEvent(BasePanel panel, PanelManagerEventType eventType)
    {
        Panel = panel;
        EventType = eventType;
    }
}

