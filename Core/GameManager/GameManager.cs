using Rino.GameFramework.GameManagerBase;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Rino.GameFramework.Localization.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rino.GameFramework.GameManager
{
    /// <summary>
    /// 遊戲管理主視窗
    /// </summary>
    public class GameManager : OdinMenuEditorWindow
    {
        private GameManagerTabSetting tabSetting;
        private const int MaxButtonsPerRow = 5;

        private GameEditorMenuBase menu;
        private bool needsMenuRebuild;
        private readonly Dictionary<Type, GameEditorMenuBase> menuCache = new();

        /// <summary>
        /// 開啟 GameManager 視窗
        /// </summary>
        [MenuItem("Tools/GameManager", priority = -10)]
        public static void OpenWindow()
        {
            var window = GetWindow<GameManager>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(1500, 800);
        }

        /// <summary>
        /// 初始化視窗
        /// </summary>
        protected override void Initialize()
        {
            tabSetting = RinoEditorUtility.FindAsset<GameManagerTabSetting>();

            if (tabSetting == null)
            {
                var data = CreateInstance<GameManagerTabSetting>();
                RinoEditorUtility.CreateSOData(data, "Data/GameManager/Tab");
                tabSetting = data;
            }

            TrySetFirstValidMenu();
        }

        /// <summary>
        /// 繪製視窗
        /// </summary>
        protected override void OnImGUI()
        {
            DrawToolbar();

            if (menu == null)
            {
                TrySetFirstValidMenu();
            }

            if (menu != null)
            {
                DrawWindowTab();
                MenuWidth = menu.HasMenuTree ? menu.MenuWidth : 0;

                if (needsMenuRebuild)
                {
                    needsMenuRebuild = false;
					menu.ForceMenuTreeRebuild();
                    ForceMenuTreeRebuild();
                }
            }

            base.OnImGUI();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            if (RinoEditorUtility.ToolbarButtonWithIcon("多語系設定", SdfIconType.Translate))
            {
                RinoLocalizationEditorWindow.OpenFromMenu();
            }

            EditorGUILayout.EndHorizontal();
        }

		/// <summary>
        /// 建立選單樹
        /// </summary>
        /// <returns>選單樹</returns>
        protected override OdinMenuTree BuildMenuTree()
        {
            if (menu == null)
            {
                var tree = new OdinMenuTree { { "頁籤設定", tabSetting } };
				return tree;
            }

            menu.EnsureInitialized();
            return menu.MenuTree;
        }

		private void TrySetFirstValidMenu()
        {
            if (tabSetting == null) return;

            var tab = tabSetting.Tabs.FirstOrDefault(t => t.CorrespondingWindowType != null);
            if (tab != null)
            {
                SwitchMenu(tab.CorrespondingWindowType);
            }
        }

		private void SwitchMenu(Type windowType)
        {
            if (windowType == null) return;

            var newMenu = GetOrCreateMenu(windowType);
            if (newMenu == null || menu == newMenu) return;

            menu = newMenu;
            menu.EnsureInitialized();
            needsMenuRebuild = true;
        }

		private GameEditorMenuBase GetOrCreateMenu(Type windowType)
        {
            if (windowType == null) return null;

            if (!menuCache.TryGetValue(windowType, out var cachedMenu))
            {
                cachedMenu = Activator.CreateInstance(windowType) as GameEditorMenuBase;
                menuCache[windowType] = cachedMenu;
            }

            return cachedMenu;
        }

		private void DrawWindowTab()
        {
            var buttonCount = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var tab in tabSetting.Tabs.Where(t => t.CorrespondingWindowType != null))
            {
                if (buttonCount >= MaxButtonsPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    buttonCount = 0;
                }

                var tabMenu = GetOrCreateMenu(tab.CorrespondingWindowType);
                EditorGUILayout.BeginVertical(GUILayout.MaxHeight(30));

                if (SirenixEditorGUI.SDFIconButton(tabMenu.TabName, 5f, tab.TabIcon))
                {
                    SwitchMenu(tab.CorrespondingWindowType);
                }

                EditorGUILayout.EndVertical();
                buttonCount++;
            }

            EditorGUILayout.EndHorizontal();
        }
	}
}
