using Rino.GameFramework.RinoUtility;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using UnityEngine;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// 建立新資料的 Editor 視窗基底類別
    /// </summary>
    /// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
    public abstract class CreateNewDataEditor<T> : GameEditorMenuBase where T : SODataBase
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
        [BoxGroup("$CreateDataGroupLabel")]
        [InlineEditor(InlineEditorObjectFieldModes.Hidden)]
        public T Data;

        [Required("程式端尚未實作資料整合方法")]
        [ShowInInspector, InlineEditor(InlineEditorObjectFieldModes.Hidden), HideLabel]
        [PropertySpace(10)]
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

            dataSet = RinoEditorUtility.FindAssetWithInheritance<DataSet<T>>();

            if (dataSet == null)
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

            if (addAllDataForMenu)
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
            if (!Data.IsDataLegal()) return;
            RinoEditorUtility.CreateSOData(Data, DataRootPath + Data.AssetName);
            SetNewData();
            ForceMenuTreeRebuild();
        }

        private void SetNewData()
        {
            Data = ScriptableObject.CreateInstance<T>();
            Data.Id = GUID.NewGuid();
            var root = DataRootPath.Split('/');
            Data.AssetName = root[^2] + " - " + Data.Id;
        }

        private void CreateDataSet()
        {
            var dataSetType = RinoEditorUtility.GetDerivedClasses<DataSet<T>>().First();

            if (dataSetType == null)
            {
                return;
            }

            var newDataSet = ScriptableObject.CreateInstance(dataSetType);
            RinoEditorUtility.CreateSOData(newDataSet, "Data/Set/" + typeof(T).Name + "DataSet");
            dataSet = (DataSet<T>)newDataSet;
            ForceMenuTreeRebuild();
        }
    }
}
