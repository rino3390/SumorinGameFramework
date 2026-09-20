using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Sumorin.SumorinUtility;
using Sumorin.SumorinUtility.Editor;
using UnityEditor;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 全部資料型別的 CSV 匯出與匯入，一個型別一份 CSV，檔名是型別名稱
	/// </summary>
	/// <remarks>
	/// 欄位取 Odin 在 Unity 政策下會序列化的成員，與編輯器顯示的欄位一致，前兩欄固定是 Id 與 AssetName。
	/// 匯入以 Id 比對集合內的資產，只補寫不刪除，逐檔逐列容錯，問題記進 <see cref="CsvImportReport" />。
	/// </remarks>
	public static class DataCsv
	{
		/// <summary>
		/// 檔名對不上任何資料型別時的說明文字
		/// </summary>
		public const string UnknownTypeLabel = "找不到對應的資料型別";

		private static readonly MethodInfo ExportMethod = typeof(DataCsv).GetMethod(nameof(ExportType), BindingFlags.NonPublic | BindingFlags.Static);
		private static readonly MethodInfo ImportMethod = typeof(DataCsv).GetMethod(nameof(ImportFile), BindingFlags.NonPublic | BindingFlags.Static);

		/// <summary>
		/// 把所有掛 <see cref="DataEditorConfigAttribute" /> 的型別各寫一份 CSV 到資料夾
		/// </summary>
		/// <param name="folder">目標資料夾</param>
		/// <returns>寫出的檔案數</returns>
		public static int Export(string folder)
		{
			return Export(folder, DataTypes());
		}

		/// <summary>
		/// 把指定型別各寫一份 CSV 到資料夾，沒有資產的型別只寫標題列
		/// </summary>
		/// <param name="folder">目標資料夾</param>
		/// <param name="dataTypes">要匯出的資料型別</param>
		/// <returns>寫出的檔案數</returns>
		public static int Export(string folder, IEnumerable<Type> dataTypes)
		{
			var count = 0;

			foreach(var type in dataTypes)
			{
				ExportMethod.MakeGenericMethod(type).Invoke(null, new object[] { folder });
				count++;
			}

			return count;
		}

		/// <summary>
		/// 列出資料夾內的 CSV 檔，依檔名排序
		/// </summary>
		/// <param name="folder">來源資料夾</param>
		/// <returns>CSV 檔的完整路徑</returns>
		public static List<string> FindCsvFiles(string folder)
		{
			// Windows 的 *.csv 也會撈到 .csvx 之類的副檔名，再比對一次
			return Directory.GetFiles(folder, "*.csv")
							.Where(path => string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
							.OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
							.ToList();
		}

		/// <summary>
		/// 以檔名找出對應的資料型別
		/// </summary>
		/// <param name="filePath">CSV 檔路徑</param>
		/// <param name="dataTypes">可對應的資料型別</param>
		/// <returns>型別名稱與檔名相同的型別，對不上時為 null</returns>
		public static Type FindType(string filePath, IEnumerable<Type> dataTypes)
		{
			var typeName = Path.GetFileNameWithoutExtension(filePath);

			return dataTypes.FirstOrDefault(type => type.Name == typeName);
		}

		/// <summary>
		/// 匯入指定的 CSV 檔，以檔名對應指定型別寫回
		/// </summary>
		/// <param name="filePaths">要匯入的 CSV 檔路徑，依此順序處理</param>
		/// <param name="dataTypes">可對應的資料型別</param>
		/// <returns>匯入結果</returns>
		public static CsvImportReport Import(IEnumerable<string> filePaths, IEnumerable<Type> dataTypes)
		{
			var types = dataTypes.ToList();
			var report = new CsvImportReport();

			foreach(var path in filePaths)
			{
				var file = report.Add(Path.GetFileName(path));
				var type = FindType(path, types);

				if(type == null)
				{
					file.WholeFileProblem = $"{UnknownTypeLabel}，已略過";
					continue;
				}

				try
				{
					ImportMethod.MakeGenericMethod(type).Invoke(null, new object[] { path, file });
				}
				catch(TargetInvocationException e)
				{
					file.WholeFileProblem = $"匯入失敗（{e.InnerException?.Message}），已略過";
					Debug.LogException(e.InnerException ?? e);
				}
			}

			AssetDatabase.SaveAssets();

			return report;
		}

		/// <summary>
		/// 所有掛 <see cref="DataEditorConfigAttribute" /> 的資料型別，與頁籤下拉同一來源
		/// </summary>
		/// <returns>資料型別清單</returns>
		public static List<Type> DataTypes()
		{
			return SumorinEditorUtility.GetTypesWithAttribute<SODataBase, DataEditorConfigAttribute>();
		}

		private static void ExportType<T>(string folder) where T: SODataBase
		{
			var members = Members(typeof(T));
			var rows = new List<List<string>> { members.Select(member => member.Name).ToList() };
			var dataSet = SumorinEditorUtility.FindAssetWithInheritance<DataSet<T>>();
			var datas = dataSet == null ? Enumerable.Empty<T>() : dataSet.Datas.Where(data => data != null);

			rows.AddRange(datas.Select(data => members.Select(member => CsvCellConverter.ToCell(member.GetMemberValue(data))).ToList()));
			CsvUtility.Write(Path.Combine(folder, typeof(T).Name + ".csv"), rows);
		}

		private static void ImportFile<T>(string path, CsvImportReport.FileReport file) where T: SODataBase
		{
			var rows = CsvUtility.Read(path);

			if(rows.Count == 0) return;

			var config = typeof(T).GetCustomAttribute<DataEditorConfigAttribute>();
			var membersByName = Members(typeof(T)).ToDictionary(member => member.Name);
			var header = rows[0];
			var idColumn = header.IndexOf(nameof(SODataBase.Id));
			var nameColumn = header.IndexOf(nameof(SODataBase.AssetName));
			var columns = new List<(int index, MemberInfo member)>();

			for(var i = 0; i < header.Count; i++)
			{
				if(i == idColumn || i == nameColumn) continue;

				if(membersByName.TryGetValue(header[i], out var member))
				{
					columns.Add((i, member));
				}
				else
				{
					file.Problems.Add($"欄位 {header[i]} 對不上任何成員，已略過");
				}
			}

			var dataSet = FindOrCreateDataSet<T>();

			for(var r = 1; r < rows.Count; r++)
			{
				var row = rows[r];

				if(row.All(string.IsNullOrEmpty)) continue;

				// 列號照 Excel 的算法，標題列是第 1 列
				var rowNumber = r + 1;
				string Cell(int column) => column >= 0 && column < row.Count ? row[column] : "";
				var id = Cell(idColumn);

				if(string.IsNullOrWhiteSpace(id))
				{
					Skip(file, rowNumber, "Id：空白");
					continue;
				}

				var assetName = Cell(nameColumn);

				if(string.IsNullOrWhiteSpace(assetName))
				{
					assetName = id;
				}

				if(!RegexChecking.OnlyEnglishAndNum(assetName))
				{
					Skip(file, rowNumber, $"AssetName：{assetName} 含非英數");
					continue;
				}

				var data = dataSet.Datas.Find(existing => existing != null && existing.Id == id);

				if(data == null)
				{
					data = LoadOrCreate(dataSet, config, id, assetName, out var skipReason);

					if(data == null)
					{
						Skip(file, rowNumber, skipReason);
						continue;
					}
				}

				SyncAssetName(data, assetName, rowNumber, file);

				foreach(var (index, member) in columns)
				{
					member.SetMemberValue(data, CsvCellConverter.Parse(Cell(index), member.GetReturnType(), out var problem));

					if(problem != null)
					{
						file.Problems.Add($"第 {rowNumber} 列 {member.Name}：{problem}");
					}
				}

				EditorUtility.SetDirty(data);
				file.Success++;
			}

			SumorinEditorUtility.SaveSOData(dataSet);
		}

		private static void Skip(CsvImportReport.FileReport file, int rowNumber, string reason)
		{
			file.Skipped++;
			file.Problems.Add($"第 {rowNumber} 列 {reason}，已跳過");
		}

		private static T LoadOrCreate<T>(DataSet<T> dataSet, DataEditorConfigAttribute config, string id, string assetName, out string skipReason)
			where T: SODataBase
		{
			skipReason = null;
			var relativePath = config.DataRoot + "/" + assetName;
			var assetPath = "Assets/" + relativePath + ".asset";
			T data;

			if(File.Exists(assetPath))
			{
				data = AssetDatabase.LoadAssetAtPath<T>(assetPath);

				if(data == null)
				{
					skipReason = $"AssetName：{assetName}.asset 已存在且型別不同";
					return null;
				}

				data.Id = id;
			}
			else
			{
				data = ScriptableObject.CreateInstance<T>();
				data.Id = id;
				data.AssetName = assetName;
				SumorinEditorUtility.CreateSOData(data, relativePath);
			}

			dataSet.AddData(data);

			return data;
		}

		private static void SyncAssetName<T>(T data, string assetName, int rowNumber, CsvImportReport.FileReport file) where T: SODataBase
		{
			var currentName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(data));

			if(AssetRenamer.TryRename(data, assetName, out var error)) return;

			file.Problems.Add($"第 {rowNumber} 列 AssetName：{error}，維持 {currentName}");
		}

		private static DataSet<T> FindOrCreateDataSet<T>() where T: SODataBase
		{
			var dataSet = SumorinEditorUtility.FindAssetWithInheritance<DataSet<T>>();

			if(dataSet != null) return dataSet;

			// 與 DataSetListEditor 建立集合的方式相同，集合不存在時新資產才有地方加
			var dataSetType = SumorinEditorUtility.GetDerivedClasses<DataSet<T>>().First();
			var created = (DataSet<T>)ScriptableObject.CreateInstance(dataSetType);
			SumorinEditorUtility.CreateSOData(created, "Data/Set/" + dataSetType.Name);

			return created;
		}

		private static List<MemberInfo> Members(Type type)
		{
			// 只取 SODataBase 以下的成員，SerializedScriptableObject 自己的序列化暫存欄位不進 CSV
			var members = FormatterUtilities.GetSerializableMembers(type, SerializationPolicies.Unity)
											.Where(member => typeof(SODataBase).IsAssignableFrom(member.DeclaringType))
											.ToList();
			var leading = new[] { nameof(SODataBase.Id), nameof(SODataBase.AssetName) };

			return leading.Select(name => members.First(member => member.Name == name))
						  .Concat(members.Where(member => !leading.Contains(member.Name)))
						  .ToList();
		}
	}
}