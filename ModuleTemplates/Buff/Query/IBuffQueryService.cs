using System.Collections.Generic;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Domain 的查詢面，供 Presenter 取用
	/// </summary>
	public interface IBuffQueryService
	{
		/// <inheritdoc cref="IBuffController.GetBuffInfo" />
		BuffInfo? GetBuffInfo(string buffId);

		/// <inheritdoc cref="IBuffController.GetBuffInfosByOwner" />
		List<BuffInfo> GetBuffInfosByOwner(string ownerId);
	}
}
