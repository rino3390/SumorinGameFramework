using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 顯示 CSV 匯入結果的視窗
	/// </summary>
	public class CsvImportReportWindow: OdinEditorWindow
	{
		[ShowInInspector, HideLabel, MultiLineProperty(30)]
		private string text;

		/// <summary>
		/// 以匯入結果開啟視窗
		/// </summary>
		/// <param name="report">匯入結果</param>
		public static void Open(CsvImportReport report)
		{
			var window = GetWindow<CsvImportReportWindow>(true, "CSV 匯入結果");
			window.text = report.ToString();
			window.minSize = new(480, 320);
			window.Show();
		}
	}
}