using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// CSV 匯入清單視窗，挑完檔按匯入才真的寫入資料
	/// </summary>
	public class CsvImportListWindow: OdinEditorWindow
	{
		[ShowInInspector, HideLabel, InlineProperty, HideReferenceObjectPicker]
		private CsvImportSelection selection;

		private Action onImported;

		/// <summary>
		/// 以挑檔清單開啟視窗
		/// </summary>
		/// <param name="selection">挑檔清單</param>
		/// <param name="onImported">匯入完成後的回呼，取消時不會呼叫</param>
		public static void Open(CsvImportSelection selection, Action onImported)
		{
			var window = GetWindow<CsvImportListWindow>(true, "CSV 匯入清單");
			window.selection = selection;
			window.onImported = onImported;
			window.minSize = new(520, 360);
			window.Show();
		}

		[Button("取消")]
		[HorizontalGroup("Actions")]
		[PropertyOrder(10)]
		private void Cancel()
		{
			Close();
		}

		// 重新編譯後 selection 不會被序列化保留，視窗還開著時會是 null
		[Button("匯入"), EnableIf("@selection != null && selection.HasSelection")]
		[HorizontalGroup("Actions")]
		[PropertyOrder(10)]
		private void Import()
		{
			var report = selection.Import();
			Close();
			CsvImportReportWindow.Open(report);
			onImported?.Invoke();
		}
	}
}