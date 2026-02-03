using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Sumorin.GameFramework.ModuleInstaller
{
    /// <summary>
    /// 模組安裝器 Editor Window
    /// 提供從 GitHub 下載並安裝可選模組的功能
    /// </summary>
    public class ModuleInstaller : EditorWindow
    {
        private const string ManifestUrl = "https://raw.githubusercontent.com/rino3390/SumorinGameFramework/main/ModuleTemplates/modules.json";
        private const string GitHubApiBaseUrl = "https://api.github.com/repos/rino3390/SumorinGameFramework/contents/";
        private const string FolderStructurePrefix = "FolderStructure/";
        private const string ModuleTemplatesPrefix = "ModuleTemplates/";
        private const string DomainsPath = "Script/Domains";
        private const string ScriptPath = "Script";

        private ModuleManifest manifest;
        private List<ModuleRuntimeData> modules = new();
        private Vector2 scrollPosition;
        private bool isLoading;
        private bool isDownloading;
        private ModuleRuntimeData downloadingModule;
        private string errorMessage;

        [MenuItem("Tools/Sumorin/Module Installer", priority = 10_100)]
        public static void OpenWindow()
        {
            var window = GetWindow<ModuleInstaller>("Module Installer");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshModuleList();
        }

        private void OnGUI()
        {
            if (isLoading)
            {
                EditorGUILayout.HelpBox("載入模組清單中...", MessageType.Info);
                return;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }

            DrawModuleList();
        }

        private void DrawModuleList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 分離基礎模組與可選模組
            var baseModules = modules.Where(IsBaseModule).ToList();
            var optionalModules = modules.Where(m => !IsBaseModule(m)).ToList();

            // 基礎模組區塊
            if (baseModules.Count > 0)
            {
                EditorGUILayout.LabelField("基礎模組", EditorStyles.boldLabel);
                foreach (var module in baseModules)
                {
                    DrawModuleItem(module);
                }
                EditorGUILayout.Space(10);
            }

            // 可選模組區塊
            if (optionalModules.Count > 0)
            {
                EditorGUILayout.LabelField($"可選模組 ({optionalModules.Count})", EditorStyles.boldLabel);
                foreach (var module in optionalModules)
                {
                    DrawModuleItem(module);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool IsBaseModule(ModuleRuntimeData module)
        {
            // FolderStructure 是基礎模組
            return module.Info.id.Equals("folder-structure", StringComparison.OrdinalIgnoreCase) ||
                   module.Info.name.Equals("FolderStructure", StringComparison.OrdinalIgnoreCase);
        }

        private void DrawModuleItem(ModuleRuntimeData module)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 標題列
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(module.Info.name, EditorStyles.boldLabel);
            DrawStatusBadge(module.Status);
            EditorGUILayout.EndHorizontal();

            // 版本與說明
            EditorGUILayout.LabelField($"版本: {module.Info.version} | ID: {module.Info.id}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(module.Info.description, EditorStyles.wordWrappedLabel);

            // 依賴資訊（含狀態圖示）
            if (module.Info.dependencies.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("依賴：", GUILayout.Width(40));
                foreach (var dep in module.Info.dependencies)
                {
                    var isSatisfied = !module.MissingDependencies.Contains(dep);
                    DrawDependencyStatus(dep, isSatisfied);
                }
                EditorGUILayout.EndHorizontal();
            }

            // 部分安裝警告
            if (module.Status == ModuleInstallStatus.PartiallyInstalled)
            {
                var missingFiles = string.Join("\n", module.MissingFiles);
                EditorGUILayout.HelpBox($"部分檔案遺失:\n{missingFiles}", MessageType.Warning);
            }

            // 操作按鈕
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var isThisModuleDownloading = isDownloading && downloadingModule == module;

            if (isThisModuleDownloading)
            {
                GUILayout.Label("下載中...", EditorStyles.boldLabel);
            }
            else
            {
                GUI.enabled = !isDownloading;

                switch (module.Status)
                {
                    case ModuleInstallStatus.NotInstalled:
                        GUI.enabled = !module.HasUnmetDependencies && !isDownloading;
                        if (GUILayout.Button("安裝", GUILayout.Width(80)))
                        {
                            InstallModuleAsync(module).Forget();
                        }
                        GUI.enabled = !isDownloading;
                        break;

                    case ModuleInstallStatus.Installed:
                        if (GUILayout.Button("移除", GUILayout.Width(80)))
                        {
                            RemoveModule(module);
                        }
                        break;

                    case ModuleInstallStatus.PartiallyInstalled:
                        if (GUILayout.Button("修復", GUILayout.Width(80)))
                        {
                            RepairModuleAsync(module).Forget();
                        }
                        if (GUILayout.Button("移除", GUILayout.Width(80)))
                        {
                            RemoveModule(module);
                        }
                        break;
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawStatusBadge(ModuleInstallStatus status)
        {
            var originalColor = GUI.backgroundColor;

			var noHoverStyle = new GUIStyle(EditorStyles.miniButton)
			{
				hover =
				{
					background = null
				}
			};

			switch (status)
            {
                case ModuleInstallStatus.Installed:
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                    GUILayout.Label("已安裝", noHoverStyle, GUILayout.Width(60));
                    break;
                case ModuleInstallStatus.PartiallyInstalled:
                    GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
                    GUILayout.Label("部分安裝", noHoverStyle, GUILayout.Width(60));
                    break;
                case ModuleInstallStatus.NotInstalled:
                    GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("未安裝", noHoverStyle, GUILayout.Width(60));
                    break;
            }

            GUI.backgroundColor = originalColor;
        }

        private void DrawDependencyStatus(string dependencyName, bool isSatisfied)
        {
            var originalColor = GUI.contentColor;
            var symbol = isSatisfied ? "✓" : "✗";
            GUI.contentColor = isSatisfied ? new Color(0.3f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.4f);
            GUILayout.Label($"{symbol} {dependencyName}", EditorStyles.label);
            GUI.contentColor = originalColor;
        }

        private void RefreshModuleList()
        {
            isLoading = true;
            errorMessage = null;
            FetchManifestAsync().Forget();
        }

        private async UniTaskVoid FetchManifestAsync()
        {
            try
            {
                var request = UnityWebRequest.Get(ManifestUrl);
                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    errorMessage = $"無法載入模組清單: {request.error}";
                    return;
                }

                manifest = JsonUtility.FromJson<ModuleManifest>(request.downloadHandler.text);
                modules = manifest.modules.Select(m => new ModuleRuntimeData(m)).ToList();
                CheckAllModuleStatus();
            }
            catch (Exception e)
            {
                errorMessage = $"解析模組清單失敗: {e.Message}";
            }
            finally
            {
                isLoading = false;
                Repaint();
            }
        }

        private void CheckAllModuleStatus()
        {
            var installedModuleIds = new HashSet<string>();

            foreach (var module in modules)
            {
                CheckModuleStatus(module);
                if (module.Status == ModuleInstallStatus.Installed)
                {
                    installedModuleIds.Add(module.Info.id);
                }
            }

            // 檢查依賴
            foreach (var module in modules)
			{
				module.MissingDependencies.Clear();

				foreach(var dep in module.Info.dependencies.Where(dep => !installedModuleIds.Contains(dep)))
				{
					module.MissingDependencies.Add(dep);
				}
			}
        }

        private void CheckModuleStatus(ModuleRuntimeData module)
        {
            module.MissingFiles.Clear();
            module.InstalledFiles.Clear();

            // FolderStructure 特殊處理：檢查資料夾是否存在
            if (IsBaseModule(module))
            {
                CheckFolderStructureStatus(module);
                return;
            }

            // 使用 GetAllFiles() 取得所有檔案（包含從 folders 展開的）
            var allFiles = module.GetAllFiles();
            foreach (var file in allFiles)
            {
                var localPath = GetLocalFilePath(file);
                if (File.Exists(localPath))
                {
                    module.InstalledFiles.Add(file);
                }
                else
                {
                    module.MissingFiles.Add(file);
                }
            }

            if (module.InstalledFiles.Count == 0)
            {
                module.Status = ModuleInstallStatus.NotInstalled;
            }
            else if (module.MissingFiles.Count == 0)
            {
                module.Status = ModuleInstallStatus.Installed;
            }
            else
            {
                module.Status = ModuleInstallStatus.PartiallyInstalled;
            }
        }

        private void CheckFolderStructureStatus(ModuleRuntimeData module)
        {
            // 檢查關鍵資料夾是否存在
            var keyFolders = new[]
            {
                "Script/Domains",
                "Script/Flow",
                "Script/Presenter",
                "Script/View",
                "Art",
                "Data",
                "Prefab",
                "Resources",
                "Scenes"
            };

            var existingFolders = 0;
            foreach (var folder in keyFolders)
            {
                var folderPath = Path.Combine(Application.dataPath, folder);
                if (Directory.Exists(folderPath))
                {
                    existingFolders++;
                    module.InstalledFiles.Add(folder);
                }
                else
                {
                    module.MissingFiles.Add(folder);
                }
            }

            if (existingFolders == 0)
            {
                module.Status = ModuleInstallStatus.NotInstalled;
            }
            else if (existingFolders == keyFolders.Length)
            {
                module.Status = ModuleInstallStatus.Installed;
            }
            else
            {
                module.Status = ModuleInstallStatus.PartiallyInstalled;
            }
        }

        private string GetLocalFilePath(string relativePath)
        {
            // FolderStructure 檔案：移除前綴，安裝到 Assets/
            if (relativePath.StartsWith(FolderStructurePrefix))
            {
                var localPath = relativePath.Substring(FolderStructurePrefix.Length);
                return Path.Combine(Application.dataPath, localPath).Replace("\\", "/");
            }

            // ModuleTemplates 檔案：移除前綴，根據 FolderStructure 是否存在決定路徑
            if (relativePath.StartsWith(ModuleTemplatesPrefix))
            {
                var modulePath = relativePath.Substring(ModuleTemplatesPrefix.Length);
                var basePath = IsFolderStructureInstalled() ? DomainsPath : ScriptPath;
                return Path.Combine(Application.dataPath, basePath, modulePath).Replace("\\", "/");
            }

            // 其他情況：直接安裝到 Assets/
            return Path.Combine(Application.dataPath, relativePath).Replace("\\", "/");
        }

        private bool IsFolderStructureInstalled()
        {
            var domainsPath = Path.Combine(Application.dataPath, DomainsPath);
            return Directory.Exists(domainsPath);
        }

        private string GetRemoteFileUrl(string relativePath)
        {
            return manifest.baseUrl + relativePath;
        }

        private async UniTaskVoid InstallModuleAsync(ModuleRuntimeData module)
        {
            if (module.HasUnmetDependencies)
            {
                EditorUtility.DisplayDialog("無法安裝",
                    $"此模組需要先安裝以下依賴：\n{string.Join("\n", module.MissingDependencies)}",
                    "確定");
                return;
            }

            isDownloading = true;
            downloadingModule = module;
            Repaint();

            try
            {
                // 如果有 folders 需要先解析
                if (module.Info.folders.Count > 0 && !module.IsFoldersResolved)
                {
                    await ResolveFoldersAsync(module);
                }

                await DownloadModuleFilesAsync(module, module.GetAllFiles());
            }
            finally
            {
                isDownloading = false;
                downloadingModule = null;
                Repaint();
            }
        }

        private async UniTaskVoid RepairModuleAsync(ModuleRuntimeData module)
        {
            isDownloading = true;
            downloadingModule = module;
            Repaint();

            try
            {
                // 如果有 folders 但尚未解析，需要先解析
                if (module.Info.folders.Count > 0 && !module.IsFoldersResolved)
                {
                    await ResolveFoldersAsync(module);
                    CheckModuleStatus(module);
                }

                await DownloadModuleFilesAsync(module, module.MissingFiles);
            }
            finally
            {
                isDownloading = false;
                downloadingModule = null;
                Repaint();
            }
        }

        private async UniTask ResolveFoldersAsync(ModuleRuntimeData module)
        {
            module.ResolvedFiles.Clear();

            foreach (var folder in module.Info.folders)
            {
                var files = await FetchFolderContentsAsync(folder);
                module.ResolvedFiles.AddRange(files);
            }

            module.IsFoldersResolved = true;
        }

        private async UniTask DownloadModuleFilesAsync(ModuleRuntimeData module, List<string> files)
        {
            var failedFiles = new List<string>();

            foreach (var file in files)
            {
                var url = GetRemoteFileUrl(file);
                var localPath = GetLocalFilePath(file);

                try
                {
                    var request = UnityWebRequest.Get(url);
                    await request.SendWebRequest().ToUniTask();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        failedFiles.Add($"{file}: {request.error}");
                    }
                    else
                    {
                        // 空檔案（如 .gitkeep）也要建立
                        var data = request.downloadHandler.data ?? Array.Empty<byte>();
                        SaveFile(localPath, data);
                    }
                }
                catch (Exception e)
                {
                    failedFiles.Add($"{file}: {e.Message}");
                }
            }

            AssetDatabase.Refresh();
            CheckModuleStatus(module);
            CheckAllModuleStatus();

            if (failedFiles.Count > 0)
            {
                errorMessage = $"安裝 {module.Info.name} 時發生錯誤:\n{string.Join("\n", failedFiles)}";
            }
        }

        private async UniTask<List<string>> FetchFolderContentsAsync(string folderPath)
        {
            var url = GitHubApiBaseUrl + folderPath + "?ref=main";
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", "Unity-ModuleInstaller");

            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"解析資料夾 {folderPath} 失敗: {request.error}");
            }

            var json = request.downloadHandler.text;
            var items = ParseGitHubContentsResponse(json);
            var files = new List<string>();

            foreach (var item in items)
            {
                if (item.type == "file" && !item.name.EndsWith(".meta"))
                {
                    files.Add(item.path);
                }
                else if (item.type == "dir")
                {
                    // 遞迴處理子資料夾
                    var subFiles = await FetchFolderContentsAsync(item.path);
                    files.AddRange(subFiles);
                }
            }

            return files;
        }

        private void SaveFile(string path, byte[] data)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, data);
        }

        private void RemoveModule(ModuleRuntimeData module)
        {
            // 檢查是否有其他模組依賴此模組
            var dependentModules = modules
                .Where(m => m.Status == ModuleInstallStatus.Installed &&
                            m.Info.dependencies.Contains(module.Info.id))
                .Select(m => m.Info.name)
                .ToList();

            // 建立確認訊息
            var message = "";
            var isBaseModule = IsBaseModule(module);

            if (isBaseModule)
            {
                // FolderStructure 特殊警告
                message = "⚠️ 警告：這會刪除所有資料夾結構，包含資料夾內的檔案，可能導致專案毀損\n\n";
            }
            else if (dependentModules.Count > 0)
            {
                // 被依賴模組警告
                message = $"⚠️ 警告：「{string.Join("」「", dependentModules)}」模組依賴此模組\n\n";
            }

            message += $"確定要移除「{module.Info.name}」嗎？\n\n這將刪除以下項目：\n• {string.Join("\n• ", module.InstalledFiles)}";

            // 確認對話框
            if (!EditorUtility.DisplayDialog("確認移除", message, "確定移除", "取消"))
            {
                return;
            }

            // FolderStructure 特殊處理：刪除整個資料夾
            if (isBaseModule)
            {
                RemoveFolderStructure();
            }
            else
            {
                // 刪除檔案
                foreach (var file in module.InstalledFiles)
                {
                    var localPath = GetLocalFilePath(file);
                    if (File.Exists(localPath))
                    {
                        File.Delete(localPath);
                        var metaPath = localPath + ".meta";
                        if (File.Exists(metaPath))
                        {
                            File.Delete(metaPath);
                        }
                    }
                }

                // 刪除空目錄
                CleanupEmptyDirectories(module);
            }

            AssetDatabase.Refresh();
            CheckModuleStatus(module);
            CheckAllModuleStatus();
            Repaint();
        }

        private void RemoveFolderStructure()
        {
            // 刪除 FolderStructure 的根資料夾
            var rootFolders = new[] { "Art", "Data", "Prefab", "Resources", "Scenes", "Script" };

            foreach (var folder in rootFolders)
            {
                var folderPath = Path.Combine(Application.dataPath, folder);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                    var metaPath = folderPath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
            }
        }

        private void CleanupEmptyDirectories(ModuleRuntimeData module)
        {
            // 找出模組的根目錄（Domain 資料夾）
            var directories = module.GetAllFiles()
                .Select(f => Path.GetDirectoryName(GetLocalFilePath(f)))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct()
                .OrderByDescending(d => d.Length) // 從最深的目錄開始
                .ToList();

            foreach (var dir in directories)
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    var metaPath = dir + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }
                }
            }
        }

        /// <summary>
        /// 解析 GitHub Contents API 的 JSON 陣列回應
        /// </summary>
        private List<GitHubContentItem> ParseGitHubContentsResponse(string json)
        {
            // GitHub API 回傳 JSON 陣列，JsonUtility 不支援直接解析陣列
            // 包裝成物件來解析
            var wrappedJson = "{\"items\":" + json + "}";
            var wrapper = JsonUtility.FromJson<GitHubContentsWrapper>(wrappedJson);
            return wrapper.items;
        }

        [Serializable]
        private class GitHubContentsWrapper
        {
            public List<GitHubContentItem> items;
        }
    }
}
