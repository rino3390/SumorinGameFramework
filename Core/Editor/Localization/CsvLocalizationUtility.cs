using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sumorin.SumorinUtility.Editor;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Sumorin.Localization.Editor
{
	/// <summary>
	/// CSV 本地化匯入匯出工具類
	/// </summary>
	public static class CsvLocalizationUtility
	{
		/// <summary>
		/// 將 StringTableCollection 匯出為 CSV 檔案
		/// </summary>
		/// <param name="collection">要匯出的 StringTableCollection</param>
		/// <param name="filePath">CSV 檔案路徑</param>
		public static void Export(StringTableCollection collection, string filePath)
		{
			if(collection == null) throw new ArgumentNullException(nameof(collection));
			if(string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

			var sb = new StringBuilder();
			var locales = new List<Locale>();
			var tables = new Dictionary<LocaleIdentifier, StringTable>();

			foreach(var table in collection.StringTables)
			{
				var locale = LocalizationEditorSettings.GetLocale(table.LocaleIdentifier);

				if(locale != null)
				{
					locales.Add(locale);
					tables[table.LocaleIdentifier] = table;
				}
			}

			sb.Append("Key");

			foreach(var locale in locales)
			{
				sb.Append(',');
				sb.Append(CsvUtility.Escape(locale.Identifier.Code));
			}

			sb.AppendLine();

			foreach(var entry in collection.SharedData.Entries)
			{
				sb.Append(CsvUtility.Escape(entry.Key));

				foreach(var locale in locales)
				{
					sb.Append(',');

					if(tables.TryGetValue(locale.Identifier, out var table))
					{
						var tableEntry = table.GetEntry(entry.Id);
						var value = tableEntry?.Value ?? string.Empty;
						sb.Append(CsvUtility.Escape(value));
					}
					else
					{
						sb.Append(string.Empty);
					}
				}

				sb.AppendLine();
			}

			File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
			Debug.Log($"Exported {collection.SharedData.Entries.Count} entries to {filePath}");
		}

		/// <summary>
		/// 從 CSV 檔案匯入翻譯資料到 StringTableCollection
		/// </summary>
		/// <param name="collection">目標 StringTableCollection</param>
		/// <param name="filePath">CSV 檔案路徑</param>
		/// <returns>匯入的條目數量</returns>
		public static int Import(StringTableCollection collection, string filePath)
		{
			if(collection == null) throw new ArgumentNullException(nameof(collection));
			if(string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
			if(!File.Exists(filePath)) throw new FileNotFoundException("CSV file not found", filePath);

			var lines = CsvUtility.Read(filePath);

			if(lines.Count < 2)
			{
				Debug.LogWarning("CSV file is empty or has no data rows");
				return 0;
			}

			var header = lines[0];

			if(header.Count < 2 || header[0] != "Key")
			{
				throw new FormatException("Invalid CSV format. First column must be 'Key'");
			}

			var localeColumns = new Dictionary<int, StringTable>();

			for(var i = 1; i < header.Count; i++)
			{
				var localeCode = header[i];
				var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(localeCode));

				if(locale == null)
				{
					Debug.LogWarning($"Locale '{localeCode}' not found, skipping column {i}");
					continue;
				}

				StringTable table = null;

				foreach(var t in collection.StringTables)
				{
					if(t.LocaleIdentifier.Code == localeCode)
					{
						table = t;
						break;
					}
				}

				if(table == null)
				{
					Debug.LogWarning($"StringTable for locale '{localeCode}' not found in collection, skipping");
					continue;
				}

				localeColumns[i] = table;
			}

			Undo.RecordObject(collection.SharedData, "Import CSV Localization");

			foreach(var table in localeColumns.Values)
			{
				Undo.RecordObject(table, "Import CSV Localization");
			}

			var importedCount = 0;

			for(var row = 1; row < lines.Count; row++)
			{
				var fields = lines[row];
				if(fields.Count == 0 || string.IsNullOrWhiteSpace(fields[0])) continue;

				var key = fields[0];
				var sharedEntry = collection.SharedData.GetEntry(key);

				if(sharedEntry == null)
				{
					sharedEntry = collection.SharedData.AddKey(key);
				}

				foreach(var kvp in localeColumns)
				{
					var columnIndex = kvp.Key;
					var table = kvp.Value;

					if(columnIndex < fields.Count)
					{
						var value = fields[columnIndex];
						var tableEntry = table.GetEntry(sharedEntry.Id);

						if(tableEntry == null)
						{
							table.AddEntry(sharedEntry.Id, value);
						}
						else if(tableEntry.Value != value)
						{
							tableEntry.Value = value;
						}
					}
				}

				importedCount++;
			}

			EditorUtility.SetDirty(collection.SharedData);

			foreach(var table in localeColumns.Values)
			{
				EditorUtility.SetDirty(table);
			}

			Debug.Log($"Imported {importedCount} entries from {filePath}");
			return importedCount;
		}
	}
}