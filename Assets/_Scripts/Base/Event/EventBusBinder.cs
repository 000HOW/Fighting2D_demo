using UnityEngine;

namespace GameFramework.Event
{
    /// <summary>
    /// MonoBehaviour 辅助组件，用于将 EventBus 实例绑定到 GameObject 的生命周期。
    /// 当 GameObject 被销毁时（OnDestroy），自动调用绑定的 EventBus.Dispose()，
    /// 从而清除该总线上的所有订阅，无需手动逐条取消订阅。
    /// 
    /// 通常不直接创建此组件，而是通过 EventBus.BindTo(GameObject) 扩展方式创建。
    /// </summary>
    public class EventBusBinder : MonoBehaviour
    {
        // 绑定的 EventBus 实例
        private EventBus _bus;

        /// <summary>
        /// 将指定的 EventBus 绑定到此组件所在 GameObject 的生命周期。
        /// </summary>
        /// <param name="bus">要绑定的 EventBus 实例。</param>
        public void Bind(EventBus bus)
        {
            _bus = bus;
        }

        /// <summary>
        /// 获取当前绑定的 EventBus 实例，若已释放或未绑定则返回 null。
        /// </summary>
        public EventBus BoundBus => _bus;

        private void OnDestroy()
        {
            if (_bus != null)
            {
                _bus.Dispose();
                _bus = null;
            }
        }
    }
}
