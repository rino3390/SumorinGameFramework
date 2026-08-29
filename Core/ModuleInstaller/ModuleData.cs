using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Sumorin.ModuleInstaller
{
	/// <summary>
	/// 模組安裝狀態
	/// </summary>
	public enum ModuleInstallStatus
	{
		NotInstalled,
		Installed,
		PartiallyInstalled,
		UpdateAvailable
	}

	/// <summary>
	/// 模組清單根結構（對應 modules.json）
	/// </summary>
	[Serializable]
	public class ModuleManifest
	{
		public string version;
		public string baseUrl;
		public List<ModuleInfo> modules = new();
	}

	/// <summary>
	/// 單一模組的資訊
	/// </summary>
	[Serializable]
	public class ModuleInfo
	{
		public string id;
		public string name;
		public string description;
		public string version;
		public List<string> dependencies = new();
		public string installPath;
		public List<string> folders = new();
		public List<string> files = new();
	}

	/// <summary>
	/// GitHub Contents API 回應項目
	/// </summary>
	[Serializable]
	public class GitHubContentItem
	{
		public string name;
		public string path;
		public string type; // "file" or "dir"
	}

	/// <summary>
	/// 模組運行時狀態（包含安裝狀態檢測結果）
	/// </summary>
	public class ModuleRuntimeData
	{
		public ModuleInfo Info { get; }
		public ModuleInstallStatus Status { get; set; }
		public List<string> MissingFiles { get; } = new();
		public List<string> InstalledFiles { get; } = new();
		public List<string> MissingDependencies { get; } = new();
		public bool HasUnmetDependencies => MissingDependencies.Count > 0;

		/// <summary>
		/// 本地紀錄的已安裝版本，沒有紀錄時為 null
		/// </summary>
		public string InstalledVersion { get; set; }

		/// <summary>
		/// 檔案已完整安裝（含有新版可更新的情況）
		/// </summary>
		public bool IsInstalled => Status is ModuleInstallStatus.Installed or ModuleInstallStatus.UpdateAvailable;

		/// <summary>
		/// 從 folders 展開後的所有檔案路徑
		/// </summary>
		public List<string> ResolvedFiles { get; } = new();

		/// <summary>
		/// 是否已完成資料夾解析
		/// </summary>
		public bool IsFoldersResolved { get; set; }

		public ModuleRuntimeData(ModuleInfo info)
		{
			Info = info;
			Status = ModuleInstallStatus.NotInstalled;
		}

		/// <summary>
		/// 取得所有檔案（直接指定的 files + 從 folders 展開的檔案）
		/// </summary>
		public List<string> GetAllFiles()
		{
			var allFiles = new List<string>(Info.files);
			allFiles.AddRange(ResolvedFiles);
			return allFiles;
		}
	}

	/// <summary>
	/// 模組安裝紀錄檔的根結構
	/// </summary>
	[Serializable]
	public class ModuleInstallRecords
	{
		public List<ModuleInstallRecord> modules = new();
	}

	/// <summary>
	/// 單一模組的安裝紀錄
	/// </summary>
	[Serializable]
	public class ModuleInstallRecord
	{
		public string id;
		public string version;
	}

	/// <summary>
	/// 模組已安裝版本的紀錄讀寫與版本比對
	/// </summary>
	public static class ModuleVersionRecord
	{
		/// <summary>
		/// 讀取安裝紀錄，檔案不存在或內容無法解析時回傳空字典
		/// </summary>
		public static Dictionary<string, string> Load(string path)
		{
			var versions = new Dictionary<string, string>();

			if(!File.Exists(path)) return versions;

			try
			{
				var records = JsonUtility.FromJson<ModuleInstallRecords>(File.ReadAllText(path));

				foreach(var record in records?.modules ?? new List<ModuleInstallRecord>())
				{
					if(!string.IsNullOrEmpty(record?.id))
					{
						versions[record.id] = record.version;
					}
				}
			}
			catch(Exception)
			{
				// ponytail: 紀錄檔損毀時當作沒安裝過，使用者重新安裝即可重建
				versions.Clear();
			}

			return versions;
		}

		/// <summary>
		/// 寫入安裝紀錄
		/// </summary>
		public static void Save(string path, Dictionary<string, string> versions)
		{
			var records = new ModuleInstallRecords();

			foreach(var pair in versions)
			{
				records.modules.Add(new() { id = pair.Key, version = pair.Value });
			}

			var directory = Path.GetDirectoryName(path);

			if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			File.WriteAllText(path, JsonUtility.ToJson(records, true));
		}

		/// <summary>
		/// 判斷遠端版本是否比已安裝版本新。沒有安裝紀錄時視為可更新
		/// </summary>
		public static bool IsUpdateAvailable(string installedVersion, string remoteVersion)
		{
			if(string.IsNullOrEmpty(installedVersion)) return true;

			if(Version.TryParse(installedVersion, out var installed) && Version.TryParse(remoteVersion, out var remote))
			{
				return remote > installed;
			}

			return installedVersion != remoteVersion;
		}
	}
}