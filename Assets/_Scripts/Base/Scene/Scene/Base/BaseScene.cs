using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///
/// </summary>
public abstract class BaseScene
{
    protected PanelManager panelManager;
    public PanelManager GetPanelManager()=>panelManager;

    /// <summary>
    /// 本场景对应的 Addressables 场景 Key（SceneKeys 常量）。
    /// 返回空表示纯逻辑场景（不触发 Unity 场景加载）。
    /// </summary>
    public virtual string SceneKey => string.Empty;

    public virtual void OnEntry(){}
    public virtual void OnExit(){}

    /// <summary>
    /// 场景已激活后的回调（替代旧的 sceneLoaded 钩子）。
    /// 由 SceneSystem 在异步加载/激活完成后调用，此时可安全 push 面板。
    /// </summary>
    public virtual void OnEntryAsync(){}
}
