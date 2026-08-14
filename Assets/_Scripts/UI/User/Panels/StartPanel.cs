using UnityEngine;
using UnityEngine.UI;
/// <summary>
///
/// </summary>
public class StartPanel : BasePanel
{
    static readonly string path = "Prefab/UI/StartPanel";//地址的最后一个是要复制的文件名称
    public StartPanel() : base(new UItype(path))
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        var btnStart = uItool.GetOrAddComponentInChildren<Button>("Start");
        btnStart?.onClick.RemoveAllListeners();
        btnStart?.onClick.AddListener(async () =>
        {
            btnStart.interactable = false; // 防重复点击
            try
            {
                await GameRoot.Instance.sceneSystem.SetSceneCinematicAsync(new MainScene());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StartPanel] 过场切换异常: {ex}");
            }
            finally
            {
                // 场景切换时 StartPanel 可能已被销毁，需判空后再访问
                if (btnStart != null)
                    btnStart.interactable = true; // 失败时可重试
            }
        });
    }
}
