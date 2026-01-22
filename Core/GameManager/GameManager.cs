using Rino.GameFramework.GameManagerBase;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
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
            if (menu == null)
            {
                TrySetFirstValidMenu();
            }

            if (menu != null)
            {
                DrawWindowTab();
                MenuWidth = menu.MenuWidth;

                if (needsMenuRebuild)
                {
                    needsMenuRebuild = false;
                    ForceMenuTreeRebuild();
                }
            }

            base.OnImGUI();
        }

        /// <summary>
        /// 建立選單樹
        /// </summary>
        /// <returns>選單樹</returns>
        protected override OdinMenuTree BuildMenuTree()
        {
            if (menu == null)
            {
                var tree = new OdinMenuTree();
                tree.Add("頁籤設定", tabSetting);
                return tree;
            }

            menu.EnsureInitialized();
            return menu.MenuTree;
        }

        private void TrySetFirstValidMenu()
        {
            if (tabSetting == null) return;

            foreach (var tab in tabSetting.Tabs)
            {
                if (tab.CorrespondingWindow != null)
                {
                    SetCurrentMenu(tab.CorrespondingWindow);
                    ForceMenuTreeRebuild();
                    return;
                }
            }
        }

        private void SwitchMenu(GameEditorMenuBase newMenu)
        {
            if (newMenu == null || menu == newMenu) return;

            SetCurrentMenu(newMenu);
            needsMenuRebuild = true;
        }

        private void SetCurrentMenu(GameEditorMenuBase newMenu)
        {
            if (newMenu == null) return;

            menu = newMenu;
            menu.EnsureInitialized();
        }

        private void DrawWindowTab()
        {
            var buttonCount = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var tab in tabSetting.Tabs)
            {
                if (buttonCount >= MaxButtonsPerRow)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    buttonCount = 0;
                }

                EditorGUILayout.BeginVertical(GUILayout.MaxHeight(30));

                if (SirenixEditorGUI.SDFIconButton(tab.CorrespondingWindow.TabName, 5f, tab.TabIcon))
                {
                    SwitchMenu(tab.CorrespondingWindow);
                }

                EditorGUILayout.EndVertical();
                buttonCount++;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
