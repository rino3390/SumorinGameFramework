using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using Object = UnityEngine.Object;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 資料成員值與 CSV 儲存格文字的雙向轉換
	/// </summary>
	/// <remarks>
	/// 基本型別寫純文字、列舉寫名稱、多語系寫「表名/Key」、Unity 資產寫資產路徑，其餘型別寫 JSON。
	/// 數字一律用不變文化，Excel 的地區設定不會影響小數點。
	/// </remarks>
	public static class CsvCellConverter
	{
		// ponytail: 巢狀型別只走 Newtonsoft 預設規則（公開欄位與公開屬性），私有 setter 的屬性要再加 ContractResolver
		private static readonly JsonSerializerSettings JsonSettings = new()
		{
			Converters = { new StringEnumConverter() },
			ReferenceLoopHandling = ReferenceLoopHandling.Ignore
		};

		/// <summary>
		/// 把成員值轉成儲存格文字，null 一律寫空格
		/// </summary>
		/// <param name="value">成員值</param>
		/// <returns>儲存格文字</returns>
		public static string ToCell(object value)
		{
			if(value == null) return "";
			if(value is string text) return text;
			if(value is LocalizedString localized) return LocalizedToCell(localized);
			if(value is Object asset) return asset == null ? "" : AssetDatabase.GetAssetPath(asset);
			if(value is bool flag) return flag ? "true" : "false";
			if(value is Enum) return value.ToString();
			if(value is float single) return single.ToString("R", CultureInfo.InvariantCulture);
			if(value is double real) return real.ToString("R", CultureInfo.InvariantCulture);
			if(value.GetType().IsPrimitive || value is decimal) return Convert.ToString(value, CultureInfo.InvariantCulture);

			return JsonConvert.SerializeObject(value, JsonSettings);
		}

		/// <summary>
		/// 把儲存格文字轉成成員值，回傳值一律可直接寫入成員
		/// </summary>
		/// <param name="cell">儲存格文字</param>
		/// <param name="type">成員型別</param>
		/// <param name="problem">解析失敗或參照找不到時的說明與處理方式，正常時為 null</param>
		/// <returns>成員值，解析失敗時為型別預設值</returns>
		public static object Parse(string cell, Type type, out string problem)
		{
			problem = null;

			if(type == typeof(string)) return cell ?? "";
			if(string.IsNullOrEmpty(cell)) return Default(type);
			if(typeof(LocalizedString).IsAssignableFrom(type)) return ParseLocalized(cell, out problem);

			if(typeof(Object).IsAssignableFrom(type))
			{
				// ponytail: 多切片圖集只會載入第一張 Sprite，要指定切片時再擴充成「路徑#名稱」
				var asset = AssetDatabase.LoadAssetAtPath(cell, type);

				if(asset == null)
				{
					problem = $"找不到資產 {cell}，已設為空";
				}

				return asset;
			}

			if(type.IsEnum)
			{
				try
				{
					return Enum.Parse(type, cell, true);
				}
				catch(Exception e) when(e is ArgumentException or OverflowException)
				{
					problem = $"{cell} 不是 {type.Name} 的選項，已留空";
					return Default(type);
				}
			}

			if(type == typeof(bool))
			{
				if(bool.TryParse(cell, out var flag)) return flag;

				problem = $"{cell} 不是布林，已留空";
				return false;
			}

			if(type.IsPrimitive || type == typeof(decimal))
			{
				try
				{
					return Convert.ChangeType(cell, type, CultureInfo.InvariantCulture);
				}
				catch(Exception e) when(e is FormatException or OverflowException)
				{
					problem = $"{cell} 不是{(IsFloating(type) ? "數值" : "整數")}，已留空";
					return Default(type);
				}
			}

			try
			{
				return JsonConvert.DeserializeObject(cell, type, JsonSettings);
			}
			catch(JsonException)
			{
				problem = $"{cell} 不是有效的 JSON，已留空";
				return Default(type);
			}
		}

		private static string LocalizedToCell(LocalizedString localized)
		{
			if(localized.IsEmpty) return "";

			var collection = LocalizationEditorSettings.GetStringTableCollection(localized.TableReference);
			var table = collection != null ? collection.TableCollectionName : localized.TableReference.TableCollectionName;
			var key = collection?.SharedData.GetEntryFromReference(localized.TableEntryReference)?.Key ?? localized.TableEntryReference.Key;

			return $"{table}/{key}";
		}

		private static object ParseLocalized(string cell, out string problem)
		{
			problem = null;
			var slash = cell.IndexOf('/');

			if(slash <= 0 || slash == cell.Length - 1)
			{
				problem = $"{cell} 不是「表名/Key」格式，已留空";
				return null;
			}

			var table = cell[..slash];
			var key = cell[(slash + 1)..];
			var entry = LocalizationEditorSettings.GetStringTableCollection(table)?.SharedData.GetEntry(key);

			// 找得到就以 Id 參照，改 Key 名稱不會失效；找不到照設 Key 參照，翻譯常晚於資料填寫
			if(entry != null) return new LocalizedString(table, entry.Id);

			problem = $"字串表 {table} 或 Key {key} 不存在，已照設參照";
			return new LocalizedString(table, key);
		}

		private static bool IsFloating(Type type)
		{
			return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
		}

		private static object Default(Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}
	}
}