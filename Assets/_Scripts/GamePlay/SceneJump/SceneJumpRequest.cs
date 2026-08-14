using UnityEngine;

/// <summary>
/// 一次场景跳转请求的数据：目标场景 Key、提示文本、确认/取消按键。
/// </summary>
public struct SceneJumpRequest
{
    /// <summary>目标场景的 Addressables Key（SceneKeys 常量）。</summary>
    public string TargetSceneKey;
    /// <summary>确认弹窗内显示的提示文本。</summary>
    public string Prompt;
    /// <summary>确认按键（如 E）。</summary>
    public KeyCode ConfirmKey;
    /// <summary>取消按键（如 Q）。</summary>
    public KeyCode CancelKey;
    /// <summary>是否只触发一次（预留）。</summary>
    public bool Once;
}
