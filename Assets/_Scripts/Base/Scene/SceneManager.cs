using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Event;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GameFramework.Scene
{
    /// <summary>
    /// 基于 Addressables 的场景管理器（单例）。
    ///
    /// 功能：
    /// - LoadSceneAsync：异步加载场景，支持激活前资源预加载
    /// - UnloadSceneAsync：卸载场景并释放 Addressables handle
    /// - 场景生命周期事件广播（LoadStart / LoadProgress / LoadComplete / Activated / UnloadStart / UnloadComplete）
    /// - 通过 SceneConfig ScriptableObject 配置行为
    ///
    /// 惰性初始化：首次访问 Instance 时自动初始化，也可通过 Initialize(config) 显式初始化。
    /// </summary>
    public class SceneManager
    {
        // ======================================================================
        // 单例
        // ======================================================================

        private static readonly Lazy<SceneManager> _instance = new(() => new SceneManager());

        /// <summary>
        /// 场景管理器单例。首次访问触发惰性初始化。
        /// </summary>
        public static SceneManager Instance
        {
            get
            {
                var inst = _instance.Value;
                inst.EnsureInitialized();
                return inst;
            }
        }

        // ======================================================================
        // 内部状态
        // ======================================================================

        /// <summary>场景 Key → Addressables handle（用于正确释放）。</summary>
        private readonly Dictionary<object, AsyncOperationHandle<SceneInstance>> _sceneHandles = new();

        /// <summary>场景 Key → Scene 引用（用于查询）。</summary>
        private readonly Dictionary<object, UnityEngine.SceneManagement.Scene> _sceneMap = new();

        /// <summary>Scene → 场景 Key 反向映射（用于 UnloadSceneAsync(Scene)）。</summary>
        private readonly Dictionary<UnityEngine.SceneManagement.Scene, object> _sceneToKey = new();

        /// <summary>
        /// Single 模式下“等待激活”的陈旧场景 Key 缓存（key → 激活前记录的所有旧 Key）。
        /// 由 LoadSceneHeldAsync 记录，ActivateSceneAsync 激活完成后统一清理旧 handle。
        /// </summary>
        private readonly Dictionary<object, object[]> _pendingStaleKeys = new();

        /// <summary>线程锁。</summary>
        private readonly object _lock = new();

        private bool _initialized;
        private SceneConfig _config;

        // 默认配置（找不到 SceneConfig 资产时使用）
        private static readonly SceneConfig DefaultConfig = ScriptableObject.CreateInstance<SceneConfig>();

        // ======================================================================
        // 配置属性
        // ======================================================================

        /// <summary>
        /// 当前生效的配置。
        /// </summary>
        public SceneConfig Config => _config ?? DefaultConfig;

        private SceneManager() { }

        // ======================================================================
        // 初始化
        // ======================================================================

        /// <summary>
        /// 显式初始化场景管理器，传入配置资产。
        /// 可在 GameRoot.Awake() 中调用以确保有序初始化；也可以不调用，首次访问 Instance 时自动惰性初始化。
        /// </summary>
        /// <param name="config">SceneConfig ScriptableObject 资产，为 null 则尝试从 Resources 加载</param>
        public void Initialize(SceneConfig config = null)
        {
            if (_initialized) return;

            if (config != null)
            {
                _config = config;
            }
            else
            {
                // 尝试从 Resources 加载默认配置
                _config = Resources.Load<SceneConfig>("SceneConfig");
            }

            _initialized = true;
            Debug.Log($"[SceneManager] 初始化完成。Config: EnableTransitionUI={Config.EnableTransitionUI}, DefaultLoadMode={Config.DefaultLoadMode}");
        }

        /// <summary>
        /// 确保已初始化（惰性初始化入口）。
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize(null);
            }
        }

        // ======================================================================
        // 公开 API — 加载
        // ======================================================================

        /// <summary>
        /// 异步加载并激活场景（一次性完成）。
        /// 等价于 LoadSceneHeldAsync + ActivateSceneAsync，供不关心两阶段控制的调用方使用。
        /// </summary>
        public async Task<UnityEngine.SceneManagement.Scene> LoadSceneAsync(
            object key,
            LoadSceneMode mode = LoadSceneMode.Single,
            IProgress<float> progress = null,
            string[] preloadAddresses = null)
        {
            var scene = await LoadSceneHeldAsync(key, mode, progress, preloadAddresses);
            if (!scene.IsValid()) return scene;

            await ActivateSceneAsync(key, progress);
            Debug.Log($"[SceneManager] 场景加载完成: key='{key}', scene='{scene.name}'");
            return scene;
        }

        /// <summary>
        /// 激活由 LoadSceneHeldAsync 加载、尚未激活的场景（两阶段切换的第二阶段）。
        /// 激活完成后按 Single 模式清理旧场景的 Addressables handle。
        /// </summary>
        /// <param name="key">Addressables 场景 Key</param>
        public async Task ActivateSceneAsync(object key, IProgress<float> progress = null)
        {
            EnsureInitialized();

            AsyncOperationHandle<SceneInstance> handle;
            lock (_lock)
            {
                if (!_sceneHandles.TryGetValue(key, out handle))
                {
                    Debug.LogWarning($"[SceneManager] ActivateSceneAsync: 场景 '{key}' 未被追踪，无法激活。");
                    return;
                }
            }

            var sceneInstance = handle.Result;
            var scene = sceneInstance.Scene;

            // 已在激活状态（LoadSceneHeldAsync 命中“已加载”短路时）→ 无需重复激活
            if (scene.isLoaded && UnitySceneManager.GetActiveScene() == scene)
            {
                return;
            }

            // ── 激活场景 ──
            await AwaitAsyncOperation(sceneInstance.ActivateAsync());

            // ── 设为 ActiveScene ──
            UnitySceneManager.SetActiveScene(scene);

            // ── Single 模式：清理已被 Unity 自动卸载的旧场景（释放 handle + 移除追踪）──
            // 注意：必须等新场景激活完成后再释放旧 handle。activateOnLoad=false 时，
            // 新场景加载到 0.9 便暂停、旧场景尚未卸载；此刻 Release 旧 handle 会触发
            // SceneManager.UnloadSceneAsync 卸载“最后一个场景”而报错。
            // 激活完成后旧场景已被 Single 模式卸载（isLoaded==false），Release 只释放 handle。
            object[] staleKeys;
            lock (_lock)
            {
                _pendingStaleKeys.TryGetValue(key, out staleKeys);
                _pendingStaleKeys.Remove(key);
            }

            if (staleKeys != null)
            {
                foreach (var k in staleKeys)
                {
                    if (Equals(k, key)) continue;
                    lock (_lock)
                    {
                        if (_sceneHandles.TryGetValue(k, out var oldHandle))
                        {
                            _sceneHandles.Remove(k);
                            if (_sceneMap.TryGetValue(k, out var oldScene))
                            {
                                _sceneMap.Remove(k);
                                _sceneToKey.Remove(oldScene);
                            }
                            if (oldHandle.IsValid())
                            {
                                Addressables.Release(oldHandle);
                            }
                        }
                    }
                }
            }

            // ── 触发 Activated 事件 ──
            EventBus.Global.Fire(new SceneActivatedEvent
            {
                SceneKey = key,
                Scene = scene
            });

            Debug.Log($"[SceneManager] 场景激活完成: key='{key}', scene='{scene.name}'");
        }

        /// <summary>
        /// 异步加载场景但不激活（两阶段切换的第一阶段，activateOnLoad=false）。
        /// 加载到 90% + 预加载资源后停住，返回 Scene；调用方随后用 <see cref="ActivateSceneAsync"/> 激活。
        /// 进度仍会报到 1.0（供加载进度文字显示 100%）。
        /// </summary>
        /// <param name="key">Addressables 场景 Key</param>
        /// <param name="mode">
        /// 加载模式，默认为 SceneConfig 中配置的 DefaultLoadMode。
        /// Single 模式会先卸载所有已追踪场景再加载新场景（激活时才真正卸载旧场景）。
        /// </param>
        /// <param name="progress">
        /// 进度回调。
        /// 无 preloadAddresses 时：0 → 1.0（场景加载全流程）。
        /// 有 preloadAddresses 时：0 → 0.9（场景加载），0.9 → 1.0（资源预加载）。
        /// </param>
        /// <param name="preloadAddresses">
        /// 可选：场景加载到 90% 后、激活前预加载的 Addressables 资源 Key 列表。
        /// 确保场景激活时关键资源已就绪。
        /// </param>
        /// <returns>已加载的 Scene 引用；失败返回 invalid Scene。</returns>
        public async Task<UnityEngine.SceneManagement.Scene> LoadSceneHeldAsync(
            object key,
            LoadSceneMode mode = LoadSceneMode.Single,
            IProgress<float> progress = null,
            string[] preloadAddresses = null)
        {
            EnsureInitialized();

            if (key == null)
            {
                Debug.LogError("[SceneManager] LoadSceneAsync: key is null.");
                return default;
            }

            // mode 已由调用方指定（C# 默认值为 LoadSceneMode.Single），直接使用。

            // ── Single 模式：记录将被替换的旧场景，等新场景加载成功后统一清理 ──
            // 注意：不能在这里手动卸载旧场景——当旧场景是“最后一个已加载场景”时，
            // Unity 会报 “Unloading the last loaded scene ... is not supported”。
            // LoadSceneMode.Single 加载新场景时 Unity 会自动卸载旧场景，
            // 我们只需在新场景加载成功后释放旧场景的 Addressables handle 并移除追踪。
            object[] staleKeys = null;
            if (mode == LoadSceneMode.Single)
            {
                lock (_lock)
                {
                    staleKeys = _sceneMap.Keys.ToArray();
                }
            }

            // ── 检查是否已加载 ──
            lock (_lock)
            {
                if (_sceneMap.TryGetValue(key, out var existingScene) && existingScene.isLoaded)
                {
                    Debug.LogWarning($"[SceneManager] 场景 '{key}' 已加载，跳过重复加载。");
                    progress?.Report(1f);
                    return existingScene;
                }
            }

            // ── 加载场景 ──
            var hasPreload = preloadAddresses != null && preloadAddresses.Length > 0;

            // 触发 LoadStart 事件
            EventBus.Global.Fire(new SceneLoadStartEvent
            {
                SceneKey = key,
                LoadMode = mode,
                HasPreload = hasPreload
            });

            Debug.Log($"[SceneManager] 开始加载场景: key='{key}', mode={mode}, preload={(hasPreload ? preloadAddresses.Length + " assets" : "none")}");

            AsyncOperationHandle<SceneInstance> handle = default;

            try
            {
                // activateOnLoad = false：场景加载到 90% 时暂停，等待我们手动激活
                handle = Addressables.LoadSceneAsync(key, mode, activateOnLoad: false);

                // ── Phase 1: 等待场景加载到 90% ──
                while (!handle.IsDone)
                {
                    float sceneProgress = handle.PercentComplete; // 0 → ~0.9

                    // 向 IProgress 报告（有预加载时只占 90% 权重，无预加载时占 100%）
                    float weightedProgress = hasPreload ? sceneProgress * 0.9f : sceneProgress;
                    ReportProgress(progress, key, weightedProgress);

                    // 广播进度事件
                    EventBus.Global.Fire(new SceneLoadProgressEvent
                    {
                        SceneKey = key,
                        Progress = weightedProgress
                    });

                    await Task.Yield();
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[SceneManager] 场景加载失败: key='{key}', status={handle.Status}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    progress?.Report(1f);
                    return default;
                }

                var sceneInstance = handle.Result;
                var scene = sceneInstance.Scene;

                // 确保进度报告到 100%
                ReportProgress(progress, key, 1f);
                EventBus.Global.Fire(new SceneLoadProgressEvent { SceneKey = key, Progress = 1f });

                // ── 记录映射 ──
                lock (_lock)
                {
                    _sceneHandles[key] = handle;
                    _sceneMap[key] = scene;
                    _sceneToKey[scene] = key;
                }

                // ── 缓存 Single 模式的陈旧场景 Key，供 ActivateSceneAsync 激活完成后统一清理 ──
                if (mode == LoadSceneMode.Single && staleKeys != null)
                {
                    lock (_lock)
                    {
                        _pendingStaleKeys[key] = staleKeys;
                    }
                }

                // ── 触发 LoadComplete 事件（激活前）──
                EventBus.Global.Fire(new SceneLoadCompleteEvent
                {
                    SceneKey = key,
                    Scene = scene
                });

                Debug.Log($"[SceneManager] 场景已加载（未激活）: key='{key}', scene='{scene.name}'");
                return scene;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManager] 场景加载异常: key='{key}', {ex}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                progress?.Report(1f);
                return default;
            }
        }

        // ======================================================================
        // 公开 API — 卸载
        // ======================================================================

        /// <summary>
        /// 按 Addressables Key 卸载场景。
        /// </summary>
        /// <param name="key">Addressables 场景 Key</param>
        public async Task UnloadSceneAsync(object key)
        {
            EnsureInitialized();

            if (key == null)
            {
                Debug.LogError("[SceneManager] UnloadSceneAsync: key is null.");
                return;
            }

            AsyncOperationHandle<SceneInstance> handle;
            UnityEngine.SceneManagement.Scene scene;

            lock (_lock)
            {
                if (!_sceneHandles.TryGetValue(key, out handle))
                {
                    Debug.LogWarning($"[SceneManager] 场景 '{key}' 未被本管理器追踪，无法卸载。");
                    return;
                }

                scene = _sceneMap.GetValueOrDefault(key);
            }

            // 触发 UnloadStart 事件
            EventBus.Global.Fire(new SceneUnloadStartEvent { SceneKey = key });

            Debug.Log($"[SceneManager] 开始卸载场景: key='{key}'");

            try
            {
                if (handle.IsValid())
                {
                    var unloadHandle = Addressables.UnloadSceneAsync(handle, false);
                    await unloadHandle.Task;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneManager] 场景卸载异常: key='{key}', {ex}");
            }
            finally
            {
                lock (_lock)
                {
                    _sceneHandles.Remove(key);
                    _sceneMap.Remove(key);
                    if (scene.IsValid())
                    {
                        _sceneToKey.Remove(scene);
                    }
                }
            }

            // 触发 UnloadComplete 事件
            EventBus.Global.Fire(new SceneUnloadCompleteEvent { SceneKey = key });

            Debug.Log($"[SceneManager] 场景卸载完成: key='{key}'");
        }

        /// <summary>
        /// 按 Scene 引用卸载场景。
        /// </summary>
        /// <param name="scene">要卸载的场景引用</param>
        public async Task UnloadSceneAsync(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid())
            {
                Debug.LogWarning("[SceneManager] UnloadSceneAsync: scene is invalid.");
                return;
            }

            object key;
            lock (_lock)
            {
                if (!_sceneToKey.TryGetValue(scene, out key))
                {
                    Debug.LogWarning($"[SceneManager] 场景 '{scene.name}' 未被本管理器追踪，无法卸载。");
                    return;
                }
            }

            await UnloadSceneAsync(key);
        }

        // ======================================================================
        // 公开 API — 查询
        // ======================================================================

        /// <summary>
        /// 设置激活场景。
        /// </summary>
        public void SetActiveScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid())
            {
                Debug.LogWarning("[SceneManager] SetActiveScene: scene is invalid.");
                return;
            }

            UnitySceneManager.SetActiveScene(scene);

            object key;
            lock (_lock)
            {
                _sceneToKey.TryGetValue(scene, out key);
            }

            EventBus.Global.Fire(new SceneActivatedEvent
            {
                SceneKey = key ?? scene.name,
                Scene = scene
            });
        }

        /// <summary>
        /// 获取当前激活场景。
        /// </summary>
        public UnityEngine.SceneManagement.Scene GetActiveScene()
        {
            return UnitySceneManager.GetActiveScene();
        }

        /// <summary>
        /// 检查指定 Key 的场景是否已通过本管理器加载。
        /// </summary>
        public bool IsSceneLoaded(object key)
        {
            if (key == null) return false;

            lock (_lock)
            {
                return _sceneMap.TryGetValue(key, out var scene) && scene.IsValid() && scene.isLoaded;
            }
        }

        /// <summary>
        /// 获取所有已加载场景的 Addressables Key 列表。
        /// </summary>
        public IReadOnlyList<object> GetLoadedSceneKeys()
        {
            lock (_lock)
            {
                return _sceneMap.Keys.ToList();
            }
        }

        /// <summary>
        /// 获取已通过本管理器加载的场景数量。
        /// </summary>
        public int GetLoadedSceneCount()
        {
            lock (_lock)
            {
                return _sceneMap.Count;
            }
        }

        /// <summary>
        /// 按 Key 获取已加载的 Scene 引用。
        /// </summary>
        public bool TryGetScene(object key, out UnityEngine.SceneManagement.Scene scene)
        {
            lock (_lock)
            {
                return _sceneMap.TryGetValue(key, out scene) && scene.IsValid() && scene.isLoaded;
            }
        }

        // ======================================================================
        // 内部方法
        // ======================================================================

        /// <summary>
        /// 向 IProgress 报告进度（安全空检查）。
        /// </summary>
        private static void ReportProgress(IProgress<float> progress, object key, float value)
        {
            try
            {
                progress?.Report(Mathf.Clamp01(value));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneManager] IProgress.Report 异常: key='{key}', {ex.Message}");
            }
        }

        /// <summary>
        /// 将 UnityEngine.AsyncOperation 包装为 awaitable Task。
        /// Unity 的 AsyncOperation 不直接支持 await，此方法通过 TaskCompletionSource 桥接。
        /// </summary>
        private static Task AwaitAsyncOperation(AsyncOperation op)
        {
            var tcs = new TaskCompletionSource<object>();
            op.completed += _ => tcs.TrySetResult(null);
            return tcs.Task;
        }
    }
}
