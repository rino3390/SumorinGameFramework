using Sumorin.SumorinUtility;
using Sirenix.OdinInspector;
using UnityEngine.Localization;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// ScriptableObject 資料基底類別，提供 Id、AssetName 與本地化顯示名稱
	/// </summary>
	public abstract class SODataBase: SerializedScriptableObject
	{
		/// <summary>
		/// 資料唯一識別碼（唯讀）
		/// </summary>
		[ReadOnly]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[PropertySpace(10)]
		public string Id;

		/// <summary>
		/// 資產檔案名稱（僅允許英數字、橫線、底線）
		/// </summary>
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[LabelText("檔案名稱")]
		[PropertySpace(10), ValidateInput(nameof(IsAssetNameLegal), "名稱只能為英數（含減號底線）")]
		public string AssetName = "";

		/// <summary>
		/// 本地化顯示名稱
		/// </summary>
		[LabelText("顯示名稱")]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[PropertySpace(10, 10), ValidateInput(nameof(IsDataNameLegal), "需要填寫名稱")]
		public LocalizedString DataName;

	#if UNITY_EDITOR
		/// <summary>
		/// 驗證資產名稱是否合法
		/// </summary>
		/// <returns>名稱合法則回傳 true</returns>
		private bool IsAssetNameLegal()
		{
			return !string.IsNullOrEmpty(AssetName) && RegexChecking.OnlyEnglishAndNum(AssetName);
		}

		/// <summary>
		/// 驗證顯示名稱是否已設定
		/// </summary>
		/// <returns>名稱已設定則回傳 true</returns>
		private bool IsDataNameLegal()
		{
			return !DataName.IsNullOrEmpty();
		}

		/// <summary>
		/// 驗證資料是否合法
		/// </summary>
		/// <returns>資料合法則回傳 true</returns>
		public virtual bool IsDataLegal()
		{
			return IsAssetNameLegal() && IsDataNameLegal();
		}
	#endif
	}
}