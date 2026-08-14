using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 场景（对应 Boss.unity，Addressables Key = "Boss"）。
/// 场景激活后由 OnEntryAsync 负责 push 本场景需要的面板。
/// </summary>
public class BossScene : BaseScene
{
    public override string SceneKey => SceneKeys.Boss;

    public override void OnEntry()
    {
        panelManager = new PanelManager();
    }

    public override void OnEntryAsync()
    {
        // Boss 场景如需基础 HUD，可在此 push（如 panelManager.Push(new XXXPanel())）
    }

    public override void OnExit()
    {
        panelManager?.Clear();
    }
}
