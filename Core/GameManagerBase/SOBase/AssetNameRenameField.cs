using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// AssetName 欄位的改名互動，負責未確認的修改、確認與還原按鈕，以及離開欄位的還原
	/// </summary>
	/// <remarks>
	/// 狀態與繪製都收在這裡，<see cref="SODataBase" /> 只留 Odin attribute 指名的回呼入口。
	/// Odin 的 action string 是在標註的型別上反射解析成員，那幾個入口搬不走。
	/// </remarks>
	internal class AssetNameRenameField
	{
	#if UNITY_EDITOR
		private static readonly Color PendingColor = new(1f, 0.85f, 0.45f);
		private static readonly Color ConfirmButtonColor = new(0.55f, 1f, 0.55f);
		private static readonly Color RevertButtonColor = new(1f, 0.55f, 0.55f);

		private readonly SODataBase data;

		/// <summary>
		/// 欄位顏色，有未確認的修改時轉琥珀色
		/// </summary>
		public Color FieldColor => HasPendingRename ? PendingColor : Color.white;

		private string CurrentFileName => Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(data));

		/// <remarks>
		/// 在 Project 視窗直接改檔名也會讓值與檔名不同，那不算改名流程的開始，所以要求欄位先被編輯過。
		/// </remarks>
		private bool HasPendingRename => fieldEdited && !string.IsNullOrEmpty(CurrentFileName) && CurrentFileName != data.AssetName;

		private string error;
		private Rect buttonsRect;
		private Rect fieldRect;

		/// <remarks>
		/// 以值的變化判斷使用者編輯過欄位，不看鍵盤焦點。
		/// 焦點抓不抓得到取決於 Odin 怎麼畫這個欄位，按鈕該不該出現不押在這件事上。
		/// </remarks>
		private bool fieldEdited;

		/// <remarks>
		/// null 代表還沒繪製過，首次只記錄現值。否則從磁碟讀進來的名稱會被當成使用者剛打的字。
		/// </remarks>
		private string lastSeenName;

		public AssetNameRenameField(SODataBase data)
		{
			this.data = data;
		}

		/// <summary>
		/// 清掉殘留的錯誤訊息
		/// </summary>
		/// <remarks>
		/// 切到別筆資料再回來時 inspector 會重新初始化，上一次的錯誤不該還留在畫面上。
		/// </remarks>
		public void ClearError()
		{
			error = null;
		}

		/// <summary>
		/// 記錄欄位值的變化，在欄位繪製前呼叫
		/// </summary>
		public void TrackEdit()
		{
			if(lastSeenName == null)
			{
				lastSeenName = data.AssetName;
			}
			else if(data.AssetName != lastSeenName)
			{
				lastSeenName = data.AssetName;
				fieldEdited = true;
				error = null;
			}
		}

		/// <summary>
		/// 繪製確認與還原按鈕，或上一次確認失敗的錯誤訊息，在欄位繪製後呼叫
		/// </summary>
		public void Draw()
		{
			// 接在欄位後面，這裡的 last rect 是剛畫完的 AssetName 欄位
			if(Event.current.type == EventType.Repaint)
			{
				fieldRect = GUILayoutUtility.GetLastRect();
			}

			if(!HasPendingRename)
			{
				// 值與檔名一致就是這輪改名的終點，旗標留著會讓之後在 Project 視窗改檔名也冒出按鈕
				fieldEdited = false;

				if(!string.IsNullOrEmpty(error))
				{
					EditorGUILayout.HelpBox(error, MessageType.Error);
				}

				return;
			}

			DrawButtons();
			RevertOnClickOutside();
		}

		private void DrawButtons()
		{
			var rect = EditorGUILayout.BeginHorizontal();

			// Layout 事件算不出 rect，只有重繪那幀的值可信
			if(Event.current.type == EventType.Repaint)
			{
				buttonsRect = rect;
			}

			var originalBackground = GUI.backgroundColor;
			var originalColor = GUI.color;

			// 欄位的 GUIColor 會一路套到這裡，不歸零按鈕就會是琥珀色疊上綠紅的濁色
			GUI.color = Color.white;

			using(new EditorGUI.DisabledScope(!data.IsAssetNameLegal()))
			{
				GUI.backgroundColor = ConfirmButtonColor;

				if(GUILayout.Button("確認"))
				{
					Confirm();
				}
			}

			GUI.backgroundColor = RevertButtonColor;

			if(GUILayout.Button("還原"))
			{
				Revert();
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

			if(fieldRect.Contains(mouse) || buttonsRect.Contains(mouse)) return;

			Revert();
		}

		private void Confirm()
		{
			if(!AssetRenamer.TryRename(data, data.AssetName, out var failure))
			{
				error = failure;
				data.AssetName = CurrentFileName;
			}

			End();
		}

		private void Revert()
		{
			data.AssetName = CurrentFileName;
			error = null;
			End();
		}

		private void End()
		{
			// 只改值不夠，游標留在欄位裡就還是編輯狀態，接著打字又會進入未確認
			GUIUtility.keyboardControl = 0;
			EditorGUIUtility.editingTextField = false;

			fieldEdited = false;
			lastSeenName = data.AssetName;
		}
	#endif
	}
}