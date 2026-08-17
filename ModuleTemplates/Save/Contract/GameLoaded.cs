using Sumorin.DDDCore;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔槽已載入完成
	/// </summary>
	/// <remarks>只有存檔槽的載入會發出，全域槽的載入不發出。</remarks>
	public class GameLoaded: IEvent
	{
		/// <summary>
		///     載入的存檔槽識別碼
		/// </summary>
		public string SlotId { get; }

		/// <summary>
		///     建立載入完成事實
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		public GameLoaded(string slotId)
		{
			SlotId = slotId;
		}
	}
}