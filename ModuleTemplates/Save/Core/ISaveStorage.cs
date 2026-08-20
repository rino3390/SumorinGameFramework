using System.Collections.Generic;

namespace Sumorin.Save
{
	/// <summary>
	///     存取媒介介面，預設實作為 <see cref="LocalFileSaveStorage" />，可由專案層覆寫
	/// </summary>
	/// <remarks>
	///     落盤時機由實作決定，可當場寫入或轉背景。
	///     採背景寫入的實作必須自行保證後續讀取拿得到尚未落盤的最新資料。
	///     全域槽與存檔槽建議分開存放，全域槽的識別碼為 <see cref="SaveSlot.GlobalId" />。
	/// </remarks>
	public interface ISaveStorage
	{
		/// <summary>
		///     寫入一個存檔槽
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		/// <param name="data">以存檔鍵為索引的參與者資料</param>
		/// <param name="info">中繼資料</param>
		/// <returns>寫入是否成功</returns>
		bool Save(string slotId, IReadOnlyDictionary<string, string> data, SaveSlotInfo info);

		/// <summary>
		///     讀取一個存檔槽
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		/// <returns>以存檔鍵為索引的參與者資料，存檔槽不存在時回傳 null</returns>
		IReadOnlyDictionary<string, string> Load(string slotId);

		/// <summary>
		///     刪除一個存檔槽
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		/// <returns>刪除是否成功，存檔槽不存在時回傳 false</returns>
		bool Delete(string slotId);

		/// <summary>
		///     列舉已存在的存檔槽
		/// </summary>
		/// <returns>各存檔槽的中繼資料，缺中繼資料的存檔槽不列入</returns>
		IReadOnlyList<SaveSlotInfo> ListSlots();
	}
}