using Sumorin.SumorinUtility;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Reflection;
using UnityEngine.Localization;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// ScriptableObject 資料基底類別，提供 Id、AssetName 與本地化顯示名稱
	/// </summary>
	/// <remarks>
	/// 資料資產一律是遊戲要讀的配置，因此實作 <see cref="IConfig" />。
	/// Domain 讀取時走各自的 <c>I{X}Config</c> 數值面，看不到本型別。
	/// </remarks>
	public abstract class SODataBase: SerializedScriptableObject, IConfig
	{
		/// <summary>
		/// 資料唯一識別碼，同時是配置的查找鍵
		/// </summary>
		/// <remarks>
		/// 建立資產時預設填入 GUID，可改為可讀的識別碼（如 <c>Health</c>）。
		/// 執行期直接讀本欄位的序列化值，不做任何推導。
		/// 全專案唯一由 <c>DataScriptIdRule</c> 驗證並提供修復，撞號時可加類別前綴或編號。
		/// </remarks>
		[OdinSerialize]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		// 屬性與欄位在 Odin 的預設排序不同，三個欄位明確標順序才不會被打散
		[PropertyOrder(0)]
		[PropertySpace(10)]
		public string Id { get; set; }

		/// <summary>
		/// 資產檔案名稱（僅允許英數字、橫線、底線）
		/// </summary>
		/// <remarks>
		/// 驗證方法以字串指定而非 <c>nameof</c>。本組件在所有平台編譯，而驗證方法僅存在於編輯器，
		/// <c>nameof</c> 會要求編譯器當場解析，正式建置就會因為找不到方法而失敗。
		/// </remarks>
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[LabelText("檔案名稱")]
		[PropertyOrder(1)]
		[PropertySpace(10), ValidateInput("IsAssetNameLegal", "名稱只能為英數（含減號底線）")]
		public string AssetName = "";

		/// <summary>
		/// 本地化顯示名稱
		/// </summary>
		[LabelText("顯示名稱")]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[PropertyOrder(2)]
		[PropertySpace(10, 10), ValidateInput("IsDataNameLegal", "需要填寫名稱")]
		public LocalizedString DataName;

	#if UNITY_EDITOR
		/// <summary>
		/// 識別碼的類別前綴，取自 <see cref="DataEditorConfigAttribute.DataRoot" /> 的末段
		/// </summary>
		/// <remarks>
		/// 僅供撞號時的修復動作組出新識別碼，不參與 <see cref="Id" /> 的組成。
		/// 讓執行期的查找鍵去推導編輯器的資料夾路徑太危險，建置時 attribute 可能被 stripping 移除。
		/// 型別沒有標註該 attribute 時為空字串。
		/// </remarks>
		public string IdPrefix
		{
			get
			{
				var config = GetType().GetCustomAttribute<DataEditorConfigAttribute>();
				return config == null ? "" : config.DataRoot.Split('/')[^1];
			}
		}

		/// <summary>
		/// 編輯器選單與下拉選單的顯示文字
		/// </summary>
		/// <remarks>
		/// 取 <see cref="DataName" /> 當前語系的字串。
		/// 顯示名稱未填、或編輯器尚未選定語系時退回 <see cref="AssetName" />。
		/// 同步取值僅供編輯器使用，執行期的顯示走 LocalizeStringEvent。
		/// </remarks>
		public string EditorLabel
		{
			get
			{
				if(DataName.IsNullOrEmpty()) return AssetName;

				var localized = DataName.GetLocalizedString();
				return string.IsNullOrEmpty(localized) ? AssetName : localized;
			}
		}

		/// <summary>
		/// 驗證識別碼是否合法
		/// </summary>
		/// <returns>非空且只含英數則回傳 true</returns>
		public bool IsIdLegal()
		{
			return !string.IsNullOrWhiteSpace(Id) && RegexChecking.OnlyEnglishAndNum(Id);
		}

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
			return IsIdLegal() && IsAssetNameLegal() && IsDataNameLegal();
		}
	#endif
	}
}