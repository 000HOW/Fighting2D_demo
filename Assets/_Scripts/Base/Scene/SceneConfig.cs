using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFramework.Scene
{
    /// <summary>
    /// 场景管理器的 ScriptableObject 配置资产。
    ///
    /// 创建方式：Project 窗口右键 → Create → GameFramework → Scene Config
    /// 默认资源路径：Resources/SceneConfig（SceneManager 惰性初始化时自动加载）
    /// </summary>
    [CreateAssetMenu(fileName = "SceneConfig", menuName = "GameFramework/Scene Config", order = 100)]
    public class SceneConfig : ScriptableObject
    {
        [Header("Transition UI")]
        [Tooltip("是否启用过渡 UI 表现。上层据此判断是否应展示 loading 画面。\n" +
                 "无论此开关为何值，场景生命周期事件都会正常广播。")]
        [SerializeField] private bool _enableTransitionUI = false;

        [Header("Default Load Mode")]
        [Tooltip("默认的场景加载模式。")]
        [SerializeField] private LoadSceneMode _defaultLoadMode = LoadSceneMode.Single;

        /// <summary>
        /// 是否启用过渡 UI 表现。
        /// 框架不提供 UI 实现，上层自行检查此开关决定是否展示 loading 画面。
        /// </summary>
        public bool EnableTransitionUI => _enableTransitionUI;

        /// <summary>
        /// 默认的场景加载模式。
        /// </summary>
        public LoadSceneMode DefaultLoadMode => _defaultLoadMode;
    }
}
