using GameFramework.Event;
using UnityEngine;

/// <summary>
/// Buff 霓虹染色：任意 buff 生效时角色 sprite 循环扫色（彩虹霓虹），全部消失后恢复原色。
/// 挂在角色根节点即可，自动寻找子级 SpriteRenderer，零侵入（不改任何现有系统）。
/// </summary>
public class BuffNeonVisual : MonoBehaviour
{
    [Header("霓虹参数")]
    [Tooltip("扫色速度：完整色相环所需秒数，越小转得越快")]
    [SerializeField] float hueCycleSeconds = 2.5f;
    [SerializeField, Range(0f, 1f)] float saturation = 1f;
    [SerializeField, Range(0f, 1f)] float brightness = 1f;

    SpriteRenderer sr;
    Color baseColor = Color.white;
    int activeCount;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseColor = sr != null ? sr.color : Color.white;

        EventBus.Global.Subscribe<ModifierAddEvent>(OnBuffAdded);
        EventBus.Global.Subscribe<ModifierRemoveEvent>(OnBuffRemoved);
    }

    void OnBuffAdded(ModifierAddEvent e)
    {
        // 只响应自己（玩家）的 buff：忽略敌人（如盾牌格挡）发来的修改器事件
        if (e.owner != gameObject) return;
        activeCount++;
    }
    void OnBuffRemoved(ModifierRemoveEvent e)
    {
        if (e.owner != gameObject) return;
        activeCount--;
    }

    void Update()
    {
        if (sr == null) return;

        // 受击闪红优先：闪红期间让 HitFlashVisual 完全接管颜色，
        // 否则下面会把红色每帧刷回原色，导致受击闪红不可见。
        var flash = GetComponent<HitFlashVisual>();
        if (flash != null && flash.IsFlashing) return;

        if (activeCount <= 0)
        {
            if (sr.color != baseColor) sr.color = baseColor;   // 无 buff 恢复原色
            return;
        }

        // 彩虹扫色（霓虹）：色相随时间循环，饱和/明度固定
        float hue = (Time.time / hueCycleSeconds) % 1f;
        sr.color = Color.HSVToRGB(hue, saturation, brightness);
    }

    void OnDestroy()
    {
        EventBus.Global.Unsubscribe<ModifierAddEvent>(OnBuffAdded);
        EventBus.Global.Unsubscribe<ModifierRemoveEvent>(OnBuffRemoved);
    }
}
