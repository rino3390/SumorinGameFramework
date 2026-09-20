using System.IO;
using Sumorin.SumorinUtility;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 資產檔名的改名操作
	/// </summary>
	/// <remarks>
	/// 編輯器的確認按鈕與 CSV 匯入共用這條路徑，檔名規則才不會兩邊各走一套。
	/// </remarks>
	public static class AssetRenamer
	{
	#if UNITY_EDITOR
		/// <summary>
		/// 把資產檔名改為指定名稱，並同步 <see cref="SODataBase.AssetName" />
		/// </summary>
		/// <param name="data">要改名的資料</param>
		/// <param name="newName">新檔名，不含副檔名</param>
		/// <param name="error">失敗原因，成功時為 null</param>
		/// <returns>改名成功或檔名本來就相符則回傳 true</returns>
		public static bool TryRename(SODataBase data, string newName, out string error)
		{
			error = null;
			var path = AssetDatabase.GetAssetPath(data);

			if(string.IsNullOrEmpty(path))
			{
				error = "資產尚未存檔";
				return false;
			}

			if(string.IsNullOrWhiteSpace(newName) || !RegexChecking.OnlyEnglishAndNum(newName))
			{
				error = "名稱只能為英數";
				return false;
			}

			if(Path.GetFileNameWithoutExtension(path) != newName)
			{
				if(File.Exists(path[..(path.LastIndexOf('/') + 1)] + newName + ".asset"))
				{
					error = "檔名已存在";
					return false;
				}

				var renameFailure = AssetDatabase.RenameAsset(path, newName);

				if(!string.IsNullOrEmpty(renameFailure))
				{
					error = renameFailure;
					return false;
				}
			}

			data.AssetName = newName;
			EditorUtility.SetDirty(data);

			return true;
		}
	#endif
	}
}