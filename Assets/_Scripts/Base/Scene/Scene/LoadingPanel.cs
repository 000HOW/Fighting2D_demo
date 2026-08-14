using System.Collections;
using GameFramework.Event;
using GameFramework.Scene;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景加载过渡面板：全屏遮罩 + 渐变 + 进度条。
/// 由 SceneSystem 在异步切换场景时通过 GlobalUILayer 显示/关闭。
/// 进度驱动框架已广播的 SceneLoadProgressEvent / SceneLoadCompleteEvent。
/// 所有子物体引用空安全：prefab 缺失时仅警告，不崩溃。
/// </summary>
public class LoadingPanel : BasePanel
{
    static readonly string path = "Prefab/UI/LoadingPanel";

    CanvasGroup canvasGroup;
    FadeHelper fadeHelper;
    Slider slider;
    Text progressText;

    public LoadingPanel() : base(new UItype(path)) { }

    public override void OnEnter()
    {
        base.OnEnter();

        canvasGroup = uItool.GetOrAddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        fadeHelper = uItool.GetOrAddComponent<FadeHelper>();

        slider = uItool.GetOrAddComponentInChildren<Slider>("ProgressBar");
        progressText = uItool.GetOrAddComponentInChildren<Text>("ProgressText");

        EventDispatcher.Subscribe<SceneLoadProgressEvent>(OnLoadProgress);
        EventDispatcher.Subscribe<SceneLoadCompleteEvent>(OnLoadComplete);
    }

    public void SetProgress(float progress)
    {
        float p = Mathf.Clamp01(progress);
        if (slider != null) slider.value = p;
        if (progressText != null) progressText.text = $"{p:P0}";
    }

    void OnLoadProgress(SceneLoadProgressEvent e) => SetProgress(e.Progress);
    void OnLoadComplete(SceneLoadCompleteEvent e) => SetProgress(1f);

    public void FadeIn()
    {
        fadeHelper?.Run(fadeHelper.TweenAlpha(canvasGroup, 0f, 1f, 0.3f));
    }

    /// <summary>淡出完成后自动 Pop 销毁自身。</summary>
    public void FadeOut()
    {
        fadeHelper?.Run(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        yield return fadeHelper.TweenAlpha(canvasGroup, 1f, 0f, 0.3f);
        if (panelManager != null) panelManager.Pop();
    }

    public override void Exit()
    {
        EventDispatcher.Unsubscribe<SceneLoadProgressEvent>(OnLoadProgress);
        EventDispatcher.Unsubscribe<SceneLoadCompleteEvent>(OnLoadComplete);
        base.Exit();
    }
}
