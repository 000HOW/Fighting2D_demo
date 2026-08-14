using UnityEngine;

/// <summary>
/// 场景跳转触发器：挂载在 isTrigger 的 Collider2D 上。
/// 玩家（含 CharacterControler）进入后弹出确认弹窗，确认后切换目标场景；
/// 离开范围时自动关闭弹窗。
/// </summary>
public class ScenePortal : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("目标场景的 Addressables Key（SceneKeys：Start / Main / Boss）")]
    [SerializeField] string targetSceneKey = SceneKeys.Boss;

    [Header("提示与按键")]
    [SerializeField] string prompt = "是否前往目标场景？";
    [SerializeField] KeyCode confirmKey = KeyCode.E;
    [SerializeField] KeyCode cancelKey = KeyCode.Q;

    [Header("其他")]
    [SerializeField] bool once = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CharacterControler>() == null) return;
        SceneJumpManager.Instance?.Request(new SceneJumpRequest
        {
            TargetSceneKey = targetSceneKey,
            Prompt = prompt,
            ConfirmKey = confirmKey,
            CancelKey = cancelKey,
            Once = once
        });
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<CharacterControler>() == null) return;
        SceneJumpManager.Instance?.Cancel();
    }
}
