using System;

namespace GameFramework.Event
{
    /// <summary>
    /// 全局事件的静态快捷入口，委托给 <see cref="EventBus.Global"/>。
    ///
    /// 日常 90% 的跨脚本通信直接用此类即可，无需创建 EventBus 实例或传引用。
    /// 需要作用域隔离 / 父子冒泡时再使用 new EventBus() 实例。
    ///
    /// 用法：
    ///   EventDispatcher.Subscribe<PlayerDiedEvent>(OnDied);
    ///   EventDispatcher.Fire(new PlayerDiedEvent(...));
    ///   EventDispatcher.Unsubscribe<PlayerDiedEvent>(OnDied);
    /// </summary>
    public static class EventDispatcher
    {
        /// <summary>
        /// 全局订阅（委托给 <see cref="EventBus.Global"/>）。
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) => EventBus.Global.Subscribe(handler);

        /// <summary>
        /// 全局取消订阅（委托给 <see cref="EventBus.Global"/>）。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) => EventBus.Global.Unsubscribe(handler);

        /// <summary>
        /// 全局触发事件（委托给 <see cref="EventBus.Global"/>）。
        /// </summary>
        public static void Fire<T>(T @event, bool bubble = true) => EventBus.Global.Fire(@event, bubble);
    }
}
