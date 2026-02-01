using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Rino.GameFramework.GameManager
{
	/// <summary>
	/// 簡化版 LocalizedString Drawer
	/// 複製 Unity Localization 的繪製邏輯，但隱藏 FallbackState、WaitForCompletion、LocalVariables
	/// </summary>
	public class SimpleLocalizedStringDrawer : OdinValueDrawer<LocalizedString>
	{
		private static class Styles
		{
			public static readonly GUIContent NoTableSelected = new("未選擇字串表");
			public static readonly GUIContent SelectedTable = new("字串表");
			public static readonly GUIContent EntryName = new("字串ID");
			public static readonly GUIContent AddTableCollection = new("新增字串表");
			public static readonly GUIContent AddTableEntry = new("新增字串");
		}

		private const float OpenTableEditorButtonWidth = 30;

		// 快取
		private static GUIContent[] tableLabels;
		private static StringTableCollection[] tableCollections;
		private static Texture tableWindowIcon;

		// 每個 property 的狀態
		private int selectedTableIndex = -1;
		private StringTableCollection selectedCollection;
		private SharedTableData.SharedTableEntry selectedEntry;
		private GUIContent fieldLabel;
		private GUIContent entryNameLabel;

		protected override void Initialize()
		{
			base.Initialize();
			RefreshTableCollections();
			entryNameLabel = new GUIContent(Styles.EntryName);

			// 取得 Table Window Icon
			if (tableWindowIcon == null)
			{
				var editorIconsType = Type.GetType("UnityEditor.Localization.EditorIcons, Unity.Localization.Editor");
				if (editorIconsType != null)
				{
					var prop = editorIconsType.GetProperty("TableWindow", BindingFlags.Public | BindingFlags.Static);
					if (prop != null)
					{
						tableWindowIcon = prop.GetValue(null) as Texture;
					}
				}
			}
		}

		private static void RefreshTableCollections()
		{
			var collections = LocalizationEditorSettings.GetStringTableCollections();
			tableCollections = new StringTableCollection[collections.Count + 1];
			collections.CopyTo(tableCollections, 1);

			tableLabels = new GUIContent[collections.Count + 1];
			tableLabels[0] = Styles.NoTableSelected;
			for (var i = 0; i < collections.Count; i++)
			{
				tableLabels[i + 1] = new GUIContent(collections[i].TableCollectionName);
			}
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var odinProperty = this.Property;
			var unityProperty = odinProperty.Tree.GetUnityPropertyForPath(odinProperty.Path, out _);

			if (unityProperty == null)
			{
				this.CallNextDrawer(label);
				return;
			}

			// 初始化狀態
			InitializeState(unityProperty);

			// 繪製 Foldout 和下拉選擇器
			var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
			var foldoutRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
			var dropdownRect = new Rect(foldoutRect.xMax, rect.y, rect.width - EditorGUIUtility.labelWidth, rect.height);

			EditorGUI.BeginProperty(foldoutRect, label, unityProperty);
			unityProperty.isExpanded = EditorGUI.Foldout(foldoutRect, unityProperty.isExpanded, label, true);

			// 下拉選擇器按鈕
			if (EditorGUI.DropdownButton(dropdownRect, GetFieldLabel(), FocusType.Passive))
			{
				ShowPicker(unityProperty);
			}

			EditorGUI.EndProperty();

			// 展開時繪製詳細內容
			if (unityProperty.isExpanded)
			{
				EditorGUI.indentLevel++;
				DrawTableDetails(unityProperty);
				EditorGUI.indentLevel--;
			}
		}

		private void InitializeState(SerializedProperty property)
		{
			var tableRefProp = property.FindPropertyRelative("m_TableReference");
			var entryRefProp = property.FindPropertyRelative("m_TableEntryReference");

			if (tableRefProp == null || entryRefProp == null)
				return;

			var tableNameProp = tableRefProp.FindPropertyRelative("m_TableCollectionName");
			var entryKeyProp = entryRefProp.FindPropertyRelative("m_Key");

			if (tableNameProp == null)
				return;

			var tableName = tableNameProp.stringValue;

			// 找到對應的 Table Collection
			if (selectedCollection == null || selectedTableIndex < 0)
			{
				selectedCollection = null;
				selectedTableIndex = 0;

				if (!string.IsNullOrEmpty(tableName))
				{
					for (var i = 1; i < tableCollections.Length; i++)
					{
						if (tableCollections[i] != null && tableCollections[i].TableCollectionName == tableName)
						{
							selectedCollection = tableCollections[i];
							selectedTableIndex = i;
							break;
						}
					}
				}
			}

			// 找到對應的 Entry
			if (selectedEntry == null && selectedCollection != null && entryKeyProp != null)
			{
				var entryKey = entryKeyProp.stringValue;
				if (!string.IsNullOrEmpty(entryKey))
				{
					selectedEntry = selectedCollection.SharedData.GetEntry(entryKey);
				}
			}
		}

		private GUIContent GetFieldLabel()
		{
			if (fieldLabel != null)
				return fieldLabel;

			var icon = EditorGUIUtility.ObjectContent(null, typeof(string));
			if (selectedCollection != null && selectedEntry != null)
			{
				var key = selectedEntry.Key;
				var eol = key.IndexOf('\n');
				if (eol > 0)
					key = key.Substring(0, eol);

				fieldLabel = new GUIContent($"{selectedCollection.TableCollectionName}/{key}", icon.image);
			}
			else
			{
				fieldLabel = new GUIContent("無字串", icon.image);
			}

			return fieldLabel;
		}

		private void ShowPicker(SerializedProperty property)
		{
			var selector = new LocalizedStringEntrySelector();
			selector.EnableSingleClickToSelect();
			selector.SelectionConfirmed += selection =>
			{
				var selected = selection.FirstOrDefault();
				if (selected.Collection == null || selected.SharedEntry == null)
					return;

				selectedCollection = selected.Collection;
				selectedEntry = selected.SharedEntry;
				fieldLabel = null;

				// 更新 SerializedProperty
				var tableRefProp = property.FindPropertyRelative("m_TableReference");
				var entryRefProp = property.FindPropertyRelative("m_TableEntryReference");

				if (tableRefProp != null)
				{
					var tableNameProp = tableRefProp.FindPropertyRelative("m_TableCollectionName");
					if (tableNameProp != null)
					{
						tableNameProp.stringValue = selectedCollection.TableCollectionName;
					}
				}

				if (entryRefProp != null)
				{
					var entryKeyProp = entryRefProp.FindPropertyRelative("m_Key");
					if (entryKeyProp != null)
					{
						entryKeyProp.stringValue = selectedEntry.Key;
					}
				}

				property.serializedObject.ApplyModifiedProperties();

				// 更新選擇的 index
				for (var i = 1; i < tableCollections.Length; i++)
				{
					if (tableCollections[i] == selectedCollection)
					{
						selectedTableIndex = i;
						break;
					}
				}
			};

			selector.ShowInPopup();
		}

		private void DrawTableDetails(SerializedProperty property)
		{
			// Table Collection 選擇
			var tableRect = EditorGUILayout.GetControlRect();
			var tableSelectionRect = selectedTableIndex != 0
				? new Rect(tableRect.x, tableRect.y, tableRect.width - OpenTableEditorButtonWidth, tableRect.height)
				: tableRect;

			EditorGUI.BeginChangeCheck();
			var newSelectedIndex = EditorGUI.Popup(tableSelectionRect, Styles.SelectedTable, selectedTableIndex, tableLabels);
			if (EditorGUI.EndChangeCheck())
			{
				selectedTableIndex = newSelectedIndex;
				selectedCollection = tableCollections[newSelectedIndex];
				selectedEntry = null;
				fieldLabel = null;

				// 更新 property
				var tableRefProp = property.FindPropertyRelative("m_TableReference");
				if (tableRefProp != null)
				{
					var tableNameProp = tableRefProp.FindPropertyRelative("m_TableCollectionName");
					if (tableNameProp != null)
					{
						tableNameProp.stringValue = selectedCollection?.TableCollectionName ?? "";
					}
				}

				// 清除 entry
				var entryRefProp = property.FindPropertyRelative("m_TableEntryReference");
				if (entryRefProp != null)
				{
					var entryKeyProp = entryRefProp.FindPropertyRelative("m_Key");
					if (entryKeyProp != null)
					{
						entryKeyProp.stringValue = "";
					}
				}

				property.serializedObject.ApplyModifiedProperties();
			}

			// 開啟 Table Editor 按鈕
			if (selectedTableIndex != 0 && tableWindowIcon != null)
			{
				var openButtonRect = new Rect(tableSelectionRect.xMax, tableRect.y, OpenTableEditorButtonWidth,
					tableRect.height);
				if (GUI.Button(openButtonRect, tableWindowIcon))
				{
					// 使用反射開啟 LocalizationTablesWindow
					var windowType =
						Type.GetType("UnityEditor.Localization.UI.LocalizationTablesWindow, Unity.Localization.Editor");
					if (windowType != null)
					{
						var showMethod = windowType.GetMethod("ShowWindow",
							BindingFlags.Public | BindingFlags.Static,
							null,
							new[] { typeof(TableReference), typeof(TableEntryReference) },
							null);

						if (showMethod != null && selectedCollection != null)
						{
							TableReference tableRef = selectedCollection.TableCollectionName;
							TableEntryReference entryRef = selectedEntry != null
								? (TableEntryReference)selectedEntry.Key
								: default;
							showMethod.Invoke(null, new object[] { tableRef, entryRef });
						}
					}
				}
			}

			// Create Table Collection / Add Table Entry 按鈕
			var buttonRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
			if (selectedTableIndex == 0)
			{
				if (GUI.Button(buttonRect, Styles.AddTableCollection, EditorStyles.miniButton))
				{
					var windowType =
						Type.GetType("UnityEditor.Localization.UI.TableCreatorWindow, Unity.Localization.Editor");
					if (windowType != null)
					{
						var showMethod = windowType.GetMethod("ShowWindow", BindingFlags.Public | BindingFlags.Static);
						showMethod?.Invoke(null, null);
					}
				}
			}
			else
			{
				if (GUI.Button(buttonRect, Styles.AddTableEntry, EditorStyles.miniButton))
				{
					if (selectedCollection != null)
					{
						var keys = selectedCollection.SharedData;
						Undo.RecordObject(keys, "Add entry.");
						var entry = keys.AddKey();
						EditorUtility.SetDirty(keys);

						selectedEntry = entry;
						fieldLabel = null;

						// 更新 property
						var entryRefProp = property.FindPropertyRelative("m_TableEntryReference");
						if (entryRefProp != null)
						{
							var entryKeyProp = entryRefProp.FindPropertyRelative("m_Key");
							if (entryKeyProp != null)
							{
								entryKeyProp.stringValue = entry.Key;
							}
						}

						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}

			// 不繪製 Enable Fallback 和 Wait For Completion

			// Entry Name
			if (selectedEntry != null)
			{
				DrawEntryName(property);
				DrawLocalePreview();
			}
		}

		private void DrawLocalePreview()
		{
			if (selectedCollection == null || selectedEntry == null)
				return;

			var projectLocales = LocalizationEditorSettings.GetLocales();
			if (projectLocales == null || projectLocales.Count == 0)
				return;

			const float smartIconWidth = 18f;

			// 繪製每個語言
			foreach (var locale in projectLocales)
			{
				var table = selectedCollection.GetTable(locale.Identifier) as StringTable;
				var label = new GUIContent(locale.Identifier.ToString());

				// 沒有 Table 的情況
				if (table == null)
				{
					var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
					var buttonRect = EditorGUI.PrefixLabel(rect, label);
					if (GUI.Button(buttonRect, "建立表格", EditorStyles.miniButton))
					{
						selectedCollection.AddNewTable(locale.Identifier);
						GUIUtility.ExitGUI();
					}
					continue;
				}

				// 取得當前值
				var tableEntry = table.GetEntry(selectedEntry.Id);
				var currentValue = tableEntry?.Value ?? "";
				var isSmart = tableEntry?.IsSmart ?? false;

				// 繪製 Label + TextField + Smart Icon
				var rowRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
				var labelRect = new Rect(rowRect.x, rowRect.y, EditorGUIUtility.labelWidth, rowRect.height);
				var smartRect = new Rect(rowRect.xMax - smartIconWidth, rowRect.y, smartIconWidth, rowRect.height);
				var fieldRect = new Rect(labelRect.xMax, rowRect.y, rowRect.width - labelRect.width - smartIconWidth - 2, rowRect.height);

				// 語言標籤
				EditorGUI.LabelField(labelRect, label);

				// 繪製輸入框背景（圓角矩形）
				var bgColor = OdinLocalizationGUI.RowEvenBackground2;
				var borderColor = Event.current.IsMouseOver(fieldRect)
					? OdinLocalizationGUI.RowBorderHover
					: OdinLocalizationGUI.RowBorder;
				GUI.DrawTexture(fieldRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, bgColor, 0, 2.5f);
				GUI.DrawTexture(fieldRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, borderColor, 1, 2.5f);

				// 取得 control id
				var controlId = GUIUtility.GetControlID(FocusType.Keyboard, fieldRect);

				// 值輸入框
				string newValue;
				bool changed;

				if (isSmart && !string.IsNullOrEmpty(currentValue))
				{
					// Smart String 模式：使用 Odin 的語法高亮 TextField
					var highlightedText = OdinLocalizationSyntaxHighlighter.HighlightAsRichText(currentValue);
					newValue = OdinLocalizationGUI.TextFieldSyntaxHighlighted(fieldRect, currentValue, highlightedText, out changed, controlId);
				}
				else
				{
					// 普通模式：使用 Odin 的 TextField
					newValue = OdinLocalizationGUI.TextField(fieldRect, currentValue, out changed, controlId);
				}

				if (changed)
				{
					Undo.RecordObject(table, "Edit localized value");
					if (tableEntry == null)
					{
						tableEntry = table.AddEntry(selectedEntry.Id, newValue);
					}
					else
					{
						tableEntry.Value = newValue;
					}
					EditorUtility.SetDirty(table);
				}

				// Smart String 燈泡圖示 - 使用 Odin 的 SdfIcons
				var smartIconRect = smartRect.AlignMiddle(16);
				var isMouseOver = Event.current.IsMouseOver(smartIconRect);
				var iconColor = new Color(1, 1, 1, isMouseOver ? 0.8f : 0.3f);

				SdfIcons.DrawIcon(smartIconRect, isSmart ? SdfIconType.LightbulbFill : SdfIconType.Lightbulb, iconColor);

				// 點擊切換 Smart String
				if (Event.current.OnMouseDown(smartIconRect, 0))
				{
					Undo.RecordObject(table, "Toggle Smart String");
					if (tableEntry == null)
					{
						tableEntry = table.AddEntry(selectedEntry.Id, "");
					}
					tableEntry.IsSmart = !tableEntry.IsSmart;
					EditorUtility.SetDirty(table);
				}

				// Tooltip
				GUI.Label(smartIconRect, GUIHelper.TempContent(string.Empty, "切換智慧字串"));
			}
		}

		private void DrawEntryName(SerializedProperty property)
		{
			EditorGUI.BeginChangeCheck();

			var currentKey = selectedEntry?.Key ?? "";
			var newKey = EditorGUILayout.TextField(entryNameLabel, currentKey);

			if (EditorGUI.EndChangeCheck() && selectedCollection != null && selectedEntry != null)
			{
				var sharedData = selectedCollection.SharedData;
				var existingEntry = sharedData.GetEntry(newKey);

				if (string.IsNullOrEmpty(newKey))
				{
					entryNameLabel = new GUIContent(Styles.EntryName.text, EditorGUIUtility.IconContent("console.warnicon").image, "ID不能為空");
				}
				else if (existingEntry == null || selectedEntry == existingEntry)
				{
					Undo.RecordObject(sharedData, "Rename key entry");
					sharedData.RenameKey(selectedEntry.Key, newKey);
					EditorUtility.SetDirty(sharedData);
					entryNameLabel = new GUIContent(Styles.EntryName);

					// 更新 property
					var entryRefProp = property.FindPropertyRelative("m_TableEntryReference");
					if (entryRefProp != null)
					{
						var entryKeyProp = entryRefProp.FindPropertyRelative("m_Key");
						if (entryKeyProp != null)
						{
							entryKeyProp.stringValue = newKey;
						}
					}

					property.serializedObject.ApplyModifiedProperties();
					fieldLabel = null;
				}
				else
				{
					entryNameLabel = new GUIContent(Styles.EntryName.text, EditorGUIUtility.IconContent("console.warnicon").image,
						$"無法將鍵值重新命名為 '{newKey}'，該ID已被使用。");
				}
			}
		}
	}

	/// <summary>
	/// LocalizedString Entry 選擇器（Odin 風格，帶搜尋框和樹狀結構）
	/// </summary>
	public class LocalizedStringEntrySelector : OdinSelector<LocalizedStringEntrySelector.EntrySelection>
	{
		public struct EntrySelection
		{
			public StringTableCollection Collection;
			public SharedTableData.SharedTableEntry SharedEntry;
		}

		/// <summary>
		/// Entry 資料（用於搜尋和重建樹）
		/// </summary>
		private struct EntryData
		{
			public StringTableCollection Collection;
			public SharedTableData.SharedTableEntry Entry;
			public string SearchString;
		}

		// 快取語言列表
		private List<Locale> projectLocales;

		// 搜尋相關
		private string searchTerm = "";
		private Dictionary<StringTableCollection, List<EntryData>> collectionEntries = new();

		protected override void DrawSelectionTree()
		{
			// 繪製自訂搜尋框（不使用 toolbar 樣式以避免分割線）
			GUILayout.BeginHorizontal();
			GUILayout.Space(4);
			GUI.SetNextControlName("LocalizedStringSearchField");
			var newSearchTerm = SirenixEditorGUI.ToolbarSearchField(searchTerm);
			GUILayout.Space(4);
			GUILayout.EndHorizontal();

			// 搜尋框與樹之間的間距
			GUILayout.Space(2);

			// 搜尋詞變更時重建樹
			if (newSearchTerm != searchTerm)
			{
				searchTerm = newSearchTerm;
				RebuildTreeWithFilter();
			}

			// 繪製樹狀結構
			base.DrawSelectionTree();
		}

		private void RebuildTreeWithFilter()
		{
			var tree = this.SelectionTree;
			var hasSearch = !string.IsNullOrEmpty(searchTerm);

			// 清除現有項目
			tree.MenuItems.Clear();

			// 重建樹
			foreach (var kvp in collectionEntries)
			{
				var collection = kvp.Key;
				var entries = kvp.Value;

				// 找出匹配的 entries
				var matchingEntries = hasSearch
					? entries.Where(e => e.SearchString.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList()
					: entries;

				// 如果沒有匹配的 entry，跳過這個 collection
				if (!matchingEntries.Any())
					continue;

				// 建立 collection 節點
				var collectionItem = CreateCollectionItem(tree, collection.TableCollectionName);

				// 建立匹配的 entry 節點
				foreach (var entryData in matchingEntries)
				{
					var entryItem = CreateEntryItem(tree, entryData);
					collectionItem.ChildMenuItems.Add(entryItem);
				}

				tree.MenuItems.Add(collectionItem);

				// 搜尋時自動展開，無搜尋時保持展開
				collectionItem.Toggled = true;
			}
		}

		private OdinMenuItem CreateCollectionItem(OdinMenuTree tree, string tableName)
		{
			var collectionItem = new OdinMenuItem(tree, tableName, default(EntrySelection))
			{
				SdfIcon = SdfIconType.Table
			};

			// 點擊整列時 toggle 展開/收合
			collectionItem.OnDrawItem += item =>
			{
				if (Event.current.type == EventType.MouseDown && item.Rect.Contains(Event.current.mousePosition))
				{
					item.Toggled = !item.Toggled;
					item.Deselect();
					Event.current.Use();
				}
			};

			return collectionItem;
		}

		private OdinMenuItem CreateEntryItem(OdinMenuTree tree, EntryData entryData)
		{
			var selection = new EntrySelection
			{
				Collection = entryData.Collection,
				SharedEntry = entryData.Entry
			};

			var entryItem = new OdinMenuItem(tree, entryData.Entry.Key, selection)
			{
				SdfIcon = SdfIconType.KeyFill
			};

			// 自定義繪製：在右側顯示翻譯值 + 單擊選擇
			var capturedCollection = entryData.Collection;
			var capturedEntry = entryData.Entry;
			entryItem.OnDrawItem += item =>
			{
				DrawEntryValues(item, capturedCollection, capturedEntry);

				// 單擊選擇並確認
				if (Event.current.type == EventType.MouseDown && item.Rect.Contains(Event.current.mousePosition))
				{
					item.Select();
					item.MenuTree.Selection.ConfirmSelection();
					Event.current.Use();
				}
			};

			return entryItem;
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			// 不使用內建搜尋工具列，改用自訂搜尋
			tree.Config.DrawSearchToolbar = false;
			tree.Config.SelectMenuItemsOnMouseDown = true;
			tree.Selection.SupportsMultiSelect = false;
			tree.DefaultMenuStyle.AlignTriangleLeft = true;
			tree.DefaultMenuStyle.TrianglePadding = 4f;
			tree.DefaultMenuStyle.IconOffset = 6f;
			tree.DefaultMenuStyle.IconPadding = 10f;

			projectLocales = LocalizationEditorSettings.GetLocales()?.ToList() ?? new List<Locale>();
			var collections = LocalizationEditorSettings.GetStringTableCollections();

			// 建立資料快取
			collectionEntries.Clear();
			foreach (var collection in collections)
			{
				if (collection == null || collection.SharedData == null)
					continue;

				if (!collection.SharedData.Entries.Any())
					continue;

				var entryList = new List<EntryData>();
				foreach (var entry in collection.SharedData.Entries)
				{
					var searchValues = GetSearchableValues(collection, entry);
					entryList.Add(new EntryData
					{
						Collection = collection,
						Entry = entry,
						SearchString = $"{entry.Key} {searchValues}"
					});
				}

				collectionEntries[collection] = entryList;
			}

			// 初始建立樹
			foreach (var kvp in collectionEntries)
			{
				var collection = kvp.Key;
				var entries = kvp.Value;

				var collectionItem = CreateCollectionItem(tree, collection.TableCollectionName);

				foreach (var entryData in entries)
				{
					var entryItem = CreateEntryItem(tree, entryData);
					collectionItem.ChildMenuItems.Add(entryItem);
				}

				tree.MenuItems.Add(collectionItem);
				collectionItem.Toggled = true;
			}
		}

		private string GetSearchableValues(StringTableCollection collection, SharedTableData.SharedTableEntry entry)
		{
			if (projectLocales == null || projectLocales.Count == 0)
				return "";

			var values = new List<string>();
			foreach (var locale in projectLocales)
			{
				var table = collection.GetTable(locale.Identifier) as StringTable;
				if (table != null)
				{
					var tableEntry = table.GetEntry(entry.Id);
					var value = tableEntry?.Value ?? "";
					if (!string.IsNullOrEmpty(value))
						values.Add(value);
				}
			}

			return string.Join(" ", values);
		}

		private void DrawEntryValues(OdinMenuItem item, StringTableCollection collection, SharedTableData.SharedTableEntry entry)
		{
			if (projectLocales == null || projectLocales.Count == 0)
				return;

			// 計算繪製區域（右側）
			var rect = item.Rect;
			var valueWidth = rect.width * 0.5f;
			var valueRect = new Rect(rect.xMax - valueWidth - 4, rect.y, valueWidth, rect.height);

			// 取得翻譯值
			var valuesToShow = new List<string>();
			var maxLocales = Mathf.Min(2, projectLocales.Count);
			for (var i = 0; i < maxLocales; i++)
			{
				var table = collection.GetTable(projectLocales[i].Identifier) as StringTable;
				if (table != null)
				{
					var tableEntry = table.GetEntry(entry.Id);
					var value = tableEntry?.Value ?? "";
					if (value.Length > 20)
						value = value.Substring(0, 17) + "...";
					valuesToShow.Add(value);
				}
			}

			if (valuesToShow.Count == 0)
				return;

			// 繪製翻譯值（使用 table 樣式）
			var cellWidth = valueWidth / valuesToShow.Count;
			for (var i = 0; i < valuesToShow.Count; i++)
			{
				var cellRect = new Rect(valueRect.x + i * cellWidth, valueRect.y + 2, cellWidth - 2, valueRect.height - 4);

				// 繪製背景
				var bgColor = OdinLocalizationGUI.RowEvenBackground2;
				GUI.DrawTexture(cellRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 1, bgColor, 0, 2f);

				// 繪製文字
				var labelStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					alignment = TextAnchor.MiddleLeft,
					padding = new RectOffset(4, 4, 0, 0),
					clipping = TextClipping.Clip
				};
				GUI.Label(cellRect, new GUIContent(valuesToShow[i]), labelStyle);
			}
		}
	}
}
