using System;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔槽的中繼資料
	/// </summary>
	/// <remarks>存檔時由協調者附加，不經參與者。</remarks>
	public readonly struct SaveSlotInfo
	{
		/// <summary>
		///     識別是哪一個存檔槽
		/// </summary>
		public string SlotId { get; }

		/// <summary>
		///     建立這份存檔的時間
		/// </summary>
		public DateTime SavedAt { get; }

		/// <summary>
		///     玩家或系統填入的說明
		/// </summary>
		public string Description { get; }

		/// <summary>
		///     建立存檔資訊
		/// </summary>
		/// <param name="slotId">存檔槽 Id</param>
		/// <param name="savedAt">存檔時間</param>
		/// <param name="description">存檔描述</param>
		public SaveSlotInfo(string slotId, DateTime savedAt, string description)
		{
			SlotId = slotId;
			SavedAt = savedAt;
			Description = description;
		}
	}
}