using System.Collections.Generic;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔 Domain 給 Presenter 的查詢介面，回傳一般值
	/// </summary>
	public interface ISaveQueryService
	{
		/// <inheritdoc cref="ISaveController.GetSlots" />
		IReadOnlyList<SaveSlotInfo> GetSlots();
	}
}