using System;
using Cysharp.Threading.Tasks;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// 事件匯流排介面，提供事件發布與訂閱功能
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 發布同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="evt">要發布的事件</param>
        void Publish<TEvent>(TEvent evt) where TEvent : IEvent;

        /// <summary>
        /// 發布非同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="evt">要發布的事件</param>
        /// <returns>非同步任務</returns>
        UniTask PublishAsync<TEvent>(TEvent evt) where TEvent : IEvent;

        /// <summary>
        /// 訂閱同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">事件處理器</param>
        /// <param name="filter">事件過濾器，可選</param>
        /// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler, Predicate<TEvent> filter = null) where TEvent : IEvent;

        /// <summary>
        /// 訂閱非同步事件
        /// </summary>
        /// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
        /// <param name="handler">非同步事件處理器</param>
        /// <param name="filter">事件過濾器，可選</param>
        /// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
        IDisposable SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler, Predicate<TEvent> filter = null) where TEvent : IEvent;
    }
}
