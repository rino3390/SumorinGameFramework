namespace Sumorin.Save
{
	/// <summary>
	///     存檔參與者，任何需要被存檔的資料來源都實作它
	/// </summary>
	/// <remarks>
	///     存放範圍與載入順序都由參與者自己宣告，協調者只做分流與排序。
	///     Repository 型的資料由轉接層包裝後參與，Repository 本身不實作本介面。
	///     單例狀態型的資料由 Controller 持有的狀態物件自行實作本介面。
	///     本介面只由 SaveController 取用，表現層不得注入。
	///     Import 與 Clear 會直接改變 Domain 狀態，經表現層呼叫等同繞過 CommandService。
	/// </remarks>
	public interface ISaveParticipant
	{
		/// <summary>
		///     存檔鍵，全域不可重複
		/// </summary>
		string SaveKey { get; }

		/// <summary>
		///     載入順序，數字越大越先載入
		/// </summary>
		/// <remarks>沒有依賴關係時回傳 0。</remarks>
		int LoadOrder { get; }

		/// <summary>
		///     是否為跨存檔資料
		/// </summary>
		/// <remarks>回傳 true 代表歸屬全域槽，false 代表歸屬存檔槽。</remarks>
		bool IsGlobal { get; }

		/// <summary>
		///     把自身資料序列化成字串
		/// </summary>
		string Export();

		/// <summary>
		///     從字串還原自身資料
		/// </summary>
		/// <param name="data">先前由 <see cref="Export" /> 產生的字串</param>
		/// <remarks>
		///     單例狀態型的參與者必須就地寫入既有欄位，不可重建值面物件，否則訂閱會斷連。
		///     匯入直接寫欄位，不走公開方法，還原不是事件，不可觸發發生型的通知。
		/// </remarks>
		void Import(string data);

		/// <summary>
		///     清除自身資料
		/// </summary>
		void Clear();
	}
}