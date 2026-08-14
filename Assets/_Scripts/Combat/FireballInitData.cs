using UnityEngine;

/// <summary>
/// 火球发射时注入的数据（生成时一次写入，之后只读）：
/// 由 FireballStateData 组装（方向 = 发射时锁定的玩家位置 - 口部），
/// FireballProjectile 据此直线飞行并结算；命中爆炸特效由 FireballStateData 显式注入（独立配置，不复用普通攻击命中特效 hitImpactFX）。
/// </summary>
public readonly struct FireballInitData
{
    /// <summary>归一化飞行方向（发射时锁定，不追踪）</summary>
    public readonly Vector2 direction;
    /// <summary>发射时锁定的玩家位置（记录用/排错用）</summary>
    public readonly Vector2 targetPoint;
    /// <summary>飞行速度（米/秒）</summary>
    public readonly float speed;
    /// <summary>伤害类型（决定受击反应）</summary>
    public readonly DamageType damageType;
    /// <summary>基础伤害</summary>
    public readonly float baseValue;
    /// <summary>攻击者（Boss 本体：受击归属 / hitDirection / OnHitTaken 的 Attacker）</summary>
    public readonly GameObject attacker;
    /// <summary>命中层（玩家层，命中即结算伤害）</summary>
    public readonly LayerMask targetLayer;
    /// <summary>障碍层（命中即爆炸消除，不结算）</summary>
    public readonly LayerMask obstacleLayer;
    /// <summary>存活秒数（超时消除）</summary>
    public readonly float lifetime;
    /// <summary>最大飞行距离（超距消除）</summary>
    public readonly float maxDistance;
    /// <summary>火球命中爆炸特效（独立配置 fxType=FireballExplosion，不复用普通攻击命中特效 hitImpactFX/hitExplosionPrefab）</summary>
    public readonly FXConfigSO hitFX;

    public FireballInitData(
        Vector2 direction, Vector2 targetPoint, float speed,
        DamageType damageType, float baseValue, GameObject attacker,
        LayerMask targetLayer, LayerMask obstacleLayer,
        float lifetime, float maxDistance, FXConfigSO hitFX)
    {
        this.direction = direction;
        this.targetPoint = targetPoint;
        this.speed = speed;
        this.damageType = damageType;
        this.baseValue = baseValue;
        this.attacker = attacker;
        this.targetLayer = targetLayer;
        this.obstacleLayer = obstacleLayer;
        this.lifetime = lifetime;
        this.maxDistance = maxDistance;
        this.hitFX = hitFX;
    }
}
