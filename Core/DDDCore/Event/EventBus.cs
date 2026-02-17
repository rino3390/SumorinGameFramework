using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sumorin.GameFramework.DDDCore
{
    /// <summary>
    /// 事件匯流排實作，使用字典管理訂閱者
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> syncHandlers = new();
        private readonly Dictionary<Type, List<Delegate>> asyncHandlers = new();
        private readonly Dictionary<object, IDisposable> subscriptions = new();

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (!syncHandlers.TryGetValue(typeof(TEvent), out var handlers))
                return;

            foreach (var handler in handlers.ToList())
            {
                try
                {
                    ((Action<TEvent>)handler)(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <inheritdoc />
        public async UniTask PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

            if (!asyncHandlers.TryGetValue(typeof(TEvent), out var handlers))
                return;

            var tasks = new List<UniTask>();
            foreach (var handler in handlers.ToList())
            {
                tasks.Add(SafeInvokeAsync((Func<TEvent, UniTask>)handler, evt));
            }

            await UniTask.WhenAll(tasks);
        }

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, Predicate<TEvent> filter = null)
            where TEvent : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(TEvent);
            if (!syncHandlers.ContainsKey(type))
                syncHandlers[type] = new();

            Action<TEvent> wrappedHandler = filter != null
                ? evt =>
                {
                    if (filter(evt)) handler(evt);
                }
                : handler;

            syncHandlers[type].Add(wrappedHandler);

            var subscription = new Subscription(() => syncHandlers[type].Remove(wrappedHandler));
            subscriptions[handler] = subscription;
            return subscription;
        }

        /// <inheritdoc />
        public void UnSubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (subscriptions.TryGetValue(handler, out var subscription))
            {
                subscription.Dispose();
                subscriptions.Remove(handler);
            }
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler, Predicate<TEvent> filter = null)
            where TEvent : IEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var type = typeof(TEvent);
            if (!asyncHandlers.ContainsKey(type))
                asyncHandlers[type] = new();

            Func<TEvent, UniTask> wrappedHandler = filter != null
                ? async evt =>
                {
                    if (filter(evt)) await handler(evt);
                }
                : handler;

            asyncHandlers[type].Add(wrappedHandler);

            var subscription = new Subscription(() => asyncHandlers[type].Remove(wrappedHandler));
            subscriptions[handler] = subscription;
            return subscription;
        }

        /// <inheritdoc />
        public void UnSubscribeAsync<TEvent>(Func<TEvent, UniTask> handler) where TEvent : IEvent
        {
            if (subscriptions.TryGetValue(handler, out var subscription))
            {
                subscription.Dispose();
                subscriptions.Remove(handler);
            }
        }

        private async UniTask SafeInvokeAsync<TEvent>(Func<TEvent, UniTask> handler, TEvent evt)
        {
            try
            {
                await handler(evt);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private class Subscription : IDisposable
        {
            private Action unsubscribe;

            public Subscription(Action unsubscribe) => this.unsubscribe = unsubscribe;

            public void Dispose()
            {
                unsubscribe?.Invoke();
                unsubscribe = null;
            }
        }
    }
}
