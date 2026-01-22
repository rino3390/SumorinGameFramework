using Cysharp.Threading.Tasks;

namespace Rino.GameFramework.DDDCore
{
	/// <summary>
	/// 事件發布介面
	/// </summary>
	public interface IPublisher
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
	}
}
