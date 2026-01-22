using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// 事件訂閱工具，提供簡化的事件訂閱 API
    /// </summary>
    public class Subscriber : ISubscriber
    {
        private readonly IEventBus eventBus;
        private readonly Dictionary<object, IDisposable> subscriptions = new();

        public Subscriber(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        /// <summary>
        /// 訂閱同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">事件處理器</param>
        /// <param name="filter">事件過濾器，可選</param>
        /// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, Predicate<TEvent> filter = null) where TEvent : IEvent
        {
            var subscription = eventBus.Subscribe(handler, filter);
            subscriptions[handler] = subscription;
            return subscription;
        }

        /// <summary>
        /// 取消訂閱同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">要取消訂閱的事件處理器</param>
        public void UnSubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (subscriptions.TryGetValue(handler, out var subscription))
            {
                subscription.Dispose();
                subscriptions.Remove(handler);
            }
        }

        /// <summary>
        /// 訂閱非同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">非同步事件處理器</param>
        /// <param name="filter">事件過濾器，可選</param>
        /// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler, Predicate<TEvent> filter = null) where TEvent : IEvent
        {
            var subscription = eventBus.SubscribeAsync(handler, filter);
            subscriptions[handler] = subscription;
            return subscription;
        }

        /// <summary>
        /// 取消訂閱非同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">要取消訂閱的非同步事件處理器</param>
        public void UnSubscribeAsync<TEvent>(Func<TEvent, UniTask> handler) where TEvent : IEvent
        {
            if (subscriptions.TryGetValue(handler, out var subscription))
            {
                subscription.Dispose();
                subscriptions.Remove(handler);
            }
        }
    }
}
