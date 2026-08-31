#if UNITY_EDITOR
	using Sumorin.SumorinUtility.Editor;
	using UnityEditor;
#endif
#if UNITY_EDITOR && !ODIN_VALIDATOR
	using Sirenix.Utilities.Editor;
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
				DraggableItems = false,
				NumberOfItemsPerPage = 20
			)]
			[InlineEditor(InlineEditorObjectFieldModes.Hidden)]
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
			/// 清單按下新增時建立一份資產並回傳（Editor 專用）
			/// </summary>
			/// <remarks>
			/// 資產路徑取自 <see cref="DataEditorConfigAttribute.DataRoot" />，沒有標註該 attribute 的型別不建立資產。
			/// <c>CustomAddFunction</c> 以字串指定而非 <c>nameof</c>，本方法僅存在於編輯器，字串在正式建置不會參與編譯。
			/// </remarks>
			/// <returns>建立好的資產，無法建立時回傳 null</returns>
			private T CreateNewData()
			{
				var config = typeof(T).GetCustomAttribute<DataEditorConfigAttribute>();

				if(config == null) return null;

				var data = CreateInstance<T>();
				data.Id = SumorinUtility.GUID.NewGuid();
				data.AssetName = config.DataRoot.Split('/')[^1] + " - " + data.Id;
				SumorinEditorUtility.CreateSOData(data, config.DataRoot + "/" + data.AssetName);

				return data;
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
		#endif
		}
	}