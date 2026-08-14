using System.Collections.Generic;
using UniRx;
using Zenject;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Domain 的查詢入口，純轉發不含邏輯
	/// </summary>
	public class BuffQueryService: IBuffValueService, IBuffQueryService
	{
		[Inject]
		private IBuffController controller;

	#region IBuffQueryService Members
		/// <inheritdoc />
		public BuffInfo? GetBuffInfo(string buffId) => controller.GetBuffInfo(buffId);

		/// <inheritdoc />
		public List<BuffInfo> GetBuffInfosByOwner(string ownerId) => controller.GetBuffInfosByOwner(ownerId);
	#endregion

	#region IBuffValueService Members
		/// <inheritdoc />
		public IReadOnlyReactiveProperty<int> ObserveStackCount(string buffId) => controller.ObserveStackCount(buffId);

		/// <inheritdoc />
		public IReadOnlyReactiveProperty<float> ObserveLifetime(string buffId) => controller.ObserveLifetime(buffId);
	#endregion
	}
}