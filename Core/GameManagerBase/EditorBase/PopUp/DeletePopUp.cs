using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using UnityEditor;
using UnityEngine;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// 刪除資料確認彈出視窗
    /// </summary>
    [Serializable]
    public class DeletePopUp
    {
        [Title("刪除確認")]
        [InlineEditor, ShowInInspector, ReadOnly]
        private SODataBase soData;

        private string assetPath;
        private static OdinEditorWindow popupWindow;

        /// <summary>
        /// 初始化刪除彈出視窗
        /// </summary>
        /// <param name="soData">要刪除的資料</param>
        public DeletePopUp(SODataBase soData)
        {
            this.soData = soData;
            assetPath = AssetDatabase.GetAssetPath(soData);
        }

        /// <summary>
        /// 開啟刪除確認視窗
        /// </summary>
        /// <param name="soData">要刪除的資料</param>
        /// <param name="rect">視窗位置</param>
        public static void OpenWindow(SODataBase soData, Rect rect)
        {
            popupWindow = OdinEditorWindow.InspectObjectInDropDown(new DeletePopUp(soData), rect, 300);
        }

        /// <summary>
        /// 執行刪除
        /// </summary>
        [Button]
        public void Delete()
        {
            AssetDatabase.DeleteAsset(assetPath);
            popupWindow.Close();
        }
    }
}
