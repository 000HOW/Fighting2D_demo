using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 受击闪红：收到 OnHitTaken 时把角色 sprite 染成红色，短暂后恢复原色。
/// 挂在角色根节点即可，自动寻找子级 SpriteRenderer，零侵入（不改任何现有系统）。
/// </summary>
public class HitFlashVisual : MonoBehaviour
{
    [Header("受击闪红参数")]
    [Tooltip("受击瞬间的染色颜色")]
    [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [Tooltip("红色持续时间（秒），到点恢复原色")]
    [SerializeField] float flashDuration = 0.12f;

    SpriteRenderer sr;
    Color baseColor = Color.white;
    float flashEndTime = -1f;
    bool isFlashing = false;

    /// <summary>
    /// 是否正在闪红。供同物体的其它颜色表现组件避让（如 BuffNeonVisual），
    /// 避免它们每帧把闪红颜色刷回原色导致特效不可见。
    /// </summary>
    public bool IsFlashing => isFlashing;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseColor = sr != null ? sr.color : Color.white;
    }

    void OnEnable()
    {
        EventBus.Global.Subscribe<OnHitTaken>(OnHit);
        EventBus.Global.Subscribe<EntityDiedEvent>(OnDied);   // 死亡信号（角色私有总线冒泡到 Global）
    }

    void OnDisable()
    {
        EventBus.Global.Unsubscribe<OnHitTaken>(OnHit);
        EventBus.Global.Unsubscribe<EntityDiedEvent>(OnDied);
    }

    void OnHit(OnHitTaken hit)
    {
        if (sr == null) return;
        // 只响应"自己"受击，避免玩家/敌人同时闪红
        if (hit.Target != null && hit.Target != gameObject) return;

        // 只在"未闪红"时记录恢复目标，避免连续受击把红色记为 baseColor 导致卡红
        if (!isFlashing)
            baseColor = sr.color;

        isFlashing = true;
        flashEndTime = Time.time + flashDuration;
        sr.color = flashColor;
    }

    void Update()
    {
        if (sr == null || !isFlashing) return;

        if (Time.time >= flashEndTime)
        {
            sr.color = baseColor;
            isFlashing = false;
            flashEndTime = -1f;
        }
    }

    /// <summary>
    /// 角色死亡：立即恢复原色、结束闪红，避免残留受击特效
    /// </summary>
    void OnDied(EntityDiedEvent e)
    {
        if (e.entity != gameObject) return;   // 只处理自己，避免不同角色互相干扰
        if (isFlashing)
        {
            sr.color = baseColor;
            isFlashing = false;
            flashEndTime = -1f;
        }
    }

    void OnDestroy()
    {
        EventBus.Global.Unsubscribe<OnHitTaken>(OnHit);
        EventBus.Global.Unsubscribe<EntityDiedEvent>(OnDied);
    }
}
