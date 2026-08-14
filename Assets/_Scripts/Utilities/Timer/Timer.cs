using System;
using UnityEngine;

namespace GameFramework.Tool
{
    /// <summary>
    /// 通用计时器工具，不依赖 MonoBehaviour，提供间隔信号。
    /// 
    /// 两种信号形式：
    /// 1. OnTick / OnCompleted 回调
    /// 2. Signal Bool（每次 Tick 反转，可供外部轮询）
    /// 
    /// 使用方式：
    /// <code>
    /// var timer = new Timer(1f, maxTicks: 0);
    /// timer.OnTick += () => Debug.Log("Tick");
    /// // 在 MonoBehaviour.Update 中：
    /// timer.Tick(Time.deltaTime);
    /// </code>
    /// </summary>
    public class Timer
    {
        // ===== 核心字段 =====
        private float _interval;
        private float _elapsed;
        private int _maxTicks;      // 0 = 无限循环
        private int _currentTicks;
        private bool _isRunning;
        private bool _paused;
        private bool _signal;       // 每次 Tick 反转的 Bool 信号

        // ===== 事件 =====
        /// <summary>每次间隔到达时触发（Tick 信号）</summary>
        public event Action OnTick;

        /// <summary>达到 maxTicks 后触发（仅有限循环时）</summary>
        public event Action OnCompleted;

        // ===== 属性 =====
        /// <summary>计时器是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>计时器是否暂停中</summary>
        public bool IsPaused => _paused;

        /// <summary>每次 Tick 反转的 Bool 信号，可供外部轮询</summary>
        public bool Signal => _signal;

        /// <summary>当前间隔的进度 (0~1)</summary>
        public float Progress => _interval > 0 ? Mathf.Clamp01(_elapsed / _interval) : 0f;

        /// <summary>当前间隔已过去的时间</summary>
        public float Elapsed => _elapsed;

        /// <summary>当前间隔剩余时间</summary>
        public float Remaining => Mathf.Max(0f, _interval - _elapsed);

        /// <summary>已触发的 Tick 次数</summary>
        public int CurrentTicks => _currentTicks;

        /// <summary>计时间隔（秒）</summary>
        public float Interval => _interval;

        /// <summary>最大触发次数（0=无限循环）</summary>
        public int MaxTicks => _maxTicks;

        // ===== 构造方法 =====
        /// <summary>
        /// 创建一个计时器。
        /// </summary>
        /// <param name="interval">间隔秒数（必须 > 0）</param>
        /// <param name="maxTicks">最大触发次数，0 表示无限循环</param>
        public Timer(float interval, int maxTicks = 0)
        {
            if (interval <= 0)
                throw new ArgumentException("间隔时间必须大于 0", nameof(interval));

            _interval = interval;
            _maxTicks = maxTicks;
            _elapsed = 0f;
            _currentTicks = 0;
            _signal = false;
            _isRunning = false;
            _paused = false;
        }

        /// <summary>
        /// 创建一个计时器，并指定初始回调。
        /// </summary>
        public Timer(float interval, int maxTicks, Action onTick, Action onCompleted = null)
            : this(interval, maxTicks)
        {
            if (onTick != null)
                OnTick += onTick;
            if (onCompleted != null)
                OnCompleted += onCompleted;
        }

        // ===== 生命周期控制 =====
        /// <summary>启动计时器</summary>
        public void Start()
        {
            _isRunning = true;
            _paused = false;
        }

        /// <summary>停止计时器</summary>
        public void Stop()
        {
            _isRunning = false;
            _paused = false;
        }

        /// <summary>暂停计时器（保留已过去的时间）</summary>
        public void Pause()
        {
            if (_isRunning)
                _paused = true;
        }

        /// <summary>恢复计时器</summary>
        public void Resume()
        {
            if (_isRunning)
                _paused = false;
        }

        /// <summary>
        /// 重置计时器至初始状态。
        /// 重置后需要调用 Start() 重新启动。
        /// </summary>
        public void Reset()
        {
            _elapsed = 0f;
            _currentTicks = 0;
            _signal = false;
            _isRunning = false;
            _paused = false;
        }

        /// <summary>重置并立即启动</summary>
        public void Restart()
        {
            Reset();
            Start();
        }

        // ===== 核心驱动 =====
        /// <summary>
        /// 每帧调用，驱动计时器前进。
        /// </summary>
        /// <param name="deltaTime">本帧的时间增量（通常传入 Time.deltaTime）</param>
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _paused)
                return;

            if (deltaTime < 0f)
                return;

            _elapsed += deltaTime;

            // 使用 while 处理 deltaTime 过大时的"追帧"情况
            while (_elapsed >= _interval)
            {
                _elapsed -= _interval;

                // 反转 Bool 信号
                _signal = !_signal;

                _currentTicks++;

                // 触发 Tick 信号
                OnTick?.Invoke();

                // 检查是否达到最大触发次数
                if (_maxTicks > 0 && _currentTicks >= _maxTicks)
                {
                    _isRunning = false;
                    OnCompleted?.Invoke();
                    return;
                }
            }
        }

        // ===== 运行时配置 =====
        /// <summary>修改计时间隔（秒）</summary>
        public void SetInterval(float seconds)
        {
            if (seconds <= 0)
                throw new ArgumentException("间隔时间必须大于 0", nameof(seconds));
            _interval = seconds;
        }

        /// <summary>设置最大触发次数（0=无限循环）</summary>
        public void SetMaxTicks(int count)
        {
            _maxTicks = count;
        }
    }
}
