using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Rino.GameFramework.Localization.Editor
{
    /// <summary>
    /// 擴展的 StringTableCollection 編輯器，新增複製 Entry 和 Tab 導航功能
    /// </summary>
    public class RinoStringTableCollectionEditor : OdinStringTableCollectionEditor
    {
        /// <summary>
        /// 最後一次 Tab 導航的目標行索引
        /// </summary>
        public int LastNavigationTargetRowIndex { get; private set; } = -1;

        /// <summary>
        /// 最後一次 Tab 導航的目標欄位索引
        /// </summary>
        public int LastNavigationTargetColumnIndex { get; private set; } = -1;

        /// <summary>
        /// 當前選中格子的螢幕座標中心點
        /// </summary>
        public Vector2 CurrentSelectedCellCenter { get; private set; }

        /// <summary>
        /// 是否有有效的選中格子位置
        /// </summary>
        public bool HasValidSelectedCellPosition { get; private set; }

        public RinoStringTableCollectionEditor(
            StringTableCollection collection,
            OdinMenuEditorWindow relatedWindow,
            OdinLocalizationEditorWindow.WindowState windowState)
            : base(collection, relatedWindow, windowState)
        {
        }

        /// <summary>
        /// 取得 Control ID Hint（用於計算 TextField 的 control ID）
        /// </summary>
        public int GetControlIdHint() => ControlIdHint;

        /// <summary>
        /// 記錄選中格子的位置（在繪製時由外部呼叫）
        /// </summary>
        public void RecordSelectedCellPosition(Rect cellRect)
        {
            // 將 GUI 座標轉換為螢幕座標
            var screenPos = GUIUtility.GUIToScreenPoint(cellRect.center);
            CurrentSelectedCellCenter = screenPos;
            HasValidSelectedCellPosition = true;
        }

        /// <summary>
        /// 清除選中格子位置記錄
        /// </summary>
        public void ClearSelectedCellPosition()
        {
            HasValidSelectedCellPosition = false;
        }

        /// <summary>
        /// 複製當前選中的 Entry
        /// </summary>
        public void DuplicateSelectedEntry()
        {
            if (CurrentSelectedSharedEntry == null)
            {
                Debug.LogWarning("No entry selected to duplicate");
                return;
            }

            var originalKey = CurrentSelectedSharedEntry.Key;
            var newKey = GenerateUniqueKey(originalKey);

            GUITables.UndoRecordCollection(Collection.SharedData, "Duplicate Shared Entry");

            var newSharedEntry = Collection.SharedData.AddKey(newKey);

            foreach (var stringTable in Collection.StringTables)
            {
                Undo.RecordObject(stringTable, "Duplicate Shared Entry");

                var originalEntry = stringTable.GetEntry(CurrentSelectedSharedEntry.Id);
                if (originalEntry != null)
                {
                    var newEntry = stringTable.AddEntry(newSharedEntry.Id, originalEntry.Value);
                    newEntry.IsSmart = originalEntry.IsSmart;
                }
            }

            OdinLocalizationEvents.RaiseTableEntryAdded(Collection, newSharedEntry);

            GUITables.SetDirty(Collection.SharedData);
            foreach (var stringTable in Collection.StringTables)
            {
                EditorUtility.SetDirty(stringTable);
            }

            SelectSharedEntry(newSharedEntry);
        }

        /// <summary>
        /// 導航到下一個或上一個欄位（包含 Key 欄位，跨行）
        /// </summary>
        /// <param name="reverse">是否反向導航（上一個）</param>
        /// <returns>是否成功導航</returns>
        public bool NavigateToNextColumn(bool reverse)
        {
            if (GUITables.Count == 0)
                return false;

            // 取得當前選中的 SharedEntry
            // 注意：當 SelectionType 為 SharedEntry 時，必須使用 CurrentSelectedSharedEntry
            // 因為 CurrentSelectedEntry 可能保留了之前選中的不同列的 entry
            SharedTableData.SharedTableEntry sharedEntry;
            if (SelectionType == OdinTableSelectionType.SharedEntry)
            {
                sharedEntry = CurrentSelectedSharedEntry;
            }
            else if (CurrentSelectedEntry != null)
            {
                sharedEntry = CurrentSelectedEntry.SharedEntry;
            }
            else
            {
                sharedEntry = CurrentSelectedSharedEntry;
            }

            if (sharedEntry == null)
                return false;

            // 找到當前選中的 table 索引
            var currentTableIndex = FindCurrentTableIndex();

            // 計算下一個位置
            int nextTableIndex;
            var targetSharedEntry = sharedEntry;

            if (currentTableIndex < 0)
            {
                // 沒有選中任何欄位，選第一個或最後一個
                nextTableIndex = reverse ? GUITables.Count - 1 : 0;
            }
            else if (reverse)
            {
                if (currentTableIndex > 0)
                {
                    nextTableIndex = currentTableIndex - 1;
                }
                else
                {
                    // 到達行首，換到上一行的最後一個欄位
                    nextTableIndex = GUITables.Count - 1;
                    targetSharedEntry = GetAdjacentSharedEntry(sharedEntry, true);
                }
            }
            else
            {
                if (currentTableIndex < GUITables.Count - 1)
                {
                    nextTableIndex = currentTableIndex + 1;
                }
                else
                {
                    // 到達行末，換到下一行的第一個欄位
                    nextTableIndex = 0;
                    targetSharedEntry = GetAdjacentSharedEntry(sharedEntry, false);
                }
            }

            if (targetSharedEntry == null)
                return false;

            // 強制結束當前的 TextField 編輯狀態
            EditorGUIUtility.editingTextField = false;
            ClearFocus();

            // 記錄目標位置（用於後續設定焦點）
            // 使用可見行索引而非內部索引
            LastNavigationTargetRowIndex = GetVisibleRowIndex(targetSharedEntry);
            LastNavigationTargetColumnIndex = nextTableIndex;

            // 根據目標欄位類型選擇
            var nextTable = GUITables[nextTableIndex];

            if (nextTable.Type == OdinGUITable<StringTable>.GUITableType.Key)
            {
                // Key 欄位：選擇 SharedEntry
                SelectSharedEntry(targetSharedEntry);
            }
            else
            {
                // 語言欄位：選擇 Entry
                var nextEntry = nextTable.Asset.GetEntry(targetSharedEntry.Id);
                if (nextEntry == null)
                {
                    nextEntry = nextTable.Asset.AddEntry(targetSharedEntry.Id, string.Empty);
                }
                SelectEntry(nextEntry);
            }

            return true;
        }

        private int FindCurrentTableIndex()
        {
            // 根據 SelectionType 判斷當前選中的類型
            if (SelectionType == OdinTableSelectionType.SharedEntry)
            {
                // 選中了 Key 欄位
                for (var i = 0; i < GUITables.Count; i++)
                {
                    if (GUITables[i].Type == OdinGUITable<StringTable>.GUITableType.Key)
                        return i;
                }
            }
            else if (SelectionType == OdinTableSelectionType.TableEntry && CurrentSelectedEntry != null)
            {
                // 選中了語言欄位的 Entry
                for (var i = 0; i < GUITables.Count; i++)
                {
                    if (GUITables[i].Type == OdinGUITable<StringTable>.GUITableType.Key)
                        continue;

                    if (GUITables[i].Asset == CurrentSelectedEntry.Table)
                        return i;
                }
            }

            return -1;
        }

        private SharedTableData.SharedTableEntry GetAdjacentSharedEntry(
            SharedTableData.SharedTableEntry current,
            bool previous)
        {
            if (SharedEntries.Length == 0)
                return null;

            var currentIndex = SharedEntries.GetIndex(current);
            if (currentIndex < 0)
                return null;

            int nextIndex;
            if (previous)
            {
                nextIndex = currentIndex > 0 ? currentIndex - 1 : SharedEntries.Length - 1;
            }
            else
            {
                nextIndex = currentIndex < SharedEntries.Length - 1 ? currentIndex + 1 : 0;
            }

            return SharedEntries[nextIndex];
        }

        /// <summary>
        /// 取得 SharedEntry 在可見列表中的索引（考慮搜索過濾）
        /// </summary>
        private int GetVisibleRowIndex(SharedTableData.SharedTableEntry targetEntry)
        {
            var visibleIndex = 0;
            for (var i = 0; i < SharedEntries.Length; i++)
            {
                var entry = SharedEntries[i];
                if (!SharedEntries.IsVisible(entry))
                    continue;

                if (entry.Id == targetEntry.Id)
                    return visibleIndex;

                visibleIndex++;
            }

            return -1;
        }

        private string GenerateUniqueKey(string originalKey)
        {
            // 檢查是否已經以「空格+數字」結尾
            var baseKey = originalKey;
            var counter = 1;

            var lastSpaceIndex = originalKey.LastIndexOf(' ');
            if (lastSpaceIndex > 0 && lastSpaceIndex < originalKey.Length - 1)
            {
                var suffix = originalKey.Substring(lastSpaceIndex + 1);
                if (int.TryParse(suffix, out var existingNumber))
                {
                    baseKey = originalKey.Substring(0, lastSpaceIndex);
                    counter = existingNumber + 1;
                }
            }

            var newKey = $"{baseKey} {counter}";

            while (Collection.SharedData.Contains(newKey))
            {
                counter++;
                newKey = $"{baseKey} {counter}";
            }

            return newKey;
        }
    }
}
