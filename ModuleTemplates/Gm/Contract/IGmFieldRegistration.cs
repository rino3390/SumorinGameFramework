namespace Sumorin.Gm
{
	/// <summary>
	///     遊戲側的欄位登記入口，實作的型別由遊戲側進入點傳給啟動點，再交給欄位目錄
	/// </summary>
	/// <remarks>
	///     實例由容器建立，注入 <c>I{Domain}ValueService</c> 即可拿到候選清單來源（例如場上角色的 Id 清單）。
	///     欄位建構器會生成 View，實作放在遊戲側的 GM 表現層組件，不要放進 GM 流程。
	/// </remarks>
	public interface IGmFieldRegistration
	{
		/// <summary>
		///     把自訂型別的欄位登記進目錄
		/// </summary>
		/// <param name="catalog">欄位目錄</param>
		void Register(IGmFieldCatalog catalog);
	}
}