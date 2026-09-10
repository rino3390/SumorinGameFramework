using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sumorin.SumorinUtility.Editor
{
	/// <summary>
	/// CSV 文字的跳脫、解析與檔案讀寫
	/// </summary>
	/// <remarks>
	/// 檔案一律 UTF-8 含 BOM，Excel 直接開啟時中文才不會變亂碼。
	/// </remarks>
	public static class CsvUtility
	{
		/// <summary>
		/// 把單一儲存格內容轉成 CSV 欄位，含逗號、引號或換行時加引號並把引號寫成兩個
		/// </summary>
		/// <param name="field">儲存格內容</param>
		/// <returns>可直接寫進 CSV 的欄位文字</returns>
		public static string Escape(string field)
		{
			if(string.IsNullOrEmpty(field)) return string.Empty;

			var needsQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");

			return needsQuoting ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
		}

		/// <summary>
		/// 把多列內容組成 CSV 文字
		/// </summary>
		/// <param name="rows">每列的儲存格內容</param>
		/// <returns>CSV 文字</returns>
		public static string ToCsv(IEnumerable<IEnumerable<string>> rows)
		{
			var sb = new StringBuilder();

			foreach(var row in rows)
			{
				sb.AppendLine(string.Join(",", row.Select(Escape)));
			}

			return sb.ToString();
		}

		/// <summary>
		/// 解析 CSV 文字，支援引號內的逗號、換行與兩個引號的跳脫，CRLF 與 LF 都當列尾
		/// </summary>
		/// <param name="content">CSV 文字</param>
		/// <returns>每列的儲存格內容</returns>
		public static List<List<string>> Parse(string content)
		{
			var result = new List<List<string>>();
			var currentField = new StringBuilder();
			var currentRow = new List<string>();
			var inQuotes = false;
			var i = 0;

			while(i < content.Length)
			{
				var c = content[i];

				if(inQuotes)
				{
					if(c == '"')
					{
						if(i + 1 < content.Length && content[i + 1] == '"')
						{
							currentField.Append('"');
							i += 2;
							continue;
						}

						inQuotes = false;
						i++;
						continue;
					}

					currentField.Append(c);
					i++;
				}
				else
				{
					switch(c)
					{
						case '"':
							inQuotes = true;
							i++;
							break;
						case ',':
							currentRow.Add(currentField.ToString());
							currentField.Clear();
							i++;
							break;
						case '\r':
							if(i + 1 < content.Length && content[i + 1] == '\n')
							{
								i++;
							}

							currentRow.Add(currentField.ToString());
							currentField.Clear();
							result.Add(currentRow);
							currentRow = new List<string>();
							i++;
							break;
						case '\n':
							currentRow.Add(currentField.ToString());
							currentField.Clear();
							result.Add(currentRow);
							currentRow = new List<string>();
							i++;
							break;
						default:
							currentField.Append(c);
							i++;
							break;
					}
				}
			}

			if(currentField.Length > 0 || currentRow.Count > 0)
			{
				currentRow.Add(currentField.ToString());
				result.Add(currentRow);
			}

			return result;
		}

		/// <summary>
		/// 把多列內容寫成 UTF-8 含 BOM 的 CSV 檔
		/// </summary>
		/// <param name="filePath">檔案路徑</param>
		/// <param name="rows">每列的儲存格內容</param>
		public static void Write(string filePath, IEnumerable<IEnumerable<string>> rows)
		{
			File.WriteAllText(filePath, ToCsv(rows), Encoding.UTF8);
		}

		/// <summary>
		/// 讀取 CSV 檔並解析，BOM 由讀檔自動去除
		/// </summary>
		/// <param name="filePath">檔案路徑</param>
		/// <returns>每列的儲存格內容</returns>
		public static List<List<string>> Read(string filePath)
		{
			return Parse(File.ReadAllText(filePath, Encoding.UTF8));
		}
	}
}