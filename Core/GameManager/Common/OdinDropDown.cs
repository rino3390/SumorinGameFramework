using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rino.GameFramework.GameManager
{
    /// <summary>
    /// Odin 下拉選單工具
    /// </summary>
    public class OdinDropDown
    {
        /// <summary>
        /// 取得語言下拉選項
        /// </summary>
        /// <returns>語言選項清單</returns>
        public static IEnumerable Languages()
        {
            return null;
        }

        private static T GetDataOverview<T>() where T : ScriptableObject
        {
            var data = AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();
            return data;
        }
    }
}
