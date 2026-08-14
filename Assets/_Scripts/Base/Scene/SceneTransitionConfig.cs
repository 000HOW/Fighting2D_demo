using UnityEngine;

namespace GameFramework.Scene
{
    /// <summary>
    /// 场景过场过渡（黑屏渐变 + Timeline + 加载进度）的可调参数配置资产。
    ///
    /// 创建方式：Project 窗口右键 → Create → GameFramework → Scene Transition Config
    /// 默认资源路径：Resources/SceneTransitionConfig（SceneSystem 惰性加载，缺失时使用默认值）
    ///
    /// 调参原则：黑幕渐变的目的不是"好看"，而是"完全盖住场景切换的瞬间"，
    /// 因此 fadeToBlack 必须在切换前拉满到全黑并 Hold，fadeFromBlack 要比淡入黑更慢，
    /// 才能遮住新场景首帧资源 pop-in 带来的撕裂感。
    /// </summary>
    [CreateAssetMenu(fileName = "SceneTransitionConfig", menuName = "GameFramework/Scene Transition Config", order = 101)]
    public class SceneTransitionConfig : ScriptableObject
    {
        [Header("Loading Progress")]
        [Tooltip("加载进度显示到 100% 后停留的时长（秒），让玩家看清进度后再开始黑屏渐变。")]
        [SerializeField] private float _hundredPercentHoldDuration = 0.4f;

        [Header("Black Fade")]
        [Tooltip("黑屏渐变到全黑所需时长（秒）。调大可让过渡更慢。")]
        [SerializeField] private float _fadeToBlackDuration = 0.6f;

        [Tooltip("全黑停留时长（秒）。用于完全盖住场景切换瞬间，避免撕裂感。")]
        [SerializeField] private float _holdAtBlackDuration = 0.25f;

        [Tooltip("黑屏渐变回来（露出新场景）所需时长（秒）。通常比淡入黑更慢，遮住新场景资源 pop-in。")]
        [SerializeField] private float _fadeFromBlackDuration = 0.9f;

        [Tooltip("新场景激活后等待的帧数，让新场景首帧稳定后再开始淡出黑幕。")]
        [SerializeField] private int _settleFramesAfterActivate = 3;

        [Header("Easing")]
        [Tooltip("黑幕渐变缓动曲线。留空（不设曲线）时退化为线性渐变。")]
        [SerializeField] private AnimationCurve _fadeCurve;

        /// <summary>进度到 100% 后的停留时长（秒）。</summary>
        public float HundredPercentHoldDuration => _hundredPercentHoldDuration;

        /// <summary>黑幕渐变到全黑时长（秒）。</summary>
        public float FadeToBlackDuration => _fadeToBlackDuration;

        /// <summary>全黑停留时长（秒）。</summary>
        public float HoldAtBlackDuration => _holdAtBlackDuration;

        /// <summary>黑幕淡出（露出新场景）时长（秒）。</summary>
        public float FadeFromBlackDuration => _fadeFromBlackDuration;

        /// <summary>激活后等待的稳定帧数。</summary>
        public int SettleFramesAfterActivate => _settleFramesAfterActivate;

        /// <summary>黑幕渐变缓动曲线（可为 null）。</summary>
        public AnimationCurve FadeCurve => _fadeCurve;
    }
}
