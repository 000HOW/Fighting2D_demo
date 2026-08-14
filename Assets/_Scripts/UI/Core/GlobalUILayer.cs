using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局 UI 层（跨场景常驻，挂载于 GameRoot 下）。
///
/// 职责：
///  - 提供独立于各业务场景面板栈的常驻 Canvas（最高 sortingOrder）
///  - 承载系统级 UI：Loading 过渡、场景跳转确认弹窗等
///
/// 为什么需要：
///   UIManager.GetSingleUI 依赖场景中名为 "Canvas" 的物体，场景切换时该物体随场景销毁，
///   因此系统级 UI 不能依赖业务场景的 Canvas。
/// </summary>
public class GlobalUILayer : MonoBehaviour
{
    public static GlobalUILayer Instance { get; private set; }

    PanelManager panelManager;
    GameObject canvasRoot;

    /// <summary>全局 UI 层的中间事件总线（面板子总线冒泡至此 → Global）。</summary>
    public EventBus EventBus => panelManager?.EventBus;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateCanvas();
        panelManager = new PanelManager(canvasRoot.transform);
    }

    void CreateCanvas()
    {
        canvasRoot = new GameObject("GlobalCanvas");
        canvasRoot.transform.SetParent(transform, false);

        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasRoot.AddComponent<GraphicRaycaster>();

        // canvasRoot 已作为子物体挂在 DontDestroyOnLoad 的根节点（transform）下，
        // 会随根节点跨场景常驻，无需也不能再对它调用 DontDestroyOnLoad，
        // 否则会报 "DontDestroyOnLoad only works for root GameObjects"。
    }

    public void Push(BasePanel panel)
    {
        panelManager?.Push(panel);
    }
    public void Pop() => panelManager?.Pop();
    public void Clear() => panelManager?.Clear();
}
