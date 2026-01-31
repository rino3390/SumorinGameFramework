using System;

namespace Rino.GameFramework.GameManagerBase
{
	/// <summary>
	/// 標記 SODataBase 子類別的 Editor 配置，用於動態建立 DataEditor Tab
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	public class DataEditorConfigAttribute : Attribute
	{
		/// <summary>
		/// Tab 顯示名稱
		/// </summary>
		public string TabName { get; }

		/// <summary>
		/// 資料根目錄路徑（相對於 Assets）
		/// </summary>
		public string DataRoot { get; }

		/// <summary>
		/// 資料類型標籤（用於顯示名稱）
		/// </summary>
		public string DataTypeLabel { get; }

		/// <summary>
		/// 初始化 DataEditorConfigAttribute
		/// </summary>
		/// <param name="tabName">Tab 顯示名稱</param>
		/// <param name="dataRoot">資料根目錄路徑</param>
		/// <param name="dataTypeLabel">資料類型標籤</param>
		public DataEditorConfigAttribute(string tabName, string dataRoot, string dataTypeLabel)
		{
			TabName = tabName;
			DataRoot = dataRoot;
			DataTypeLabel = dataTypeLabel;
		}
	}
}
