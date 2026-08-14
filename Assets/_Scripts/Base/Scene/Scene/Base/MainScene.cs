using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主场景（对应 Main.unity，Addressables Key = "Main"）。
/// 场景加载/激活由 SceneSystem.SetSceneAsync 完成，激活后回调 OnEntryAsync。
/// </summary>
public class MainScene : BaseScene
{
    public override string SceneKey => SceneKeys.Main;

    public override void OnEntry()
    {
        panelManager = new PanelManager();
    }

    public override void OnEntryAsync()
    {

    }

    public override void OnExit()
    {
        panelManager?.Clear();
    }
}
