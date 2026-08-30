using Sumorin.SumorinUtility;
using Sumorin.SumorinUtility.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 建立新資料的 Editor 視窗基底類別
	/// </summary>
	/// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
	public abstract class CreateNewDataEditor<T>: GameEditorMenuBase where T: SODataBase
	{
		/// <summary>
		/// 資料根目錄路徑（相對於 Assets）
		/// </summary>
		protected abstract string DataRoot { get; }

		/// <summary>
		/// 資料類型標籤（用於顯示名稱）
		/// </summary>
		protected abstract string DataTypeLabel { get; }

		private string DataRootPath => DataRoot + "/";

		private string CreateDataGroupLabel => "新增" + DataTypeLabel;

		/// <summary>
		/// 新建的資料物件
		/// </summary>
		/// <remarks>
		/// 標 <see cref="DontValidateAttribute" /> 關掉驗證。這是還在填寫的表單，欄位空著是常態，
		/// 逐欄跳紅字沒有意義。缺什麼由 Create 按鈕下方的提示統一說，按鈕本身也會鎖住。
		/// 資產建立之後才會被 Validator 掃到。
		/// </remarks>
		[BoxGroup("$CreateDataGroupLabel")]
		[InlineEditor(InlineEditorObjectFieldModes.Hidden)]
		[DontValidate]
		public T Data;

		/// <summary>
		/// 資料集合，新建的資產會加入其中供執行期載入
		/// </summary>
		/// <remarks>
		/// 不繪製在畫面上，左側選單已列出資料夾內的全部資產，再畫一份只會重複且可能漏列。
		/// </remarks>
		private DataSet<T> dataSet;

		private readonly bool addAllDataForMenu;
		private readonly bool drawDelete;

		/// <summary>
		/// 初始化 CreateNewDataEditor
		/// </summary>
		/// <param name="addAllDataForMenu">是否將所有資料加入選單</param>
		/// <param name="drawDelete">是否繪製刪除按鈕</param>
		protected CreateNewDataEditor(bool addAllDataForMenu = true, bool drawDelete = true)
		{
			this.drawDelete = drawDelete;
			this.addAllDataForMenu = addAllDataForMenu;
		}

		/// <summary>
		/// 初始化模組
		/// </summary>
		protected override void OnInitialize()
		{
			SetNewData();

			dataSet = SumorinEditorUtility.FindAssetWithInheritance<DataSet<T>>();

			if(dataSet == null)
			{
				CreateDataSet();
			}
		}

		/// <summary>
		/// 建立選單樹
		/// </summary>
		/// <returns>選單樹</returns>
		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = SetTree().AddSelfMenu(this, DataTypeLabel);

			if(addAllDataForMenu)
			{
				tree.AddAllAssets<T>(DataTypeLabel, DataRootPath, drawDelete);
			}

			return tree;
		}

		[BoxGroup("$CreateDataGroupLabel")]
		[OnInspectorGUI, ShowIf("@!Data.IsDataLegal()")]
		private void CreateNewDataInfoBox()
		{
			SirenixEditorGUI.ErrorMessageBox("資料尚未正確設定");
		}

		[BoxGroup("$CreateDataGroupLabel")]
		[Button("Create"), DisableIf("@!Data.IsDataLegal()"), GUIColor(0.67f, 1f, 0.65f)]
		private void CreateNewData()
		{
			if(!Data.IsDataLegal()) return;

			SumorinEditorUtility.CreateSOData(Data, DataRootPath + Data.AssetName);
			dataSet?.AddData(Data);
			SetNewData();
			ForceMenuTreeRebuild();
		}

		private void SetNewData()
		{
			Data = ScriptableObject.CreateInstance<T>();
			Data.IdName = GUID.NewGuid();
			Data.AssetName = DataRootPath.Split('/')[^2] + " - " + Data.IdName;
		}

		private void CreateDataSet()
		{
			var dataSetType = SumorinEditorUtility.GetDerivedClasses<DataSet<T>>().First();

			if(dataSetType == null)
			{
				return;
			}

			var newDataSet = ScriptableObject.CreateInstance(dataSetType);
			// 用集合型別自己的名稱。接在資料型別後面會變成 BuffDataDataSet 這種重複的檔名
			SumorinEditorUtility.CreateSOData(newDataSet, "Data/Set/" + dataSetType.Name);
			dataSet = (DataSet<T>)newDataSet;
			ForceMenuTreeRebuild();
		}
	}
}