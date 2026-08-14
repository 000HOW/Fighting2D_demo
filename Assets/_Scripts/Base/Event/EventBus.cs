using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Event
{
    /// <summary>
    /// 类型安全的事件总线，支持父子级联冒泡与 Dispose 生命周期管理。
    /// 完全基于泛型，不含任何字符串事件机制。
    /// </summary>
    public class EventBus : IDisposable
    {
        // 内部存储：以事件类型为键，值存储 List<Action<T>>，通过 object 统一装箱避免开多个 Dictionary。
        // 触发时直接强转回 List<Action<T>>，无装箱开销。
        private readonly Dictionary<Type, object> _handlers = new Dictionary<Type, object>();

        // 父总线引用，形成单向冒泡链（子 → 父）
        private EventBus _parent;

        // 释放标记
        private bool _disposed;

        // 全局根总线实例
        private static readonly EventBus _global = new EventBus(null);

        /// <summary>
        /// 全局根总线，无父总线，生命周期与应用一致。
        /// </summary>
        public static EventBus Global => _global;

        /// <summary>
        /// 创建一个无父总线的 EventBus 实例。
        /// </summary>
        public EventBus() : this(null) { }

        /// <summary>
        /// 创建一个 EventBus 实例，并指定父总线。
        /// 事件触发时将先执行本地回调，然后冒泡到父总线。
        /// </summary>
        /// <param name="parent">父总线实例，可为 null。</param>
        public EventBus(EventBus parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型（可为 class 或 struct）。</typeparam>
        /// <param name="handler">事件回调委托。</param>
        /// <remarks>
        /// 重要说明（委托注销限制）：
        /// 由于 C# 委托相等性基于引用（target 对象 + method 指针），
        /// 若要成功通过 Unsubscribe 注销，必须传入与 Subscribe 时完全相同的委托实例。
        /// 例如：
        ///   - 直接使用方法引用：Subscribe<MyEvent>(OnMyEvent) / Unsubscribe<MyEvent>(OnMyEvent) ✓
        ///   - 将 Lambda 存入字段/变量后复用：var h = new Action<MyEvent>(e => ...); Subscribe(h); Unsubscribe(h); ✓
        ///   - 在 Unsubscribe 中写新的 Lambda：Unsubscribe<MyEvent>(e => ...) ✗ 无法匹配
        /// </remarks>
        public void Subscribe<T>(Action<T> handler)
        {
            if (_disposed)
            {
                Debug.LogWarning("[EventBus] 尝试在已释放的 EventBus 上执行 Subscribe。");
                return;
            }

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var listObj))
            {
                // 初始容量设为 4，减少频繁扩容
                var list = new List<Action<T>>(4);
                list.Add(handler);
                _handlers[type] = list;
            }
            else
            {
                var list = (List<Action<T>>)listObj;
                list.Add(handler);
            }
        }

        /// <summary>
        /// 取消订阅指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="handler">之前订阅时使用的同一个委托实例。</param>
        /// <remarks>
        /// 必须传入与 Subscribe 时完全相同的委托实例。
        /// C# 委托相等性取决于 target 对象和 method 指针，
        /// 匿名 Lambda（即使方法体相同）会被视为不同的委托实例。
        /// </remarks>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (_disposed) return;

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var listObj))
            {
                var list = (List<Action<T>>)listObj;
                list.Remove(handler);
                // 清理空列表，防止内存泄漏
                if (list.Count == 0)
                {
                    _handlers.Remove(type);
                }
            }
        }

        /// <summary>
        /// 触发指定类型的事件，依次调用所有已注册的回调。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="event">事件实例。</param>
        /// <param name="bubble">
        /// 是否允许事件冒泡至父总线，默认为 true。
        /// 设为 false 可阻止本次事件向上传播。
        /// </param>
        /// <remarks>
        /// 执行顺序：
        /// 1. 遍历当前总线的所有已注册回调（异常隔离，单个回调异常不影响其他回调）。
        /// 2. 若 bubble == true 且存在父总线，将同一事件递交给父总线。
        /// 3. 父总线以相同规则继续处理并冒泡。
        /// </remarks>
        public void Fire<T>(T @event, bool bubble = true)
        {
            if (_disposed)
            {
                Debug.LogWarning("[EventBus] 尝试在已释放的 EventBus 上执行 Fire。");
                return;
            }

            var type = typeof(T);

            // 1) 执行本地回调
            if (_handlers.TryGetValue(type, out var listObj))
            {
                var list = (List<Action<T>>)listObj;

                // 使用 for 循环代替 foreach，避免枚举器 GC 分配
                // 注意：若回调在遍历过程中修改了订阅列表（增删），行为与大多数事件系统一致，不保证确定性。
                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        list[i](@event);
                    }
                    catch (Exception ex)
                    {
                        // 异常隔离：单个回调异常不影响其他回调及冒泡流程
                        Debug.LogError($"[EventBus] 事件 '{type.Name}' 的回调执行异常：{ex}");
                    }
                }
            }

            // 2) 冒泡到父总线（递归）
            // 内部冒泡调用固定传递 bubble: true，确保父总线继续正常冒泡。
            // 调用方通过 Fire(@event, bubble: false) 可完全阻止此次事件向上传播。
            if (bubble && _parent != null)
            {
                _parent.Fire(@event, bubble: true);
            }
        }

        /// <summary>
        /// 返回当前总线的调试信息字符串，包含所有已注册的事件类型及订阅者数量。
        /// </summary>
        public string DebugInfo()
        {
            if (_disposed)
                return "[EventBus] 当前总线已释放。";

            if (_handlers.Count == 0)
                return "[EventBus] 未注册任何事件。";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[EventBus] 已注册事件（{_handlers.Count} 种类型）：");
            foreach (var kvp in _handlers)
            {
                // List<Action<T>> 实现了非泛型 IList，可直接获取 Count，避免反射
                int count = ((System.Collections.IList)kvp.Value).Count;
                sb.AppendLine($"  - {kvp.Key.Name}: {count} 个订阅者");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 释放当前总线：清空所有订阅、断开父总线引用、标记为已释放。
        /// 释放后调用 Subscribe/Fire 会输出警告。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _handlers.Clear();
            _parent = null;
        }

        /// <summary>
        /// 将当前 EventBus 实例绑定到指定 GameObject 的生命周期。
        /// 当该 GameObject 被销毁时（OnDestroy），总线会自动释放。
        /// </summary>
        /// <param name="gameObject">目标 GameObject。</param>
        /// <returns>EventBusBinder 组件实例，可用于解绑或检查状态。</returns>
        public EventBusBinder BindTo(GameObject gameObject)
        {
            if (_disposed)
            {
                Debug.LogWarning("[EventBus] 无法将已释放的 EventBus 绑定到 GameObject。");
                return null;
            }

            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            var binding = gameObject.GetComponent<EventBusBinder>();
            if (binding == null)
            {
                binding = gameObject.AddComponent<EventBusBinder>();
            }
            binding.Bind(this);
            return binding;
        }
    }
}
