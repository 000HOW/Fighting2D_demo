using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// 业务层场景切换门面。
///  - SetScene：同步切换（兼容旧调用）。
///  - SetSceneAsync：异步切换（接入 GameFramework.Scene.SceneManager + Loading 过渡 + 跨场景玩家数据保护）。
/// </summary>
public class SceneSystem
{
    BaseScene CurrentScene;
    PanelManager Cur_PanelManager;
    public EventBus GetCur_Eventbus()=>Cur_PanelManager.EventBus;

    /// <summary>过场参数配置（惰性加载 Resources/SceneTransitionConfig，缺失时用默认值）。</summary>
    GameFramework.Scene.SceneTransitionConfig transitionConfig;
    GameFramework.Scene.SceneTransitionConfig GetTransitionConfig()
    {
        if (transitionConfig == null)
        {
            transitionConfig = Resources.Load<GameFramework.Scene.SceneTransitionConfig>("SceneTransitionConfig");
            if (transitionConfig == null)
            {
                transitionConfig = ScriptableObject.CreateInstance<GameFramework.Scene.SceneTransitionConfig>();
                Debug.LogWarning("[SceneSystem] 未找到 Resources/SceneTransitionConfig 资产，过场使用默认参数。");
            }
        }
        return transitionConfig;
    }

    public void SetScene(BaseScene new_scene)
    {
        CurrentScene?.OnExit();
        CurrentScene = new_scene;
        CurrentScene?.OnEntry();
        Cur_PanelManager = CurrentScene.GetPanelManager();
    }

    /// <summary>
    /// 异步切换场景：旧场景退出 → 玩家数据快照 → Loading 过渡 → Addressables 异步加载 → 新场景逻辑进入 → 淡出。
    /// </summary>
    public async Task SetSceneAsync(BaseScene new_scene, string[] preload = null, IProgress<float> uiProgress = null)
    {
        if (new_scene == null)
        {
            Debug.LogError("[SceneSystem] SetSceneAsync: new_scene is null.");
            return;
        }

        // 目标场景已是当前激活场景 → 短路，跳过 Unity 场景加载（避免重复加载）
        string sceneName = SceneKeys.SceneNameOf(new_scene.SceneKey);
        bool skipLoad = !string.IsNullOrEmpty(sceneName) && UnitySceneManager.GetActiveScene().name == sceneName;

        CurrentScene?.OnExit();

        // 跨场景保护：切换前抓取玩家运行时数据（由 PlayerSessionBridge 在目标场景恢复）
        if (PlayerSession.Instance != null)
        {
            var ctrl = UnityEngine.Object.FindObjectOfType<CharacterControler>();
            var sender = ctrl != null ? ctrl.GetComponent<SkillSender>() : null;
            if (ctrl != null && sender != null)
                PlayerSession.Instance.Capture(ctrl, sender);
        }

        LoadingPanel loading = null;
        if (!skipLoad && GlobalUILayer.Instance != null)
        {
            loading = new LoadingPanel();
            GlobalUILayer.Instance.Push(loading);
            loading.FadeIn();
        }

        if (!skipLoad)
        {
            var scene = await GameFramework.Scene.SceneManager.Instance.LoadSceneAsync(
                new_scene.SceneKey,
                LoadSceneMode.Single,
                new Progress<float>(p =>
                {
                    loading?.SetProgress(p);
                    uiProgress?.Report(p);
                }),
                preload);

            if (!scene.IsValid())
            {
                Debug.LogError($"[SceneSystem] 场景加载失败: key='{new_scene.SceneKey}'");
            }
        }

        CurrentScene = new_scene;
        CurrentScene?.OnEntry();
        CurrentScene?.OnEntryAsync();
        Cur_PanelManager = CurrentScene.GetPanelManager();

        if (loading != null)
        {
            loading.SetProgress(1f);
            loading.FadeOut();
        }
    }

    // ======================================================================
    // 电影式过场切换（开始游戏流程专用）
    // ======================================================================

    /// <summary>
    /// 电影式过场切换场景：后台加载（不激活）→ 并行播退场 Timeline + 显示加载进度文字 →
    /// 进度到 100% 且退场播完 → 黑幕渐变到全黑 → 激活新场景 → 黑幕更慢淡出 → 播开场 Timeline。
    /// 用于“开始游戏”流程；其它跳转仍走 <see cref="SetSceneAsync"/>。
    /// </summary>
    /// <param name="new_scene">目标逻辑场景（SceneRegistry 创建）。</param>
    /// <param name="preload">可选：激活前预加载的 Addressables 资源 Key 列表。</param>
    /// <param name="uiProgress">可选：外部进度回调（0~1，与 HUD 进度同步）。</param>
    public async Task SetSceneCinematicAsync(BaseScene new_scene, string[] preload = null, IProgress<float> uiProgress = null)
    {
        if (new_scene == null)
        {
            Debug.LogError("[SceneSystem] SetSceneCinematicAsync: new_scene is null.");
            return;
        }

        // 目标场景已是当前激活场景 → 短路跳过 Unity 场景加载（仍播动画与渐变）
        string sceneName = SceneKeys.SceneNameOf(new_scene.SceneKey);
        bool skipLoad = !string.IsNullOrEmpty(sceneName) && UnitySceneManager.GetActiveScene().name == sceneName;

        CurrentScene?.OnExit();

        // 跨场景保护：切换前抓取玩家运行时数据（旧场景此时仍加载中）
        if (PlayerSession.Instance != null)
        {
            var ctrl = UnityEngine.Object.FindObjectOfType<CharacterControler>();
            var sender = ctrl != null ? ctrl.GetComponent<SkillSender>() : null;
            if (ctrl != null && sender != null)
                PlayerSession.Instance.Capture(ctrl, sender);
        }

        var cfg = GetTransitionConfig();
        var sceneManager = GameFramework.Scene.SceneManager.Instance;

        TransitionHUD hud = null;
        Task loadTask = Task.CompletedTask;
        Task outroTask = Task.CompletedTask;

        if (!skipLoad)
        {
            // 过场 HUD：加载进度文字 + 全屏黑幕（挂 GlobalUILayer 常驻层）
            if (GlobalUILayer.Instance != null)
            {
                hud = new TransitionHUD();
                GlobalUILayer.Instance.Push(hud);
            }

            // 并行：后台加载（进度 → HUD 文字）+ 当前场景退场 Timeline
            loadTask = sceneManager.LoadSceneHeldAsync(
                new_scene.SceneKey,
                LoadSceneMode.Single,
                new Progress<float>(p =>
                {
                    hud?.SetProgress(p);
                    uiProgress?.Report(p);
                }),
                preload);

            var hook = UnityEngine.Object.FindObjectOfType<SceneTransitionHook>();
            hook?.StopLoopAnimation();
            outroTask = TimelinePlayer.PlayAndWait(hook != null ? hook.outroDirector : null);
        }

        await Task.WhenAll(loadTask, outroTask);

        if (!skipLoad)
        {
            // 进度显示到 100% 后停留一拍，让玩家看清
            await WaitSeconds(cfg.HundredPercentHoldDuration);

            // 黑幕渐变到全黑
            if (hud != null) await hud.FadeToBlack(cfg.FadeToBlackDuration, cfg.FadeCurve);

            // 全黑 Hold，完全盖住场景切换瞬间
            await WaitSeconds(cfg.HoldAtBlackDuration);

            // 真正激活 / 切换到目标场景
            await sceneManager.ActivateSceneAsync(new_scene.SceneKey);
        }

        // 新场景逻辑进入（沿用现有流程）
        CurrentScene = new_scene;
        CurrentScene?.OnEntry();
        CurrentScene?.OnEntryAsync();
        Cur_PanelManager = CurrentScene.GetPanelManager();

        if (!skipLoad)
        {
            // 等新场景首帧稳定，再更慢地淡出黑幕（遮住新场景资源 pop-in）
            await WaitFrames(cfg.SettleFramesAfterActivate);
            if (hud != null) await hud.FadeFromBlack(cfg.FadeFromBlackDuration, cfg.FadeCurve);
            GlobalUILayer.Instance?.Pop();

            // 新场景开场 Timeline
            var mainHook = UnityEngine.Object.FindObjectOfType<SceneTransitionHook>();
            await TimelinePlayer.PlayAndWait(mainHook != null ? mainHook.introDirector : null);
        }
    }

    /// <summary>按真实（unscaled）时间等待，暂停菜单等 timeScale=0 场景下依然可靠。</summary>
    static async Task WaitSeconds(float seconds)
    {
        if (seconds <= 0f) return;
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
            await Task.Yield();
    }

    /// <summary>等待若干帧（用于新场景首帧稳定）。</summary>
    static async Task WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            await Task.Yield();
    }
}
