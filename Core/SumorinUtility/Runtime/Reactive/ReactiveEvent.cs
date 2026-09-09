using System;
using R3;

namespace Sumorin.SumorinUtility
{
	/// <summary>
	/// 響應式事件，結合 Subject 的觸發能力與 Observable 的訂閱介面
	/// </summary>
	/// <typeparam name="T">事件資料型別</typeparam>
	public sealed class ReactiveEvent<T>: Observable<T>, IDisposable
	{
		private readonly Subject<T> subject = new();

		/// <summary>
		/// 觸發事件
		/// </summary>
		/// <param name="value">事件資料</param>
		public void Invoke(T value)
		{
			subject.OnNext(value);
		}

		/// <summary>
		/// 釋放資源
		/// </summary>
		public void Dispose()
		{
			subject.Dispose();
		}

		/// <inheritdoc />
		protected override IDisposable SubscribeCore(Observer<T> observer)
		{
			return subject.Subscribe(observer);
		}
	}
}