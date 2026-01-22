#if UNITY_EDITOR
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.Utilities.Editor;
#endif
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// 資料集合基底類別，管理 SODataBase 衍生類別的清單
    /// </summary>
    /// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
    public class DataSet<T> : ScriptableObject where T : SODataBase
    {
        /// <summary>
        /// 資料清單
        /// </summary>
        [ListDrawerSettings(OnTitleBarGUI = nameof(DrawCustomRefreshButton), DraggableItems = false, NumberOfItemsPerPage = 20)]
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

            if (data == null)
            {
                throw new ArgumentNullException(nameof(dataId), $"找不到 dataId: {dataId}");
            }

            return data;
        }

        /// <summary>
        /// 取得隨機一筆資料
        /// </summary>
        /// <returns>隨機資料</returns>
        /// <exception cref="ArgumentNullException">資料清單為空時拋出</exception>
        public T GetRandomData()
        {
            if (Datas.Count == 0)
            {
                throw new ArgumentNullException(nameof(Datas), "找不到任何資料");
            }

            return Datas[Random.Range(0, Datas.Count)];
        }

#if UNITY_EDITOR
        /// <summary>
        /// 繪製自訂重新整理按鈕（Editor 專用）
        /// </summary>
        private void DrawCustomRefreshButton()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                Datas = RinoEditorUtility.FindAssets<T>();
                RinoEditorUtility.SaveSOData(this);
            }
        }

        /// <summary>
        /// 建立下拉選單資料來源（Editor 專用）
        /// </summary>
        /// <returns>下拉選單項目集合</returns>
        public IEnumerable DrawDropDown()
        {
            return Datas.Select(data => new ValueDropdownItem(data.AssetName, data.Id));
        }
#endif
    }
}
