using System;
using Cysharp.Threading.Tasks;

namespace Sumorin.GameFramework.DDDCore
{
	/// <summary>
	/// 事件訂閱工具介面
	/// </summary>
	public interface ISubscriber
	{
		/// <summary>
		/// 訂閱同步事件
		/// </summary>
		/// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
		/// <param name="handler">事件處理器</param>
		/// <param name="filter">事件過濾器，可選</param>
		/// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
		IDisposable Subscribe<TEvent>(Action<TEvent> handler, Predicate<TEvent> filter = null) where TEvent : IEvent;

		/// <summary>
		/// 取消訂閱同步事件
		/// </summary>
		/// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
		/// <param name="handler">要取消訂閱的事件處理器</param>
		void UnSubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;

		/// <summary>
		/// 訂閱非同步事件
		/// </summary>
		/// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
		/// <param name="handler">非同步事件處理器</param>
		/// <param name="filter">事件過濾器，可選</param>
		/// <returns>訂閱的 Disposable，呼叫 Dispose 可取消訂閱</returns>
		IDisposable SubscribeAsync<TEvent>(Func<TEvent, UniTask> handler, Predicate<TEvent> filter = null) where TEvent : IEvent;

		/// <summary>
		/// 取消訂閱非同步事件
		/// </summary>
		/// <typeparam name="TEvent">事件類型，必須實作 IEvent</typeparam>
		/// <param name="handler">要取消訂閱的非同步事件處理器</param>
		void UnSubscribeAsync<TEvent>(Func<TEvent, UniTask> handler) where TEvent : IEvent;
	}
}
