using System;

namespace GameFramework.Tool
{
    /// <summary>
    /// 计时器 Tick 事件数据。
    /// 当你需要将 Timer 信号接入 EventBus 时，将此 struct 作为 Fire<T>() 的类型参数：
    /// <code>
    /// _bus.Fire(new TimerTickEvent(timer.CurrentTicks, timer.MaxTicks, timer.Signal));
    /// </code>
    /// </summary>
    public readonly struct TimerTickEvent
    {
        /// <summary>当前已触发的 Tick 次数</summary>
        public readonly int CurrentTick;

        /// <summary>总共需要触发的次数（0 表示无限循环）</summary>
        public readonly int TotalTicks;

        /// <summary>当前 Signal Bool 状态</summary>
        public readonly bool Signal;

        public TimerTickEvent(int currentTick, int totalTicks, bool signal)
        {
            CurrentTick = currentTick;
            TotalTicks = totalTicks;
            Signal = signal;
        }
    }

    /// <summary>
    /// 计时器完成事件数据（达到 maxTicks 时触发）。
    /// <code>
    /// _bus.Fire(new TimerCompletedEvent(timer.CurrentTicks));
    /// </code>
    /// </summary>
    public readonly struct TimerCompletedEvent
    {
        /// <summary>总共触发的次数</summary>
        public readonly int TotalTicks;

        public TimerCompletedEvent(int totalTicks)
        {
            TotalTicks = totalTicks;
        }
    }
}
