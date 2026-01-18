using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// 事件發布工具，提供簡化的事件發布 API
    /// </summary>
    public class Publisher
    {
        private readonly IEventBus eventBus;

        public Publisher(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        /// <summary>
        /// 發布同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="evt">要發布的事件</param>
        public void Publish<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
            {
                Debug.LogError($"Publisher.Publish<{typeof(TEvent).Name}>: evt 不可為 null");
                return;
            }

            eventBus.Publish(evt);
        }

        /// <summary>
        /// 發布非同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="evt">要發布的事件</param>
        /// <returns>非同步任務</returns>
        public UniTask PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent
        {
            if (evt == null)
            {
                Debug.LogError($"Publisher.PublishAsync<{typeof(TEvent).Name}>: evt 不可為 null");
                return UniTask.CompletedTask;
            }

            return eventBus.PublishAsync(evt);
        }
    }
}
