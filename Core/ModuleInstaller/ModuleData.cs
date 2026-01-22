using System;
using System.Collections.Generic;

namespace Rino.GameFramework.ModuleInstaller
{
    /// <summary>
    /// 模組安裝狀態
    /// </summary>
    public enum ModuleInstallStatus
    {
        NotInstalled,
        Installed,
        PartiallyInstalled
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
}
