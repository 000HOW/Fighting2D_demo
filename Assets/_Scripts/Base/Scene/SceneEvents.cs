using UnityEngine.SceneManagement;

namespace GameFramework.Scene
{
    // ======================================================================
    // 场景生命周期事件
    // 所有事件通过 EventBus.Global（Events 静态类）广播，
    // 上层可选订阅，框架（如 SceneManager）不强制消费。
    // ======================================================================

    /// <summary>
    /// 场景开始加载时触发。
    /// </summary>
    public struct SceneLoadStartEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;
        /// <summary>加载模式。</summary>
        public LoadSceneMode LoadMode;
        /// <summary>是否有预加载资源列表。</summary>
        public bool HasPreload;

        public override string ToString() => $"SceneLoadStart: key='{SceneKey}', mode={LoadMode}, preload={HasPreload}";
    }

    /// <summary>
    /// 场景加载进度更新时触发（每帧多次）。
    /// </summary>
    public struct SceneLoadProgressEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;
        /// <summary>综合进度值（0 ~ 1）。含场景加载 + 资源预加载。</summary>
        public float Progress;

        public override string ToString() => $"SceneLoadProgress: key='{SceneKey}', progress={Progress:P1}";
    }

    /// <summary>
    /// 场景加载完成时触发（激活前）。
    /// </summary>
    public struct SceneLoadCompleteEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;
        /// <summary>已加载的 Scene 引用。</summary>
        public UnityEngine.SceneManagement.Scene Scene;

        public override string ToString() => $"SceneLoadComplete: key='{SceneKey}', scene='{Scene.name}'";
    }

    /// <summary>
    /// 场景被设为 ActiveScene 时触发。
    /// </summary>
    public struct SceneActivatedEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;
        /// <summary>已激活的 Scene 引用。</summary>
        public UnityEngine.SceneManagement.Scene Scene;

        public override string ToString() => $"SceneActivated: key='{SceneKey}', scene='{Scene.name}'";
    }

    /// <summary>
    /// 场景开始卸载时触发。
    /// </summary>
    public struct SceneUnloadStartEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;

        public override string ToString() => $"SceneUnloadStart: key='{SceneKey}'";
    }

    /// <summary>
    /// 场景卸载完成时触发。
    /// </summary>
    public struct SceneUnloadCompleteEvent
    {
        /// <summary>Addressables 场景 Key。</summary>
        public object SceneKey;

        public override string ToString() => $"SceneUnloadComplete: key='{SceneKey}'";
    }
}
