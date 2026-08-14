using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景跳转确认弹窗（由 GlobalUILayer 承载）。
/// 提示文本 + 确认/取消按钮；按键监听由 SceneJumpManager（MonoBehaviour）驱动。
/// </summary>
public class SceneJumpPanel : BasePanel
{
    static readonly string path = "Prefab/UI/SceneJumpPanel";

    SceneJumpRequest request;
    /// <summary>确认回调（由 SceneJumpManager 注入）。</summary>
    public Action OnConfirm;

    public SceneJumpPanel(SceneJumpRequest request) : base(new UItype(path))
    {
        this.request = request;
    }

    public override void OnEnter()
    {
        base.OnEnter();

        var prompt = uItool.GetOrAddComponentInChildren<Text>("Prompt");
        if (prompt != null) prompt.text = request.Prompt;

        var confirm = uItool.GetOrAddComponentInChildren<Button>("Confirm");
        confirm?.onClick.RemoveAllListeners();
        confirm?.onClick.AddListener(() => OnConfirm?.Invoke());

        var cancel = uItool.GetOrAddComponentInChildren<Button>("Cancel");
        cancel?.onClick.RemoveAllListeners();
        cancel?.onClick.AddListener(() =>
        {
            if (panelManager != null) panelManager.Pop();
        });
    }
}
