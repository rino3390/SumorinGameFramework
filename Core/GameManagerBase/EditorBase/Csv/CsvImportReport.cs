using System.Collections.Generic;
using System.Linq;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// CSV 匯入結果，以檔案分組記錄筆數與逐條問題
	/// </summary>
	public class CsvImportReport
	{
		/// <summary>
		/// 各檔案的結果，順序同處理順序
		/// </summary>
		public List<FileReport> Files { get; } = new();

		/// <summary>
		/// 新增一個檔案的結果
		/// </summary>
		/// <param name="fileName">檔名</param>
		/// <returns>該檔案的結果，供後續累計</returns>
		public FileReport Add(string fileName)
		{
			var file = new FileReport { FileName = fileName };
			Files.Add(file);

			return file;
		}

		/// <summary>
		/// 彈窗顯示的文字，每檔一段，先寫檔名與筆數，再逐條列問題
		/// </summary>
		/// <returns>顯示文字</returns>
		public override string ToString()
		{
			return Files.Count == 0 ? "沒有可匯入的檔案" : string.Join("\n\n", Files.Select(file => file.ToString()));
		}

		/// <summary>
		/// 單一檔案的結果
		/// </summary>
		public class FileReport
		{
			/// <summary>
			/// 檔名
			/// </summary>
			public string FileName;

			/// <summary>
			/// 成功寫入的列數
			/// </summary>
			public int Success;

			/// <summary>
			/// 跳過的列數
			/// </summary>
			public int Skipped;

			/// <summary>
			/// 整檔略過的原因，null 表示有逐列處理
			/// </summary>
			public string WholeFileProblem;

			/// <summary>
			/// 逐條問題，含列號、欄位、原因與處理方式
			/// </summary>
			public List<string> Problems = new();

			/// <summary>
			/// 這個檔案的顯示文字
			/// </summary>
			/// <returns>檔名與筆數一行，問題各一行縮排</returns>
			public override string ToString()
			{
				if(WholeFileProblem != null) return $"{FileName}：{WholeFileProblem}";

				var lines = new List<string> { $"{FileName}：成功 {Success} 筆、跳過 {Skipped} 筆" };
				lines.AddRange(Problems.Select(problem => "  " + problem));

				return string.Join("\n", lines);
			}
		}
	}
}