using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Configs;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Rino.GameFramework.Localization.Editor
{
    /// <summary>
    /// 擴展的本地化編輯器視窗，新增 CSV 匯入匯出和複製 Entry 功能
    /// </summary>
    public class RinoLocalizationEditorWindow : OdinLocalizationEditorWindow
    {
        private const float ExtendedToolbarHeight = 24f;

        [MenuItem("Tools/Rino/Localization Editor", priority = 10_101)]
        public new static void OpenFromMenu()
        {
            var wnd = GetWindow<RinoLocalizationEditorWindow>();
            wnd.titleContent = new GUIContent("Rino Localization");
            wnd.MenuWidth = 300.0f;
        }

        private object rinoLastSelection;

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree
            {
                Config =
                {
                    AutoHandleKeyboardNavigation = false,
                    DrawSearchToolbar = true
                },
                DefaultMenuStyle =
                {
                    Height = 28,
                    AlignTriangleLeft = true,
                    TrianglePadding = 0.0f
                }
            };

            MenuBackgroundColor = OdinLocalizationGUI.MenuBackground;

            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                return tree;

            var createMenu = new OdinLocalizationCreateTableMenu();

            tree.Add("Create Table", createMenu, SdfIconType.Plus);
            tree.Add("User Config", OdinLocalizationConfig.Instance, SdfIconType.GearFill);

            tree.Selection.SelectionChanged += type =>
            {
                switch (type)
                {
                    case SelectionChangedType.ItemAdded:
                        if (rinoLastSelection != null)
                        {
                            switch (rinoLastSelection)
                            {
                                case OdinAssetTableCollectionEditor assetCollection:
                                    assetCollection.DetachEvents();
                                    break;

                                case OdinStringTableCollectionEditor stringCollection:
                                    stringCollection.DetachEvents();
                                    break;
                            }

                            State.MetadataTree?.Dispose();
                            State.MetadataTree = null;
                        }

                        switch (tree.Selection.SelectedValue)
                        {
                            case OdinAssetTableCollectionEditor assetCollection:
                                assetCollection.OnSelectInWindow();

                                if (assetCollection.SelectionType == OdinTableSelectionType.TableEntry &&
                                    State.CurrentTopTab == RightMenuTopTabs.Metadata)
                                {
                                    assetCollection.UpdateMetadataViewForEntry(assetCollection.CurrentSelectedEntry);
                                }
                                break;

                            case OdinStringTableCollectionEditor stringCollection:
                                stringCollection.OnSelectInWindow();

                                if (stringCollection.SelectionType == OdinTableSelectionType.TableEntry &&
                                    State.CurrentTopTab == RightMenuTopTabs.Metadata)
                                {
                                    stringCollection.UpdateMetadataViewForEntry(stringCollection.CurrentSelectedEntry);
                                }
                                break;

                            case OdinLocalizationCreateTableMenu createTableMenu:
                                createTableMenu.Locales.Clear();

                                foreach (var locale in LocalizationEditorSettings.GetLocales())
                                {
                                    createTableMenu.Locales.Add(new OdinLocalizationCreateTableMenu.LocaleItem
                                    {
                                        Locale = locale,
                                        Enabled = true
                                    });
                                }
                                break;
                        }

                        rinoLastSelection = MenuTree.Selection.SelectedValue;
                        break;
                }
            };

            var collectionGUIDs = AssetDatabase.FindAssets($"t:{nameof(LocalizationTableCollection)}");

            foreach (var guid in collectionGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var collection = AssetDatabase.LoadAssetAtPath<LocalizationTableCollection>(assetPath);

                var assetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(collection.TableCollectionNameReference);

                if (assetTableCollection != null)
                {
                    var guiCollection = new OdinAssetTableCollectionEditor(assetTableCollection, this, State);

                    assetPath = assetPath.Replace(".asset", string.Empty);
                    if (assetPath.StartsWith("Assets/"))
                    {
                        assetPath = assetPath.Remove(0, "Assets/".Length);
                    }

                    tree.Add(assetPath, guiCollection, SdfIconType.Table);
                    continue;
                }

                var stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(collection.TableCollectionNameReference);

                if (stringTableCollection != null)
                {
                    var guiCollection = new RinoStringTableCollectionEditor(stringTableCollection, this, State);

                    assetPath = assetPath.Replace(".asset", string.Empty);
                    if (assetPath.StartsWith("Assets/"))
                    {
                        assetPath = assetPath.Remove(0, "Assets/".Length);
                    }

                    tree.Add(assetPath, guiCollection, SdfIconType.LayoutTextWindow);
                }
            }

            foreach (var treeMenuItem in tree.EnumerateTree())
            {
                if (treeMenuItem.Value != null)
                {
                    if (treeMenuItem.Value is OdinAssetTableCollectionEditor assetEditor)
                    {
                        treeMenuItem.Name = assetEditor.Collection.SharedData.TableCollectionName;
                        assetEditor.MenuItem = treeMenuItem;

                        treeMenuItem.OnDrawItem += item =>
                        {
                            if (Event.current.OnMouseDown(item.Rect, 0, false))
                            {
                                if (Event.current.clickCount > 1)
                                {
                                    EditorGUIUtility.PingObject(assetEditor.Collection);
                                }
                            }
                        };
                        continue;
                    }

                    if (treeMenuItem.Value is OdinStringTableCollectionEditor stringEditor)
                    {
                        treeMenuItem.Name = stringEditor.Collection.SharedData.TableCollectionName;
                        stringEditor.MenuItem = treeMenuItem;

                        treeMenuItem.OnDrawItem += item =>
                        {
                            if (Event.current.OnMouseDown(item.Rect, 0, false))
                            {
                                if (Event.current.clickCount > 1)
                                {
                                    EditorGUIUtility.PingObject(stringEditor.Collection);
                                }
                            }
                        };
                        continue;
                    }

                    continue;
                }

                treeMenuItem.Value = createMenu;
                treeMenuItem.SdfIcon = SdfIconType.FolderFill;

                treeMenuItem.OnDrawItem += item =>
                {
                    var addTableRect = item.Rect.AlignRight(20).SubX(14);
                    var isMouseOver = Event.current.IsMouseOver(addTableRect);

                    if (EditorGUIUtility.isProSkin)
                    {
                        SdfIcons.DrawIcon(addTableRect.AlignCenter(16, 16),
                            SdfIconType.Plus,
                            isMouseOver ? new Color(1, 1, 1, 0.8f) : new Color(1, 1, 1, 0.4f));
                    }
                    else
                    {
                        SdfIcons.DrawIcon(addTableRect.AlignCenter(16, 16),
                            SdfIconType.Plus,
                            isMouseOver ? new Color(0, 0, 0, 0.8f) : new Color(0, 0, 0, 0.4f));
                    }

                    if (Event.current.OnMouseDown(item.Rect, 0, false))
                    {
                        createMenu.Folder = treeMenuItem.GetFullPath();
                    }
                };
            }

            return tree;
        }

        private int pendingClearFocusFrames;
        private Vector2 pendingClickPosition;
        private bool hasPendingClick;

        protected override void OnImGUI()
        {
            // 持續清除焦點直到計數歸零
            if (pendingClearFocusFrames > 0)
            {
                EditorGUIUtility.editingTextField = false;
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;

                if (Event.current.type == EventType.Repaint)
                {
                    pendingClearFocusFrames--;
                    if (pendingClearFocusFrames == 0 && hasPendingClick)
                    {
                        // 清除完成後，模擬點擊
                        hasPendingClick = false;
                        SimulateClick(pendingClickPosition);
                    }
                    Repaint();
                }
            }

            // 最優先攔截 Tab 鍵，在任何 GUI 處理之前
            if (Event.current.keyCode == KeyCode.Tab &&
                (Event.current.type == EventType.KeyDown || Event.current.rawType == EventType.KeyDown))
            {
                if (MenuTree?.Selection?.SelectedValue is RinoStringTableCollectionEditor editor)
                {
                    // 強制結束 TextField 編輯狀態並清除焦點
                    EditorGUIUtility.editingTextField = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    GUI.FocusControl(null);

                    editor.NavigateToNextColumn(Event.current.shift);
                    Event.current.Use();

                    // 計算目標儲存格的點擊位置
                    pendingClickPosition = CalculateTargetCellPosition(editor);
                    hasPendingClick = true;

                    // 持續清除焦點 2 幀，然後模擬點擊
                    pendingClearFocusFrames = 2;

                    Repaint();
                    return;
                }
            }

            HandleKeyboardShortcuts();

            // 先繪製擴展 toolbar 的背景
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                DrawExtendedToolbarBackground();
            }

            // 將整個內容區域向下偏移
            var contentRect = new Rect(0, ExtendedToolbarHeight, position.width, position.height - ExtendedToolbarHeight);
            GUI.BeginGroup(contentRect);
            {
                // 調整 position 讓 base.OnImGUI 認為視窗較小
                var originalPosition = position;
                try
                {
                    base.OnImGUI();
                }
                finally
                {
                    // position 是 readonly，無法還原，但 GUI.EndGroup 會處理
                }
            }
            GUI.EndGroup();

            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                return;

            DrawExtendedToolbarButtons();
        }

        private void DrawExtendedToolbarBackground()
        {
            // 繪製 toolbar 背景
            var toolbarBgRect = new Rect(0, 0, position.width, ExtendedToolbarHeight);
            EditorGUI.DrawRect(toolbarBgRect, EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f));

            // 繪製底部分隔線
            EditorGUI.DrawRect(new Rect(0, ExtendedToolbarHeight - 1, position.width, 1),
                EditorGUIUtility.isProSkin
                    ? new Color(0.15f, 0.15f, 0.15f)
                    : new Color(0.6f, 0.6f, 0.6f));
        }

        private void HandleKeyboardShortcuts()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            if (!(MenuTree?.Selection?.SelectedValue is RinoStringTableCollectionEditor ctrlEditor))
                return;

            if (Event.current.control && Event.current.keyCode == KeyCode.D)
            {
                if (ctrlEditor.CurrentSelectedSharedEntry != null)
                {
                    ctrlEditor.DuplicateSelectedEntry();
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        private void DrawExtendedToolbarButtons()
        {
            var editor = GetCurrentStringTableEditor();
            if (editor == null)
                return;

            // 在頂部 toolbar 繪製按鈕（靠右排列）
            var buttonWidth = 85f;
            var buttonHeight = 20f;
            var spacing = 4f;
            var toolbarY = 2f;

            // 從右側開始計算按鈕位置
            var toolbarRight = position.width - State.RightMenuWidth - 8;
            var duplicateRect = new Rect(toolbarRight - (buttonWidth + 20), toolbarY, buttonWidth + 20, buttonHeight);
            var importRect = new Rect(duplicateRect.x - spacing - buttonWidth, toolbarY, buttonWidth, buttonHeight);
            var exportRect = new Rect(importRect.x - spacing - buttonWidth, toolbarY, buttonWidth, buttonHeight);

            // 快捷鍵提示在按鈕左側
            var hintRect = new Rect(exportRect.x - spacing - 400, toolbarY + 2, 400, buttonHeight);
            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.4f) : new Color(0, 0, 0, 0.4f) }
            };
            GUI.Label(hintRect, "Ctrl+D 複製 | Tab 跳下一格 | Shift+Tab 跳上一格", hintStyle);

            if (GUI.Button(exportRect, "Export CSV", SirenixGUIStyles.MiniButton))
            {
                var path = EditorUtility.SaveFilePanel(
                    "Export CSV",
                    "",
                    editor.Collection.name,
                    "csv");

                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        CsvLocalizationUtility.Export(editor.Collection, path);
                        ShowToast(ToastPosition.BottomLeft,
                            SdfIconType.Check2,
                            $"Exported to {path}",
                            new Color(0.29f, 0.57f, 0.42f),
                            8.0f);
                    }
                    catch (System.Exception e)
                    {
                        ShowToast(ToastPosition.BottomLeft,
                            SdfIconType.ExclamationOctagonFill,
                            $"Export failed: {e.Message}",
                            new Color(0.68f, 0.2f, 0.2f),
                            8.0f);
                        Debug.LogException(e);
                    }
                }
            }

            if (GUI.Button(importRect, "Import CSV", SirenixGUIStyles.MiniButton))
            {
                var path = EditorUtility.OpenFilePanel(
                    "Import CSV",
                    "",
                    "csv");

                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        var count = CsvLocalizationUtility.Import(editor.Collection, path);
                        ShowToast(ToastPosition.BottomLeft,
                            SdfIconType.Check2,
                            $"Imported {count} entries",
                            new Color(0.29f, 0.57f, 0.42f),
                            8.0f);
                        ForceMenuTreeRebuild();
                    }
                    catch (System.Exception e)
                    {
                        ShowToast(ToastPosition.BottomLeft,
                            SdfIconType.ExclamationOctagonFill,
                            $"Import failed: {e.Message}",
                            new Color(0.68f, 0.2f, 0.2f),
                            8.0f);
                        Debug.LogException(e);
                    }
                }
            }

            var hasSelection = editor.CurrentSelectedSharedEntry != null;
            GUI.enabled = hasSelection;

            if (GUI.Button(duplicateRect, "Duplicate Entry", SirenixGUIStyles.MiniButton))
            {
                if (editor is RinoStringTableCollectionEditor rinoEditor)
                {
                    rinoEditor.DuplicateSelectedEntry();
                }
            }

            GUI.enabled = true;

            // 處理拖曳區域點擊來選擇整行
            HandleDragHandleClick(editor);
        }

        /// <summary>
        /// 處理拖曳按鈕區域的點擊，選擇整行
        /// </summary>
        private void HandleDragHandleClick(OdinStringTableCollectionEditor editor)
        {
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
                return;

            // OdinLocalizationConstants 值
            const float dragHandleWidth = 27f;
            const float originalToolbarHeight = 28f;
            const float columnHeight = 38f;
            const float rowHeight = 30f; // OdinLocalizationConstants.ROW_HEIGHT

            // 拖曳區域的 X 範圍
            var dragAreaX = MenuWidth;
            var dragAreaRight = dragAreaX + dragHandleWidth;

            // 拖曳區域的 Y 範圍（extended toolbar + original toolbar + column header 之後）
            var dragAreaTop = ExtendedToolbarHeight + originalToolbarHeight + columnHeight;
            var dragAreaBottom = position.height;

            var mousePos = Event.current.mousePosition;

            // 檢查滑鼠是否在拖曳區域內
            if (mousePos.x >= dragAreaX && mousePos.x <= dragAreaRight &&
                mousePos.y >= dragAreaTop && mousePos.y <= dragAreaBottom)
            {
                // 計算點擊的是哪一行（考慮捲動位置）
                var scrollY = editor.EntryScrollView.PositionY;
                var relativeY = mousePos.y - dragAreaTop + scrollY;
                var clickedRowIndex = (int)(relativeY / rowHeight);

                if (clickedRowIndex >= 0 && clickedRowIndex < editor.SharedEntries.Length)
                {
                    var sharedEntry = editor.SharedEntries[clickedRowIndex];
                    editor.SelectSharedEntry(sharedEntry);
                    Event.current.Use();
                    Repaint();
                }
            }
        }

        private OdinStringTableCollectionEditor GetCurrentStringTableEditor()
        {
            if (MenuTree?.Selection?.SelectedValue is OdinStringTableCollectionEditor editor)
                return editor;

            return null;
        }

        private Vector2 CalculateTargetCellPosition(RinoStringTableCollectionEditor editor)
        {
            // OdinLocalizationConstants 值
            const float originalToolbarHeight = 28f;
            const float columnHeight = 38f;
            const float rowHeight = 30f;
            const float dragHandleWidth = 27f;
            const float deleteButtonWidth = 27f;

            var targetRow = editor.LastNavigationTargetRowIndex;
            var targetCol = editor.LastNavigationTargetColumnIndex;

            // 計算 Y 位置（考慮滾動）
            var scrollY = editor.EntryScrollView.PositionY;
            var cellY = ExtendedToolbarHeight + originalToolbarHeight + columnHeight +
                        targetRow * rowHeight - scrollY + rowHeight / 2;

            // 計算 X 位置（使用實際欄位寬度）
            var tableAreaStart = MenuWidth + dragHandleWidth + deleteButtonWidth;
            float cellX = tableAreaStart;

            // 累加前面欄位的寬度
            for (var i = 0; i < targetCol && i < editor.GUITables.Count; i++)
            {
                cellX += editor.GUITables[i].Width;
            }

            // 加上目標欄位的一半寬度
            if (targetCol < editor.GUITables.Count)
            {
                cellX += editor.GUITables[targetCol].Width / 2;
            }

            return new Vector2(cellX, cellY);
        }

        private void SimulateClick(Vector2 clickPosition)
        {
            // 將 GUI 座標轉換為螢幕座標，再轉回視窗座標
            // 這樣可以確保座標在正確的座標系統中
            var screenPos = GUIUtility.GUIToScreenPoint(clickPosition);
            var windowPos = screenPos - new Vector2(position.x, position.y);

            // 使用 delayCall 確保在下一幀執行
            EditorApplication.delayCall += () =>
            {
                // 發送點擊事件
                var mouseDown = new Event
                {
                    type = EventType.MouseDown,
                    button = 0,
                    mousePosition = windowPos,
                    clickCount = 1
                };
                SendEvent(mouseDown);

                var mouseUp = new Event
                {
                    type = EventType.MouseUp,
                    button = 0,
                    mousePosition = windowPos,
                    clickCount = 1
                };
                SendEvent(mouseUp);
                Repaint();
            };
        }
    }
}
