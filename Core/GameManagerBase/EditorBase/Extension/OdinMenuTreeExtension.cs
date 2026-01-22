using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// OdinMenuTree 擴充方法
    /// </summary>
    public static class OdinMenuTreeExtension
    {
        /// <summary>
        /// 將物件加入選單樹作為自身選單項目
        /// </summary>
        /// <param name="tree">選單樹</param>
        /// <param name="instance">要加入的物件</param>
        /// <param name="path">選單路徑</param>
        /// <returns>選單樹</returns>
        public static OdinMenuTree AddSelfMenu(this OdinMenuTree tree, object instance, string path = "Home")
        {
            tree.Add(path, instance);
            return tree;
        }

        /// <summary>
        /// 將指定路徑下所有資產加入選單樹
        /// </summary>
        /// <typeparam name="T">資料類型</typeparam>
        /// <param name="tree">選單樹</param>
        /// <param name="menuPath">選單路徑</param>
        /// <param name="path">資產路徑</param>
        /// <param name="drawDelete">是否繪製刪除按鈕</param>
        /// <returns>選單樹</returns>
        public static OdinMenuTree AddAllAssets<T>(this OdinMenuTree tree, string menuPath, string path, bool drawDelete = true) where T : SODataBase
        {
            var menuItems = tree.AddAllAssetsAtPath(menuPath, "Assets/" + path, typeof(T), true);

            if (drawDelete)
            {
                menuItems.ForEach(DrawDelete<T>);
            }

            tree.EnumerateTree().AddIcons<IconIncludedData>(x => x.Icon);
            return tree;
        }

        /// <summary>
        /// 繪製刪除按鈕
        /// </summary>
        /// <typeparam name="T">資料類型</typeparam>
        /// <param name="menuItem">選單項目</param>
        public static void DrawDelete<T>(OdinMenuItem menuItem) where T : SODataBase
        {
            menuItem.OnDrawItem += _ =>
            {
                if (menuItem.Value == null || !(menuItem.Value is T)) return;

                var buttonRect = new Rect(new Vector2(menuItem.Rect.width - 50, menuItem.Rect.y + 5), new Vector2(20, 20));

                if (GUI.Button(buttonRect, "") || SirenixEditorGUI.IconButton(buttonRect, EditorIcons.X))
                {
                    DeletePopUp.OpenWindow((T)menuItem.Value, buttonRect);
                }
            };
        }
    }
}
