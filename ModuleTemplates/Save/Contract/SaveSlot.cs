namespace Sumorin.Save
{
	/// <summary>
	///     存檔槽的保留識別碼
	/// </summary>
	public static class SaveSlot
	{
		/// <summary>
		///     全域槽使用的保留識別碼
		/// </summary>
		/// <remarks>
		///     不可作為一般存檔槽使用，也不可刪除。
		///     列舉存檔槽時不會出現。
		/// </remarks>
		public const string GlobalId = "__global__";
	}
}