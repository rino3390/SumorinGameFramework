using Sumorin.SumorinUtility;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Reflection;
using UnityEngine.Localization;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
#endif

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
		/// 資料唯一 Id，同時是配置的查找鍵
		/// </summary>
		/// <remarks>
		/// 建立資產時預設填入 GUID，可改為可讀的 Id（如 <c>Health</c>）。
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
		/// 值與資產檔名永遠一致，改名由欄位下方的確認按鈕發起，見 <see cref="TryRenameAsset" />。
		/// 驗證與繪製方法以字串指定而非 <c>nameof</c>。本組件在所有平台編譯，而這些方法僅存在於編輯器，
		/// <c>nameof</c> 會要求編譯器當場解析，正式建置就會因為找不到方法而失敗。
		/// </remarks>
		[HorizontalGroup(LayoutConst.TopInfoLayout)]
		[VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
		[LabelText("檔案名稱")]
		[PropertyOrder(1)]
		[PropertySpace(10), ValidateInput("IsAssetNameLegal", "名稱只能為英數（含減號底線）")]
		[OnInspectorGUI("TrackAssetNameEdit", "DrawRenameUi")]
		[OnInspectorInit("ClearRenameError")]
		[GUIColor("AssetNameFieldColor")]
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
		/// Id 的類別前綴，取自 <see cref="DataEditorConfigAttribute.DataRoot" /> 的末段
		/// </summary>
		/// <remarks>
		/// 僅供撞號時的修復動作組出新 Id，不參與 <see cref="Id" /> 的組成。
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

		private static readonly Color PendingRenameColor = new(1f, 0.85f, 0.45f);
		private static readonly Color ConfirmButtonColor = new(0.55f, 1f, 0.55f);
		private static readonly Color RevertButtonColor = new(1f, 0.55f, 0.55f);

		private Color AssetNameFieldColor => HasPendingRename ? PendingRenameColor : Color.white;

		private string CurrentFileName => Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));

		/// <remarks>
		/// 在 Project 視窗直接改檔名也會讓值與檔名不同，那不算改名流程的開始，所以要求欄位先被編輯過。
		/// </remarks>
		private bool HasPendingRename => assetNameFieldEdited && !string.IsNullOrEmpty(CurrentFileName) && CurrentFileName != AssetName;

		private string renameError;
		private Rect renameButtonsRect;
		private Rect assetNameFieldRect;

		/// <remarks>
		/// 以值的變化判斷使用者編輯過欄位，不看鍵盤焦點。
		/// 焦點抓不抓得到取決於 Odin 怎麼畫這個欄位，按鈕該不該出現不押在這件事上。
		/// </remarks>
		private bool assetNameFieldEdited;

		/// <remarks>
		/// null 代表還沒繪製過，首次只記錄現值。否則從磁碟讀進來的名稱會被當成使用者剛打的字。
		/// </remarks>
		private string lastSeenAssetName;

		/// <summary>
		/// 把資產檔名改為指定名稱，並同步 <see cref="AssetName" />
		/// </summary>
		/// <param name="newName">新檔名，不含副檔名</param>
		/// <param name="error">失敗原因，成功時為 null</param>
		/// <returns>改名成功或檔名本來就相符則回傳 true</returns>
		/// <remarks>
		/// 編輯器的確認按鈕與 CSV 匯入共用這條路徑，檔名規則才不會兩邊各走一套。
		/// </remarks>
		public bool TryRenameAsset(string newName, out string error)
		{
			error = null;
			var path = AssetDatabase.GetAssetPath(this);

			if(string.IsNullOrEmpty(path))
			{
				error = "資產尚未存檔";
				return false;
			}

			if(string.IsNullOrWhiteSpace(newName) || !RegexChecking.OnlyEnglishAndNum(newName))
			{
				error = "名稱只能為英數";
				return false;
			}

			if(Path.GetFileNameWithoutExtension(path) != newName)
			{
				if(File.Exists(path[..(path.LastIndexOf('/') + 1)] + newName + ".asset"))
				{
					error = "檔名已存在";
					return false;
				}

				var renameFailure = AssetDatabase.RenameAsset(path, newName);

				if(!string.IsNullOrEmpty(renameFailure))
				{
					error = renameFailure;
					return false;
				}
			}

			AssetName = newName;
			EditorUtility.SetDirty(this);

			return true;
		}

		/// <summary>
		/// 驗證 Id 是否合法
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

		private void ClearRenameError()
		{
			renameError = null;
		}

		private void TrackAssetNameEdit()
		{
			if(lastSeenAssetName == null)
			{
				lastSeenAssetName = AssetName;
			}
			else if(AssetName != lastSeenAssetName)
			{
				lastSeenAssetName = AssetName;
				assetNameFieldEdited = true;
				renameError = null;
			}
		}

		private void DrawRenameUi()
		{
			// append 的起點就接在欄位後面，這裡的 last rect 是剛畫完的 AssetName 欄位
			if(Event.current.type == EventType.Repaint)
			{
				assetNameFieldRect = GUILayoutUtility.GetLastRect();
			}

			if(!HasPendingRename)
			{
				// 值與檔名一致就是這輪改名的終點，旗標留著會讓之後在 Project 視窗改檔名也冒出按鈕
				assetNameFieldEdited = false;

				if(!string.IsNullOrEmpty(renameError))
				{
					EditorGUILayout.HelpBox(renameError, MessageType.Error);
				}

				return;
			}

			DrawRenameButtons();
			RevertOnClickOutside();
		}

		private void DrawRenameButtons()
		{
			var rect = EditorGUILayout.BeginHorizontal();

			// Layout 事件算不出 rect，只有重繪那幀的值可信
			if(Event.current.type == EventType.Repaint)
			{
				renameButtonsRect = rect;
			}

			var originalBackground = GUI.backgroundColor;
			var originalColor = GUI.color;

			// 欄位的 GUIColor 會一路套到這裡，不歸零按鈕就會是琥珀色疊上綠紅的濁色
			GUI.color = Color.white;

			using(new EditorGUI.DisabledScope(!IsAssetNameLegal()))
			{
				GUI.backgroundColor = ConfirmButtonColor;

				if(GUILayout.Button("確認"))
				{
					ConfirmRename();
				}
			}

			GUI.backgroundColor = RevertButtonColor;

			if(GUILayout.Button("還原"))
			{
				RevertRename();
			}

			GUI.backgroundColor = originalBackground;
			GUI.color = originalColor;
			EditorGUILayout.EndHorizontal();
		}

		/// <remarks>
		/// 以點擊落點判斷離開，不看鍵盤焦點。Odin 重建欄位的 drawer 時焦點名稱會短暫對不上，
		/// 拿焦點當依據會在使用者還在打字時就把值還原掉。
		/// </remarks>
		private void RevertOnClickOutside()
		{
			if(Event.current.type != EventType.MouseDown) return;

			var mouse = Event.current.mousePosition;

			if(assetNameFieldRect.Contains(mouse) || renameButtonsRect.Contains(mouse)) return;

			RevertRename();
		}

		private void ConfirmRename()
		{
			if(!TryRenameAsset(AssetName, out var error))
			{
				renameError = error;
				AssetName = CurrentFileName;
			}

			EndRename();
		}

		private void RevertRename()
		{
			AssetName = CurrentFileName;
			renameError = null;
			EndRename();
		}

		private void EndRename()
		{
			// 只改值不夠，游標留在欄位裡就還是編輯狀態，接著打字又會進入未確認
			GUIUtility.keyboardControl = 0;
			EditorGUIUtility.editingTextField = false;

			assetNameFieldEdited = false;
			lastSeenAssetName = AssetName;
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