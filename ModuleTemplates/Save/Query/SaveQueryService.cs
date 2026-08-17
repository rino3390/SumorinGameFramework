using System.Collections.Generic;
using Zenject;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔 Domain 的查詢入口，純轉發不含邏輯
	/// </summary>
	/// <remarks>存檔資訊是當下快照，沒有值面成員，因此不提供 ValueService。</remarks>
	public class SaveQueryService: ISaveQueryService
	{
		[Inject]
		private ISaveController controller;

	#region ISaveQueryService Members
		/// <inheritdoc />
		public IReadOnlyList<SaveSlotInfo> GetSlots() => controller.GetSlots();
	#endregion
	}
}