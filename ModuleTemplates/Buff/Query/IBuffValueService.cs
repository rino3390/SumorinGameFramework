using UniRx;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Domain 的值面，供 View 訂閱
	/// </summary>
	/// <remarks>
	///     Buff 的生成與回收由 Flow 收到 <see cref="BuffApplied" /> 與 <see cref="BuffRemoved" /> 後交給 Presenter 處理。
	///     本介面只提供單一 Buff 的值綁定。
	/// </remarks>
	public interface IBuffValueService
	{
		/// <inheritdoc cref="IBuffController.ObserveStackCount" />
		IReadOnlyReactiveProperty<int> ObserveStackCount(string buffId);

		/// <inheritdoc cref="IBuffController.ObserveLifetime" />
		IReadOnlyReactiveProperty<float> ObserveLifetime(string buffId);
	}
}