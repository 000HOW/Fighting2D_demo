using UnityEngine;

/// <summary>
/// Boss 普通火球（Custom 技能状态）：施法动画期间到 fireDelay 在口部生成火球实体并注入 FireballInitData。
///  - 火球朝发射时锁定的玩家位置直线飞（FireballProjectile 自管移动/扫描/结算/回池）
///  - 发射一次（哨兵防重发），随后等待收招完成回 Idle
/// 无 SO 运行时字段：施放计时走 brain.CurrentCast.castTimer（哨兵双阶段复用）。
/// </summary>
[CreateAssetMenu(fileName = "FireballStateData", menuName = "Enemy/FireballStateData")]
public class FireballStateData : BaseCustomStateData
{
    [Header("火球预制体")]
    [Tooltip("火球预制体（挂 FireballProjectile）")] public GameObject fireballPrefab;
    [Tooltip("口部发射偏移（x 随 facingDir 镜像）")] public Vector2 muzzleOffset = new Vector2(0.6f, 0.3f);

    [Header("施法节奏")]
    [Tooltip("进入状态后多少秒发射火球")] public float fireDelay = 0.4f;
    [Tooltip("总施法时长（发射后收招，到时回 Idle）")] public float castDuration = 0.9f;

    [Header("弹道参数（注入 FireballInitData）")]
    [Tooltip("火球飞行速度（米/秒）")] public float projectileSpeed = 7f;
    [Tooltip("火球基础伤害")] public float projectileDamage = 15f;
    [Tooltip("火球伤害类型（决定受击反应）")] public DamageType damageType = DamageType.Tap;
    [Tooltip("障碍层（火球命中即爆炸消除，不结算）")] public LayerMask obstacleLayer;
    [Tooltip("火球存活秒数（超时消除）")] public float projectileLifetime = 3f;
    [Tooltip("火球最大飞行距离（超距消除）")] public float projectileMaxDistance = 10f;
    [Tooltip("火球命中爆炸特效（独立配置：新建 fxType=FireballExplosion 的 FXConfigSO；不复用普通攻击命中特效 CharacterSO.hitImpactFX）")] public FXConfigSO hitFX;

    public override void OnEntryStart(Blackboard bb)
    {
        var cur = GetBrain(bb)?.CurrentCast;
        if (cur != null) cur.castTimer = 0f;
    }

    public override void OnExitStart(Blackboard bb) { }
    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        var cur = GetBrain(bb)?.CurrentCast;
        if (cur == null) return;

        if (cur.castTimer >= 0f)
        {
            // 阶段 1：未发射——计时到 fireDelay 发射并置哨兵（防重发）
            if (cur.castTimer >= fireDelay)
            {
                Fire(bb);
                cur.castTimer = -1f;
            }
            else
            {
                cur.castTimer += Time.fixedDeltaTime;
            }
        }
        else
        {
            // 阶段 2：已发射——收招倒计时，到时回 Idle
            cur.castTimer -= Time.fixedDeltaTime;
            if (cur.castTimer <= -castDuration) Drop(bb);
        }
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        // 施法定身：水平阻尼到 0，保留重力（施法不走路）
        var rt = bb.characterRunTimeData;
        bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
        bb.readytoApply.exp_VerticalVelocity = rt.verticalVelocity;
    }

    /// <summary>发射火球：口部生成预制体并注入 FireballInitData（走 FXManager 受管实体池）</summary>
    void Fire(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        var brain = GetBrain(bb);
        if (brain == null || rt.self == null) return;

        Vector3 muzzle = rt.self.transform.position
            + new Vector3(muzzleOffset.x * rt.facingDir, muzzleOffset.y, 0f);

        // 锁定发射时刻的玩家位置（火球朝该点直线飞，不追踪）
        Vector2 locked = brain.Target != null
            ? (Vector2)brain.Target.position
            : (Vector2)muzzle + Vector2.right * rt.facingDir;
        Vector2 dir = (locked - (Vector2)muzzle).normalized;
        if (dir == Vector2.zero) dir = Vector2.right * rt.facingDir;

        if (hitFX == null)
            Debug.LogWarning("[FireballStateData] 未配置 hitFX（火球爆炸特效，fxType=FireballExplosion），命中将无爆炸表现。", this);

        var data = new FireballInitData(
            direction: dir,
            targetPoint: locked,
            speed: projectileSpeed,
            damageType: damageType,
            baseValue: projectileDamage,
            attacker: rt.self,                       // 攻击者 = Boss 本体（受击归属/方向）
            targetLayer: brain.profile != null ? brain.profile.targetLayer : 0,
            obstacleLayer: obstacleLayer,
            lifetime: projectileLifetime,
            maxDistance: projectileMaxDistance,
            hitFX: hitFX);                           // 火球爆炸特效独立配置，不复用普通攻击命中特效 hitImpactFX

        FXManager.Instance.SpawnFireball(fireballPrefab, muzzle, data);
    }

    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }

    static AI_EnomyBrain GetBrain(Blackboard bb)
        => bb.characterRunTimeData.self != null
            ? bb.characterRunTimeData.self.GetComponent<AI_EnomyBrain>() : null;
}
