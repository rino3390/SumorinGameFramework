using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔 Controller 介面
	/// </summary>
	public interface ISaveController
	{
		/// <summary>
		///     把存檔槽範圍的參與者資料寫入指定存檔槽
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		/// <param name="description">存檔描述</param>
		/// <remarks>回傳即代表快照完成，狀態已序列化交付存取媒介。</remarks>
		CommandResult Save(string slotId, string description);

		/// <summary>
		///     載入指定存檔槽，並發出 <see cref="GameLoaded" />
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		CommandResult Load(string slotId);

		/// <summary>
		///     刪除指定存檔槽
		/// </summary>
		/// <param name="slotId">存檔槽識別碼</param>
		CommandResult Delete(string slotId);

		/// <summary>
		///     清空存檔槽範圍的所有參與者，全域設定不受影響
		/// </summary>
		CommandResult NewGame();

		/// <summary>
		///     把全域槽範圍的參與者資料寫入全域槽
		/// </summary>
		CommandResult SaveGlobal();

		/// <summary>
		///     載入全域槽，不發出任何事實
		/// </summary>
		/// <remarks>首次啟動時沒有全域檔屬正常情況，回傳成功且不做任何還原。</remarks>
		CommandResult LoadGlobal();

		/// <summary>
		///     列舉已存在的存檔槽，結果不含全域槽
		/// </summary>
		IReadOnlyList<SaveSlotInfo> GetSlots();
	}
}