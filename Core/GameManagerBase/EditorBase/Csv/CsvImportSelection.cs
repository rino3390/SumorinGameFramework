using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// CSV 匯入前的挑檔清單，列出資料夾內的 CSV 並記錄哪些要匯入
	/// </summary>
	/// <remarks>
	/// 不含視窗行為，Odin 直接依本類別的標註繪製，挑檔邏輯不開視窗也能測試。
	/// </remarks>
	public class CsvImportSelection
	{
		/// <summary>
		/// 選定的資料夾
		/// </summary>
		[ShowInInspector, DisplayAsString, LabelText("資料夾")]
		[PropertyOrder(0)]
		public string Folder { get; }

		/// <summary>
		/// 已選筆數的顯示文字
		/// </summary>
		[ShowInInspector, DisplayAsString, HideLabel]
		[HorizontalGroup("Select", Width = 120)]
		[PropertyOrder(1)]
		public string SelectedSummary => $"已選 {SelectedCount} / {ImportableCount}";

		/// <summary>
		/// 資料夾內的 CSV，依檔名排序
		/// </summary>
		[ShowInInspector]
		[TableList(IsReadOnly = true, AlwaysExpanded = true, ShowPaging = false)]
		[PropertyOrder(2)]
		// setter 不能拿掉：Odin 把沒有 setter 的屬性判為唯讀，整張表的勾選框會一起停用
		public List<Entry> Entries { get; private set; }

		/// <summary>
		/// 對得上資料型別、可以匯入的檔案數
		/// </summary>
		public int ImportableCount => Entries.Count(entry => entry.CanImport);

		/// <summary>
		/// 目前勾選的檔案數
		/// </summary>
		public int SelectedCount => Entries.Count(entry => entry.CanImport && entry.Selected);

		/// <summary>
		/// 是否至少勾選一個可匯入的檔案
		/// </summary>
		public bool HasSelection => SelectedCount > 0;

		private readonly List<Type> types;

		/// <summary>
		/// 以所有掛 <see cref="DataEditorConfigAttribute" /> 的型別建立清單
		/// </summary>
		/// <param name="folder">放 CSV 的資料夾</param>
		public CsvImportSelection(string folder): this(folder, DataCsv.DataTypes()) { }

		/// <summary>
		/// 以指定型別建立清單，可匯入的檔案預設全部勾選
		/// </summary>
		/// <param name="folder">放 CSV 的資料夾</param>
		/// <param name="dataTypes">可對應的資料型別</param>
		public CsvImportSelection(string folder, IEnumerable<Type> dataTypes)
		{
			Folder = folder;
			types = dataTypes.ToList();
			Entries = DataCsv.FindCsvFiles(folder).Select(CreateEntry).ToList();
		}

		/// <summary>
		/// 勾選全部可匯入的檔案
		/// </summary>
		[Button("全選")]
		[HorizontalGroup("Select")]
		[PropertyOrder(1)]
		public void SelectAll()
		{
			Entries.ForEach(entry => entry.Selected = entry.CanImport);
		}

		/// <summary>
		/// 取消全部勾選
		/// </summary>
		[Button("全不選")]
		[HorizontalGroup("Select")]
		[PropertyOrder(1)]
		public void SelectNone()
		{
			Entries.ForEach(entry => entry.Selected = false);
		}

		/// <summary>
		/// 匯入勾選且可匯入的檔案
		/// </summary>
		/// <returns>匯入結果，只含有匯入的檔案</returns>
		public CsvImportReport Import()
		{
			return DataCsv.Import(Entries.Where(entry => entry.CanImport && entry.Selected).Select(entry => entry.FilePath), types);
		}

		private Entry CreateEntry(string filePath)
		{
			var type = DataCsv.FindType(filePath, types);

			return new()
			{
				FilePath = filePath,
				FileName = Path.GetFileName(filePath),
				TabName = type == null ? DataCsv.UnknownTypeLabel : type.GetCustomAttribute<DataEditorConfigAttribute>()?.TabName ?? type.Name,
				CanImport = type != null,
				Selected = type != null
			};
		}

		/// <summary>
		/// 清單上的一個 CSV 檔
		/// </summary>
		public class Entry
		{
			/// <summary>
			/// 是否匯入，不能匯入的檔案勾了也不會處理
			/// </summary>
			// TableList 的欄位標題只取成員名稱，LabelText 改不到標題、反而畫進每一格；包成群組後標題改用群組名稱
			[VerticalGroup("匯入"), HideLabel, TableColumnWidth(50, false), EnableIf(nameof(CanImport))]
			[CustomValueDrawer(nameof(DrawCenteredToggle))]
			public bool Selected;

			/// <summary>
			/// CSV 檔名
			/// </summary>
			[VerticalGroup("檔名"), HideLabel, DisplayAsString]
			public string FileName;

			/// <summary>
			/// 對應資料型別的頁籤名稱，對不上時是說明文字
			/// </summary>
			[VerticalGroup("對應頁籤"), HideLabel, DisplayAsString]
			public string TabName;

			/// <summary>
			/// 檔名是否對得上資料型別
			/// </summary>
			[HideInInspector]
			public bool CanImport;

			/// <summary>
			/// CSV 檔完整路徑
			/// </summary>
			[HideInInspector]
			public string FilePath;

			private static bool DrawCenteredToggle(bool value, GUIContent label)
			{
				var rect = EditorGUILayout.GetControlRect();

				return EditorGUI.Toggle(rect.AlignCenterX(EditorStyles.toggle.CalcSize(GUIContent.none).x), value);
			}
		}
	}
}