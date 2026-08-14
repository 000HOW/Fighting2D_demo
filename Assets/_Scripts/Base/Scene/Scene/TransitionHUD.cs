using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景过场 HUD：全屏黑幕 + 加载进度文字（挂 GlobalUILayer 常驻 Canvas 上）。
/// - SetProgress(p)：更新加载进度文字（0~1，显示为百分比）。
/// - FadeToBlack() / FadeFromBlack()：黑幕渐变（时长/缓动由 SceneSystem 从 SceneTransitionConfig 传入）。
///
/// 层级要求（prefab）：
///   TransitionHUD (root, 代码自动挂 CanvasGroup + FadeHelper)
///     ├─ ProgressText   (Text, 显示 "Loading 42%")
///     └─ BlackOverlay   (全屏黑色 Image, 代码自动挂 CanvasGroup；必须排在 ProgressText 之后，
///                        渲染在最上层 —— 渐变其 CanvasGroup.alpha 即可实现"进度100%后文字没入黑色")
/// </summary>
public class TransitionHUD : BasePanel
{
    static readonly string path = "Prefab/UI/TransitionHUD";

    CanvasGroup canvasGroup;
    FadeHelper fadeHelper;
    CanvasGroup overlayGroup;   // 黑幕 Image 上的 CanvasGroup（控制黑幕透明度）
    Text progressText;

    public TransitionHUD() : base(new UItype(path)) { }

    public override void OnEnter()
    {
        base.OnEnter();

        canvasGroup = uItool.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true; // 过场期间屏蔽所有输入

        fadeHelper = uItool.GetOrAddComponent<FadeHelper>();

        // 确保黑幕 Image 存在（预制体提供黑色纯色），并给黑幕挂 CanvasGroup 用于渐变
        uItool.GetOrAddComponentInChildren<Image>("BlackOverlay");
        overlayGroup = uItool.GetOrAddComponentInChildren<CanvasGroup>("BlackOverlay");
        progressText = uItool.GetOrAddComponentInChildren<Text>("ProgressText");

        // 初始：黑幕透明、进度归零
        SetBlackAlpha(0f);
        SetProgress(0f);
    }

    /// <summary>更新加载进度文字（0~1）。</summary>
    public void SetProgress(float progress)
    {
        float p = Mathf.Clamp01(progress);
        if (progressText != null) progressText.text = $"Loading {p:P0}";
    }

    /// <summary>直接设置黑幕透明度（0 透明 ~ 1 全黑）。</summary>
    public void SetBlackAlpha(float a)
    {
        if (overlayGroup != null) overlayGroup.alpha = Mathf.Clamp01(a);
    }

    /// <summary>黑幕渐变到全黑（返回 Task，渐变完成后继续）。</summary>
    public Task FadeToBlack(float duration, AnimationCurve curve = null)
        => RunTween(1f, duration, curve);

    /// <summary>黑幕渐变回透明（露出场景）。</summary>
    public Task FadeFromBlack(float duration, AnimationCurve curve = null)
        => RunTween(0f, duration, curve);

    async Task RunTween(float to, float duration, AnimationCurve curve)
    {
        if (fadeHelper == null || overlayGroup == null) return;
        var tcs = new TaskCompletionSource<bool>();
        fadeHelper.Run(TweenRoutine(to, duration, curve, tcs));
        await tcs.Task;
    }

    IEnumerator TweenRoutine(float to, float duration, AnimationCurve curve, TaskCompletionSource<bool> tcs)
    {
        if (overlayGroup != null)
            yield return fadeHelper.TweenAlpha(overlayGroup, overlayGroup.alpha, to, duration, curve);
        tcs.TrySetResult(true);
    }
}
