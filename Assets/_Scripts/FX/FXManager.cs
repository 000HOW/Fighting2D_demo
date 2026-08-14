using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 特效管理器：对象池 + 统一排序，静态入口 FXManager.Instance
/// 注意：需要场景中放置一个挂此组件的空对象，并在 Inspector 拖入特效预制体
/// </summary>
public class FXManager : MonoBehaviour
{
    public static FXManager Instance { get; private set; }

    [Header("特效预制体（Inspector 拖引用）")]
    [SerializeField] GameObject hitExplosionPrefab;      // 普通攻击命中特效（AttackScan 命中时经 CharacterSO.hitImpactFX 触发）
    [SerializeField] GameObject fireballExplosionPrefab; // 火球命中爆炸特效（FireballProjectile 命中时经 FireballStateData.hitFX 触发）
    [SerializeField] GameObject runDustPrefab;
    [SerializeField] GameObject jumpDustPrefab;

    [Header("排序：爆炸在角色前，尾气在角色后")]
    [SerializeField] int explosionSortingOrder = 100;
    [SerializeField] int dustSortingOrder = -10;

    readonly Dictionary<FXType, ObjectPool<GameObject>> pools = new();

    // 受管实体池（弹道类，如火球）：按预制体引用分组；普通/大型火球各自一个池，共用 FireballProjectile
    readonly Dictionary<GameObject, ObjectPool<GameObject>> entityPools = new();

    void Awake()
    {
        Instance = this;
        Register(FXType.HitExplosion, hitExplosionPrefab, 10, 40);
        Register(FXType.FireballExplosion, fireballExplosionPrefab, 5, 20);
        Register(FXType.RunDust, runDustPrefab, 5, 20);
        Register(FXType.JumpDust, jumpDustPrefab, 3, 10);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Register(FXType type, GameObject prefab, int defaultCapacity, int maxSize)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[FXManager] 未配置 {type} 的特效预制体，跳过注册。");
            return;
        }
        pools[type] = new ObjectPool<GameObject>(
            () => Instantiate(prefab),
            go => go.SetActive(true),
            go => go.SetActive(false),
            Destroy,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    /// <summary>
    /// 通用入口：按配置播放一个特效。cfg 为空或未注册则忽略
    /// </summary>
    public void PlayFX(FXConfigSO cfg, Vector3 pos, int facingDir = 1)
    {
        if (cfg == null || cfg.fxType == FXType.None || !pools.TryGetValue(cfg.fxType, out var pool)) return;
        var go = pool.Get();
        int order = (cfg.fxType == FXType.HitExplosion || cfg.fxType == FXType.FireballExplosion) ? explosionSortingOrder : dustSortingOrder;
        var p = new FXPlayParams
        {
            position = pos,
            rotationZ = facingDir*cfg.localRotationZ,
            scale = new Vector3(cfg.localScale.x, cfg.localScale.y, 1f),
            playSpeed = cfg.playSpeed,
            sortingOrder = order
        };
        go.GetComponent<FXPlayer>().Play(p, _ => pool.Release(go));
    }

    #region 受管实体池（火球等自管生命周期的实体）

    /// <summary>
    /// 生成一个火球实体（普通/大型共用）：从 FXManager 实体池取实例，
    /// 注入 FireballInitData 并挂回池回调；火球自管移动/扫描/结算/命中后回池。
    /// 场景缺 FXManager 时兜底直接实例化（播完自毁，不回收），保证调试期可用。
    /// </summary>
    public GameObject SpawnFireball(GameObject prefab, Vector3 pos, FireballInitData data)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[FXManager] 未配置火球预制体，无法生成。");
            return null;
        }

        if (Instance == null)
        {
            Debug.LogWarning("[FXManager] 场景缺少 FXManager，火球将直接实例化（不回收）。");
            GameObject fallback = Instantiate(prefab, pos, Quaternion.identity);
            fallback.GetComponent<FireballProjectile>()?.Init(data, null);
            return fallback;
        }

        ObjectPool<GameObject> pool = GetEntityPool(prefab);
        GameObject go = pool.Get();
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;

        FireballProjectile proj = go.GetComponent<FireballProjectile>();
        if (proj == null)
        {
            Debug.LogError($"[FXManager] 预制体 {prefab.name} 缺少 FireballProjectile，已回收。");
            pool.Release(go);
            return null;
        }
        proj.Init(data, () => pool.Release(go));
        return go;
    }

    ObjectPool<GameObject> GetEntityPool(GameObject prefab)
    {
        if (entityPools.TryGetValue(prefab, out var pool)) return pool;

        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab),
            actionOnGet: go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            collectionCheck: true,
            defaultCapacity: 5,
            maxSize: 30);
        entityPools[prefab] = pool;
        return pool;
    }

    #endregion
}
