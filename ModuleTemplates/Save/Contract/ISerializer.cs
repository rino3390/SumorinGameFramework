namespace Sumorin.Save
{
	/// <summary>
	///     序列化介面，預設實作為 NewtonsoftSaveSerializer，可由專案層覆寫
	/// </summary>
	/// <remarks>
	///     實作必須掛上值面型別的轉換器，讓值面欄位落地為純值而非其內部結構。
	///     例如音效頻道落地為識別碼、音量、靜音三個純值。
	///     實體必須能由序列化資料還原，還原方式由實作決定。
	/// </remarks>
	public interface ISerializer
	{
		/// <summary>
		///     把物件序列化成字串
		/// </summary>
		/// <typeparam name="T">來源型別</typeparam>
		/// <param name="target">要序列化的物件</param>
		string Serialize<T>(T target);

		/// <summary>
		///     把字串還原成物件
		/// </summary>
		/// <typeparam name="T">目標型別</typeparam>
		/// <param name="data">先前序列化產生的字串</param>
		/// <returns>還原後的物件，無法還原時回傳 null</returns>
		T Deserialize<T>(string data);
	}
}