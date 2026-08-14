using UnityEngine;

/// <summary>
/// 火球实体脚本（挂在火球预制体上，自包含生命周期）：
///  - 朝发射时锁定的玩家位置直线飞（方向/速度由 FireballInitData 注入）
///  - 生成时按飞行方向翻转精灵（预制体默认朝右，Boss 朝左发射时 flipX）
///  - 每帧头部 OverlapCircle 双档扫描：
///      玩家层   → 爆炸特效 + IDamageable.TakeDamage 结算 + 消除（一发一结算 hasHit）
///      障碍层   → 爆炸特效 + 消除（被障碍阻挡命中即爆炸）
///  - 超时（lifetime）/ 超距（maxDistance）→ 消除
///  - 消除统一回调回池（FXManager 实体池注入的回调；无池兜底时自毁）
/// </summary>
public class FireballProjectile : MonoBehaviour
{
    [Header("扫描")]
    [Tooltip("头部扫描半径（命中判定）")] public float scanRadius = 0.3f;

    FireballInitData data;
    SpriteRenderer sr;
    System.Action onFinished;
    bool hasHit;
    float elapsed;
    float traveled;

    /// <summary>初始化（由 FXManager.SpawnFireball 调用并注入回池回调）</summary>
    public void Init(FireballInitData initData, System.Action finished)
    {
        data = initData;
        onFinished = finished;
        hasHit = false;
        elapsed = 0f;
        traveled = 0f;

        // 火球精灵默认朝右（SpriteRenderer.flipX=0）；按飞行方向水平翻转，
        // 否则 Boss 朝左发射时火球向左飞但精灵仍朝右（视觉朝向 bug）。
        // 只在 Init 设置一次：直线弹道方向固定；池化复用每次生成都会重新 Init → 方向正确重置。
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = data.direction.x < 0f;
    }

    void Update()
    {
        if (hasHit) return;

        // 1. 移动：朝锁定方向直线飞
        Vector3 step = (Vector3)(data.direction * data.speed * Time.deltaTime);
        transform.position += step;
        traveled += step.magnitude;

        // 2. 超时 / 超距 → 消除
        elapsed += Time.deltaTime;
        if (elapsed >= data.lifetime || traveled >= data.maxDistance)
        {
            Finish();
            return;
        }

        // 3. 头部扫描：玩家层优先（结算伤害），障碍层其次（阻挡爆炸）
        Vector2 head = (Vector2)transform.position;
        if (ScanMask(head, data.targetLayer))
        {
            Explode();
            ResolvePlayerHit(head);
            Finish();
            return;
        }
        if (ScanMask(head, data.obstacleLayer))
        {
            Explode();
            Finish();
            return;
        }
    }

    /// <summary>扫描指定层是否命中（mask 为 0 视为未配置，跳过）</summary>
    bool ScanMask(Vector2 center, LayerMask mask)
    {
        if (mask == 0) return false;
        return Physics2D.OverlapCircle(center, scanRadius, mask) != null;
    }

    /// <summary>对命中的玩家结算伤害（已死亡目标 TakeDamage 返回 false，火球仍消耗）</summary>
    void ResolvePlayerHit(Vector2 head)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(head, scanRadius, data.targetLayer);
        if (hits.Length == 0) return;
        foreach (var c in hits)
        {
            if (c.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(new DamageData
                {
                    damageType = data.damageType,
                    baseValue = data.baseValue,
                    Attacker = data.attacker,
                    // 受击击退/闪红方向：命中瞬间取「目标 - 火球」方向（DamageReceiver 会归一化）
                    hitDirection = (Vector2)(c.transform.position - transform.position),
                });
            }
        }
    }

    /// <summary>命中爆炸特效</summary>
    void Explode()
    {
        if (data.hitFX == null) return;
        int facing = data.direction.x >= 0f ? 1 : -1;
        FXManager.Instance?.PlayFX(data.hitFX, transform.position, facing);
    }

    /// <summary>消除：回池（无池则自毁兜底）</summary>
    void Finish()
    {
        if (hasHit) return;
        hasHit = true;
        if (onFinished != null) onFinished.Invoke();
        else Destroy(gameObject);
    }
}
