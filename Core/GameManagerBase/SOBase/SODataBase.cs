using Sumorin.DDDCore;
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
		/// 識別碼的名稱部分，不含類別前綴
		/// </summary>
		/// <remarks>
		/// 建立資產時預設填入 GUID，可改為可讀的名稱（如 <c>Health</c>）。
		/// 前綴由 <see cref="Id" /> 自動組上，不在此填寫。
		/// </remarks>
		[OdinSerialize]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[LabelText("ID")]
		// 屬性與欄位在 Odin 的預設排序不同，三個欄位明確標順序才不會被打散
		[PropertyOrder(0)]
		[PropertySpace(10)]
		public string IdName { get; set; }

		/// <summary>
		/// 資料唯一識別碼，同時是配置的查找鍵
		/// </summary>
		/// <remarks>
		/// 格式為「類別前綴_識別碼」，前綴取自 <see cref="DataEditorConfigAttribute.DataRoot" /> 的末段。
		/// 前綴由程式組上而非人工填寫，因此不可能漏帶或打錯。
		/// 前綴讓不同類別的資料不會撞 Id，<c>ConfigManager</c> 的查找字典全專案共用一份。
		/// 全專案唯一由 <c>DataScriptIdRule</c> 驗證，改名前請確認沒有其他資產引用舊值。
		/// </remarks>
		public string Id => string.IsNullOrEmpty(IdPrefix) ? IdName : IdPrefix + "_" + IdName;

		/// <summary>
		/// 識別碼的類別前綴，取自 <see cref="DataEditorConfigAttribute.DataRoot" /> 的末段
		/// </summary>
		/// <remarks>
		/// 型別沒有標註 <see cref="DataEditorConfigAttribute" /> 時為空字串，該情況不加前綴。
		/// </remarks>
		public string IdPrefix => cachedIdPrefix ??= ResolveIdPrefix();

		private string cachedIdPrefix;

		/// <summary>
		/// 資產檔案名稱（僅允許英數字、橫線、底線）
		/// </summary>
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[LabelText("檔案名稱")]
		[PropertyOrder(1)]
		[PropertySpace(10), ValidateInput(nameof(IsAssetNameLegal), "名稱只能為英數（含減號底線）")]
		public string AssetName = "";

		/// <summary>
		/// 本地化顯示名稱
		/// </summary>
		[LabelText("顯示名稱")]
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[PropertyOrder(2)]
		[PropertySpace(10, 10), ValidateInput(nameof(IsDataNameLegal), "需要填寫名稱")]
		public LocalizedString DataName;

		private string ResolveIdPrefix()
		{
			var config = GetType().GetCustomAttribute<DataEditorConfigAttribute>();
			return config == null ? "" : config.DataRoot.Split('/')[^1];
		}

	#if UNITY_EDITOR
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
		/// 驗證識別碼的名稱部分是否合法
		/// </summary>
		/// <returns>非空且只含英數則回傳 true</returns>
		public bool IsIdNameLegal()
		{
			return !string.IsNullOrWhiteSpace(IdName) && RegexChecking.OnlyEnglishAndNum(IdName);
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
			return IsIdNameLegal() && IsAssetNameLegal() && IsDataNameLegal();
		}
	#endif
	}
}