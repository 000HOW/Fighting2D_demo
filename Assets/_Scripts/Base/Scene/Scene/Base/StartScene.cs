using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 开始场景（对应 Start.unity，Addressables Key = "Start"）。
/// 场景加载/激活由 SceneSystem.SetSceneAsync 完成，激活后回调 OnEntryAsync。
/// </summary>
public class StartScene : BaseScene
{
    public override string SceneKey => SceneKeys.Start;

    public override void OnEntry()
    {
        panelManager = new PanelManager();
    }

    public override void OnEntryAsync()
    {
        panelManager.Push(new StartPanel());
    }

    public override void OnExit()
    {
        panelManager?.Clear();
    }
}
