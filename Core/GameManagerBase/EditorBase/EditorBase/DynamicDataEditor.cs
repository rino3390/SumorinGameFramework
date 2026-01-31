using System;
using System.Reflection;

namespace Rino.GameFramework.GameManagerBase
{
	/// <summary>
	/// 動態資料編輯器，根據 DataEditorConfigAttribute 自動配置
	/// </summary>
	/// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
	public class DynamicDataEditor<T> : CreateNewDataEditor<T> where T : SODataBase
	{
		private readonly DataEditorConfigAttribute config;

		/// <summary>
		/// Tab 顯示名稱
		/// </summary>
		public override string TabName => config.TabName;

		/// <summary>
		/// 資料根目錄路徑
		/// </summary>
		protected override string DataRoot => config.DataRoot;

		/// <summary>
		/// 資料類型標籤
		/// </summary>
		protected override string DataTypeLabel => config.DataTypeLabel;

		/// <summary>
		/// 初始化 DynamicDataEditor，從 T 的 DataEditorConfigAttribute 讀取配置
		/// </summary>
		public DynamicDataEditor()
		{
			config = typeof(T).GetCustomAttribute<DataEditorConfigAttribute>();

			if (config == null)
			{
				throw new InvalidOperationException(
					$"Type {typeof(T).Name} does not have DataEditorConfigAttribute.");
			}
		}
	}
}
