using System;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// 事件匯流排實作，封裝 MessagePipe
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly IServiceProvider serviceProvider;

        public EventBus(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

			if (serviceProvider.GetService(typeof(IPublisher<TEvent>)) is not IPublisher<TEvent> publisher)
            {
                Debug.LogError($"IPublisher<{typeof(TEvent).Name}> 未註冊，請確認 MessagePipe 配置正確");
                return;
            }

            publisher.Publish(evt);
        }

        /// <inheritdoc />
        public async UniTask PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
                throw new ArgumentNullException(nameof(evt));

			if (serviceProvider.GetService(typeof(IAsyncPublisher<TEvent>)) is not IAsyncPublisher<TEvent> publisher)
            {
                Debug.LogError($"IAsyncPublisher<{typeof(TEvent).Name}> 未註冊，請確認 MessagePipe 配置正確");
                return;
            }

            await publisher.PublishAsync(evt);
        }

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler, Predicate<TEvent> filter = null) where TEvent : IEvent
        {
			if (serviceProvider.GetService(typeof(ISubscriber<TEvent>)) is not ISubscriber<TEvent> subscriber)
            {
                Debug.LogError($"ISubscriber<{typeof(TEvent).Name}> 未註冊，請確認 MessagePipe 配置正確");
                return new EmptyDisposable();
            }

            if (filter != null)
            {
                return subscriber.Subscribe(evt =>
                {
                    if (filter(evt))
                        handler(evt);
                });
            }

            return subscriber.Subscribe(handler);
        }

        /// <inheritdoc />
        public IDisposable SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler, Predicate<TEvent> filter = null) where TEvent : IEvent
        {
			if (serviceProvider.GetService(typeof(IAsyncSubscriber<TEvent>)) is not IAsyncSubscriber<TEvent> subscriber)
            {
                Debug.LogError($"IAsyncSubscriber<{typeof(TEvent).Name}> 未註冊，請確認 MessagePipe 配置正確");
                return new EmptyDisposable();
            }

            if (filter != null)
            {
                return subscriber.Subscribe(async (evt, _) =>
                {
                    if (filter(evt))
                        await handler(evt);
                });
            }

            return subscriber.Subscribe(async (evt, _) => await handler(evt));
        }

        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
