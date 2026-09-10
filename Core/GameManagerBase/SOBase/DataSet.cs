#if UNITY_EDITOR
	using Sirenix.OdinInspector.Editor;
	using Sirenix.Utilities.Editor;
	using Sumorin.SumorinUtility.Editor;
	using System.IO;
	using UnityEditor;
#endif
	using Sirenix.OdinInspector;
	using Sumorin.SumorinUtility;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using UnityEngine;
	using Random = UnityEngine.Random;

	namespace Sumorin.GameManagerBase
	{
		/// <summary>
		/// 資料集合基底類別，管理 SODataBase 衍生類別的清單
		/// </summary>
		/// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
		public class DataSet<T>: ScriptableObject where T: SODataBase
		{
			/// <summary>
			/// 資料清單
			/// </summary>
			/// <remarks>
			/// 新增會直接建立資產、移除會連資產一起刪，增刪即是對本集合的增刪。
			/// 清單內的唯一性等同該型別的唯一性，跨型別的 Id 衝突由 <c>DataScriptIdRule</c> 全專案掃描涵蓋。
			/// 資產在編輯器外被增刪造成的落差由 <c>DataSetMembershipRule</c> 檢出並修復，該規則需要 Odin Validator。
			/// 沒有 Validator 的專案改在標題列給重新整理按鈕，是同一件事的替代手段，兩者不並存。
			/// </remarks>
			[ListDrawerSettings(
			#if !ODIN_VALIDATOR
				OnTitleBarGUI = "DrawRefreshButton",
			#endif
				CustomAddFunction = "CreateNewData",
				CustomRemoveElementFunction = "DeleteData",
				ListElementLabelName = "@Sumorin.GameManagerBase.OdinMenuTreeExtension.GetDisplayName(this) + \"：\"",
				DraggableItems = false,
				NumberOfItemsPerPage = 20
			)]
			[InlineEditor(InlineEditorObjectFieldModes.Foldout)]
			[Searchable]
			[UniqueList(nameof(SODataBase.Id), "Id重複")]
			public List<T> Datas = new();

			/// <summary>
			/// 根據 Id 取得資料
			/// </summary>
			/// <param name="dataId">資料 Id</param>
			/// <returns>對應的資料</returns>
			/// <exception cref="ArgumentNullException">找不到指定 Id 的資料時拋出</exception>
			public T GetData(string dataId)
			{
				var data = Datas.Find(x => x.Id == dataId);

				return data == null ? throw new ArgumentNullException(nameof(dataId), $"找不到 dataId: {dataId}") : data;
			}

			/// <summary>
			/// 取得隨機一筆資料
			/// </summary>
			/// <returns>隨機資料</returns>
			/// <exception cref="ArgumentNullException">資料清單為空時拋出</exception>
			public T GetRandomData()
			{
				return Datas.Count == 0 ? throw new ArgumentNullException(nameof(Datas), "找不到任何資料") : Datas[Random.Range(0, Datas.Count)];
			}

		#if UNITY_EDITOR
			/// <summary>
			/// 清單按下新增時開啟名稱輸入彈窗，確認後才建立資產並加入清單（Editor 專用）
			/// </summary>
			/// <remarks>
			/// 資產路徑取自 <see cref="DataEditorConfigAttribute.DataRoot" />，沒有標註該 attribute 的型別不開彈窗。
			/// 回傳 void 讓 Odin 不自行加入元素，加入由彈窗確認後的 <see cref="AddData" /> 負責。
			/// <c>CustomAddFunction</c> 以字串指定而非 <c>nameof</c>，本方法僅存在於編輯器，字串在正式建置不會參與編譯。
			/// </remarks>
			private void CreateNewData()
			{
				var config = typeof(T).GetCustomAttribute<DataEditorConfigAttribute>();

				if(config == null) return;

				CreateDataPopUp.Open(this, config);
			}

			/// <summary>
			/// 清單按下移除時連資產一併刪除（Editor 專用）
			/// </summary>
			/// <remarks>
			/// 本集合就是該型別資料的全部，從清單移除等同這筆資料不再存在，留著孤兒資產只會變成待清的垃圾。
			/// </remarks>
			/// <param name="data">要刪除的資產</param>
			private void DeleteData(T data)
			{
				Datas.Remove(data);

				if(data != null)
				{
					AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(data));
				}

				SumorinEditorUtility.SaveSOData(this);
			}

			/// <summary>
			/// 把資產加入清單並存檔（Editor 專用）
			/// </summary>
			/// <param name="data">要加入的資產，已在清單內時不重複加入</param>
			public void AddData(T data)
			{
				if(data == null || Datas.Contains(data)) return;

				Datas.Add(data);
				SumorinEditorUtility.SaveSOData(this);
			}

		#if !ODIN_VALIDATOR
			/// <summary>
			/// 以專案內該型別的全部資產重建清單（Editor 專用，僅在沒有 Odin Validator 時提供）
			/// </summary>
			/// <remarks>
			/// 新增資產時會自動加入清單，本方法用於資產在編輯器外被增刪後的手動校正。
			/// 裝有 Validator 的專案由 <c>DataSetMembershipRule</c> 逐筆檢出並修復，不需要整份重掃。
			/// </remarks>
			public void RefreshFromAssets()
			{
				Datas = SumorinEditorUtility.FindAssets<T>();
				SumorinEditorUtility.SaveSOData(this);
			}

			/// <summary>
			/// 繪製清單標題列的重新整理按鈕（Editor 專用，僅在沒有 Odin Validator 時提供）
			/// </summary>
			private void DrawRefreshButton()
			{
				if(!SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh)) return;

				RefreshFromAssets();
			}
		#endif

			/// <summary>
			/// 自動尋找對應 DataSet 資產並建立下拉選單資料來源（Editor 專用，無需實例）
			/// </summary>
			/// <returns>下拉選單項目集合</returns>
			public static IEnumerable DrawValueDropDown()
			{
				var dataSet = SumorinEditorUtility.FindAsset<DataSet<T>>();

				return dataSet.Datas.Select(data => new ValueDropdownItem(data.EditorLabel, data.Id));
			}

			/// <summary>
			/// 新增資料的名稱輸入彈窗，輸入的名稱同時作為 Id 與檔案名稱（Editor 專用）
			/// </summary>
			private class CreateDataPopUp
			{
				private const float Width = 400f;

				[Title("$title")]
				[LabelText("名稱")]
				[ValidateInput(nameof(IsNameLegal), "只能用英數（含減號底線），且不得與現有的 Id、檔名重複")]
				public string Name = "";

				private static OdinEditorWindow popupWindow;

				private readonly DataSet<T> owner;
				private readonly DataEditorConfigAttribute config;
				private readonly string title;

				private string AssetPath => config.DataRoot + "/" + Name;

				private CreateDataPopUp(DataSet<T> owner, DataEditorConfigAttribute config)
				{
					this.owner = owner;
					this.config = config;
					title = "新增" + config.DataTypeLabel;
				}

				/// <summary>
				/// 在清單標題列下方開啟彈窗，右緣貼齊標題列右緣（加號按鈕所在）
				/// </summary>
				/// <remarks>
				/// 本方法在加號按鈕的點擊分支內被呼叫，此時事件已被按鈕吃掉（Used），
				/// <c>GUILayoutUtility.GetLastRect</c> 只會回 (0,0,1,1) 的假矩形，改讀目前版面群組（標題列）的矩形。
				/// Odin 把彈窗左上角貼在傳入矩形的左下角，所以把矩形往左推一個彈窗寬度，右緣就對齊標題列。
				/// </remarks>
				/// <param name="owner">確認後要加入的資料集合</param>
				/// <param name="config">提供資產路徑與型別標籤</param>
				public static void Open(DataSet<T> owner, DataEditorConfigAttribute config)
				{
					var toolbarRect = GUIHelper.GetCurrentLayoutRect();

					// ponytail: 左緣以目前 GUI 群組的原點為下限，群組本身一定在視窗內；視窗窄於彈窗時右緣會脫離標題列
					var anchor = new Rect(Mathf.Max(0f, toolbarRect.xMax - Width), toolbarRect.y, Width, toolbarRect.height);
					popupWindow = OdinEditorWindow.InspectObjectInDropDown(new CreateDataPopUp(owner, config), anchor, Width);
				}

				[Button("建立"), EnableIf(nameof(IsNameLegal))]
				private void Create()
				{
					var data = CreateInstance<T>();
					data.Id = Name;
					data.AssetName = Name;
					SumorinEditorUtility.CreateSOData(data, AssetPath);
					owner.AddData(data);
					popupWindow.Close();
				}

				private bool IsNameLegal()
				{
					return !string.IsNullOrEmpty(Name)
						   && RegexChecking.OnlyEnglishAndNum(Name)
						   && !owner.Datas.Any(data => data != null && (data.Id == Name || data.AssetName == Name))
						   && !File.Exists("Assets/" + AssetPath + ".asset");
				}
			}
		#endif
		}
	}