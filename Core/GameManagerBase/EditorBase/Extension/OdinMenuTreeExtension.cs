using Sumorin.GameFramework.SumorinUtility;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Linq;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Sumorin.GameFramework.GameManagerBase
{
	/// <summary>
	/// OdinMenuTree 擴充方法
	/// </summary>
	public static class OdinMenuTreeExtension
	{
		private const float DeleteButtonSize = 20f;
		private const float DeleteButtonRightPadding = 5f;
		private const float LabelButtonSpacing = 4f;

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
		public static OdinMenuTree AddAllAssets<T>(this OdinMenuTree tree, string menuPath, string path, bool drawDelete = true) where T: SODataBase
		{
			var menuItems = tree.AddAllAssetsAtPath(menuPath, "Assets/" + path, typeof(T), true).ToList();

			if(drawDelete)
			{
				menuItems.ForEach(DrawDelete<T>);
			}

			menuItems.ForEach(UseDataNameAsMenuName);

			tree.EnumerateTree().AddIcons<IconIncludedData>(x => x.Icon);
			return tree;
		}

		/// <summary>
		/// 以資料的本地化顯示名稱（DataName）作為選單名稱，未設定時保留原檔名；
		/// 掛 OnDrawItem 每次重繪重算，DataName 變更後即時反映。
		/// </summary>
		/// <param name="menuItem">選單項目</param>
		private static void UseDataNameAsMenuName(OdinMenuItem menuItem)
		{
			if(menuItem.Value is not SODataBase data) return;

			void Apply()
			{
				if(data.DataName.IsNullOrEmpty()) return;

				var displayName = ResolveDataName(data.DataName);
				if(!string.IsNullOrEmpty(displayName))
				{
					menuItem.Name = displayName;
				}
			}

			Apply();                              // 初始名稱（搜尋／首次顯示）
			menuItem.OnDrawItem += _ => Apply();  // 每次重繪即時更新
		}

		/// <summary>
		/// 解析 DataName 在主要語言下的顯示文字；無翻譯值時退回字串表 entry key
		/// </summary>
		/// <param name="dataName">本地化顯示名稱</param>
		/// <returns>主要語言翻譯值，無則回傳 entry key</returns>
		private static string ResolveDataName(LocalizedString dataName)
		{
			var key = dataName.TableEntryReference.Key;
			var collection = LocalizationEditorSettings.GetStringTableCollection(dataName.TableReference);

			if(collection == null) return key;

			// 編輯選單時沒有 active locale，改取專案主要語言（無則退回第一個語言）
			var locale = (LocalizationSettings.HasSettings ? LocalizationSettings.ProjectLocale : null)
						 ?? LocalizationEditorSettings.GetLocales()?.FirstOrDefault();

			if(locale == null) return key;

			var value = (collection.GetTable(locale.Identifier) as StringTable)?.GetEntry(key)?.Value;

			return string.IsNullOrEmpty(value) ? key : value;
		}

		/// <summary>
		/// 繪製刪除按鈕
		/// </summary>
		/// <typeparam name="T">資料類型</typeparam>
		/// <param name="menuItem">選單項目</param>
		public static void DrawDelete<T>(OdinMenuItem menuItem) where T: SODataBase
		{
			var owner = menuItem.MenuTree.EnumerateTree().Select(item => item.Value).OfType<GameEditorMenuBase>().FirstOrDefault();

			menuItem.OnDrawItem += _ =>
			{
				if(menuItem.Value == null || !(menuItem.Value is T)) return;

				var rect = menuItem.Rect;
				if(rect.width <= 1f) return;

				var buttonRect = new Rect(
					rect.xMax - DeleteButtonRightPadding - DeleteButtonSize,
					rect.y + (rect.height - DeleteButtonSize) * 0.5f,
					DeleteButtonSize,
					DeleteButtonSize
				);

				// 用與該列相同底色的遮罩蓋掉超出按鈕的名稱
				DrawNameMask(menuItem, rect, buttonRect.xMin - LabelButtonSpacing);

				// 整列加上完整名稱 Tooltip，hover 即可顯示
				GUI.Label(rect, new GUIContent(string.Empty, menuItem.Name));

				if(SirenixEditorGUI.IconButton(buttonRect, EditorIcons.X))
				{
					DeletePopUp.OpenWindow((T)menuItem.Value, buttonRect, () => owner?.ForceMenuTreeRebuild());
				}
			};
		}

		/// <summary>
		/// 以該列底色遮罩右側區域，使名稱不超出刪除按鈕
		/// </summary>
		/// <param name="menuItem">選單項目</param>
		/// <param name="rect">該列範圍</param>
		/// <param name="maskLeft">遮罩左緣</param>
		private static void DrawNameMask(OdinMenuItem menuItem, Rect rect, float maskLeft)
		{
			if(maskLeft >= rect.xMax) return;

			var maskRect = Rect.MinMaxRect(maskLeft, rect.y, rect.xMax, rect.yMax);

			// 先鋪不透明底色蓋住文字，再疊上選單色調與狀態色還原該列外觀
			SirenixEditorGUI.DrawSolidRect(maskRect, SirenixGUIStyles.EditorWindowBackgroundColor);
			SirenixEditorGUI.DrawSolidRect(maskRect, SirenixGUIStyles.MenuBackgroundColor);

			var style = menuItem.Style ?? menuItem.MenuTree?.DefaultMenuStyle;

			if(menuItem.IsSelected && style != null)
			{
				var selectedColor = GUIHelper.CurrentWindowHasFocus ? style.SelectedColor : style.SelectedInactiveColor;
				SirenixEditorGUI.DrawSolidRect(maskRect, selectedColor);
			}
			else if(rect.Contains(Event.current.mousePosition))
			{
				SirenixEditorGUI.DrawSolidRect(maskRect, SirenixGUIStyles.MouseOverBgOverlayColor);
			}
		}
	}
}