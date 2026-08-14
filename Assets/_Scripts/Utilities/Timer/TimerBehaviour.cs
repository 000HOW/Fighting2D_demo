using UnityEngine;
using UnityEngine.Events;

namespace GameFramework.Tool
{
    /// <summary>
    /// Timer 的 MonoBehaviour 封装，可将计时器挂载到场景 GameObject 上使用。
    /// 
    /// Inspector 配置：
    /// - Interval: 计时间隔（秒）
    /// - Max Ticks: 最大触发次数（0=无限循环）
    /// - Auto Start: 启动时自动开始
    /// - On Tick: 每次 Tick 触发的 UnityEvent
    /// - On Completed: 达到 Max Ticks 时触发的 UnityEvent
    /// 
    /// 运行时也可通过脚本获取 Timer 实例进行操作：
    /// <code>
    /// var tb = GetComponent&lt;TimerBehaviour&gt;();
    /// tb.StartTimer();
    /// tb.PauseTimer();
    /// Debug.Log(tb.Timer.Signal);
    /// </code>
    /// </summary>
    public class TimerBehaviour : MonoBehaviour
    {
        [Header("计时器配置")]
        [SerializeField, Tooltip("计时间隔（秒）")] private float _interval = 1f;
        [SerializeField, Tooltip("最大触发次数（0=无限循环）")] private int _maxTicks = 0;
        [SerializeField, Tooltip("启动时自动开始计时")] private bool _autoStart = true;

        [Header("事件")]
        public UnityEvent OnTick = new UnityEvent();
        public UnityEvent OnCompleted = new UnityEvent();

        // 内部的 Timer 实例
        private Timer _timer;

        /// <summary>获取内部的 Timer 实例，可直接操作所有 API</summary>
        public Timer Timer => _timer;

        // ===== MonoBehaviour 生命周期 =====

        private void Awake()
        {
            _timer = new Timer(_interval, _maxTicks);
            _timer.OnTick += OnTickHandler;
            _timer.OnCompleted += OnCompletedHandler;
        }

        private void Start()
        {
            if (_autoStart)
                _timer.Start();
        }

        private void Update()
        {
            _timer?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_timer != null)
            {
                _timer.OnTick -= OnTickHandler;
                _timer.OnCompleted -= OnCompletedHandler;
                _timer.Stop();
                _timer = null;
            }
        }

        // ===== 事件转发 =====

        private void OnTickHandler()
        {
            OnTick?.Invoke();
        }

        private void OnCompletedHandler()
        {
            OnCompleted?.Invoke();
        }

        // ===== 公开 API（Timer 操作的快捷方式） =====

        public void StartTimer() => _timer?.Start();
        public void StopTimer() => _timer?.Stop();
        public void PauseTimer() => _timer?.Pause();
        public void ResumeTimer() => _timer?.Resume();
        public void ResetTimer() => _timer?.Reset();
        public void RestartTimer() => _timer?.Restart();

        // ===== Inspector 配置变动时同步到 Timer =====

        private void OnValidate()
        {
            if (_timer != null)
            {
                _timer.SetInterval(_interval);
                _timer.SetMaxTicks(_maxTicks);
            }
        }
    }
}
