using UnityEngine;

/// <summary>
/// 场景跳转管理器（单例，挂 GameRoot 下，DontDestroyOnLoad）。
/// 职责：进入场景触发器后显示确认弹窗，确认（按钮或按键）后异步切换场景。
/// </summary>
public class SceneJumpManager : MonoBehaviour
{
    public static SceneJumpManager Instance { get; private set; }

    SceneJumpPanel curPanel;
    SceneJumpRequest curRequest;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 按键监听（BasePanel 非 MonoBehaviour，故在此驱动）：
    /// 确认键 → 跳转；取消键 → 关闭弹窗。
    /// </summary>
    void Update()
    {
        // curRequest 与 curPanel 生命周期同步（Request 一起赋值、Cancel 一起清空），
        // 且 SceneJumpRequest 为 struct 无法与 null 比较，故仅以 curPanel 判断。
        if (curPanel == null) return;

        if (Input.GetKeyDown(curRequest.ConfirmKey))
            ConfirmJump(curRequest);
        else if (Input.GetKeyDown(curRequest.CancelKey))
            Cancel();
    }

    /// <summary>进入触发器时调用：弹出确认框（防重复弹窗）。</summary>
    public void Request(SceneJumpRequest request)
    {
        if (curPanel != null) return;

        if (GlobalUILayer.Instance == null)
        {
            Debug.LogWarning("[SceneJumpManager] GlobalUILayer 未初始化，无法显示跳转弹窗。");
            return;
        }

        curRequest = request;
        curPanel = new SceneJumpPanel(request);
        curPanel.OnConfirm = () => ConfirmJump(request);
        GlobalUILayer.Instance.Push(curPanel);
    }

    /// <summary>离开触发器 / 取消时调用：关闭弹窗。</summary>
    public void Cancel()
    {
        if (curPanel == null) return;
        curPanel = null;
        curRequest = default;
        GlobalUILayer.Instance.Pop();
    }

    async void ConfirmJump(SceneJumpRequest request)
    {
        Cancel();

        var scene = SceneRegistry.Create(request.TargetSceneKey);
        if (scene == null)
        {
            Debug.LogError($"[SceneJumpManager] 无法创建目标场景: key='{request.TargetSceneKey}'");
            return;
        }

        await GameRoot.Instance.sceneSystem.SetSceneAsync(scene);
    }
}
