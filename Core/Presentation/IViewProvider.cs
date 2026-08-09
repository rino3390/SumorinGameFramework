using UnityEngine;

namespace Sumorin.Presentation
{
	/// <summary>
	///     供應策略，抽象「View 從哪來、回哪去」，呼叫端不需知道背後是新建還是重用
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public interface IViewProvider<TView> where TView: MonoBehaviour, IBindableView
	{
		/// <summary>
		///     借出一個 View
		/// </summary>
		/// <returns>可供使用的 View</returns>
		TView Get();

		/// <summary>
		///     歸還 View，內部先呼叫 Unbind 再回收
		/// </summary>
		/// <param name="view">要歸還的 View</param>
		void Release(TView view);
	}
}