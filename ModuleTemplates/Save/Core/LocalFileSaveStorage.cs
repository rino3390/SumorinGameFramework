using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VContainer;

namespace Sumorin.Save
{
	/// <summary>
	///     存取媒介的預設實作，把存檔落在本機檔案
	/// </summary>
	/// <remarks>
	///     每個存檔槽對應一個資料夾，內含中繼資料檔與資料檔。
	///     存檔槽與全域槽放在不同的根目錄，全域槽不會出現在列舉結果。
	///     當場寫入不轉背景，命令回傳時檔案已落盤。
	///     存檔槽 Id 含檔案系統不接受的字元時回傳失敗，不自行清洗。
	/// </remarks>
	public class LocalFileSaveStorage: ISaveStorage
	{
		private const string SlotsFolderName = "Saves";
		private const string GlobalFolderName = "Global";
		private const string DataFileName = "data.json";
		private const string MetaFileName = "meta.json";

		private readonly ISerializer serializer;
		private readonly string rootPath;

		/// <summary>
		///     建立本機檔案存取媒介，存檔落在 <see cref="Application.persistentDataPath" />
		/// </summary>
		/// <param name="serializer">序列化器</param>
		[Inject]
		public LocalFileSaveStorage(ISerializer serializer): this(serializer, Application.persistentDataPath) { }

		/// <summary>
		///     建立本機檔案存取媒介，指定存檔的根目錄
		/// </summary>
		/// <param name="serializer">序列化器</param>
		/// <param name="rootPath">存檔根目錄，存檔槽與全域槽各自在其下的子目錄</param>
		public LocalFileSaveStorage(ISerializer serializer, string rootPath)
		{
			this.serializer = serializer;
			this.rootPath = rootPath;
		}

	#region ISaveStorage Members
		/// <inheritdoc />
		public bool Save(SaveSlotInfo info, IReadOnlyDictionary<string, string> data)
		{
			var slotId = info.SlotId;
			if(!IsValidSlotId(slotId)) return false;

			try
			{
				var directory = GetSlotDirectory(slotId);
				Directory.CreateDirectory(directory);
				File.WriteAllText(Path.Combine(directory, DataFileName), serializer.Serialize(data));
				File.WriteAllText(Path.Combine(directory, MetaFileName), serializer.Serialize(SlotMeta.From(info)));

				return true;
			}
			catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
			{
				Debug.LogError($"存檔寫入失敗：{slotId}，{exception.Message}");

				return false;
			}
		}

		/// <inheritdoc />
		public IReadOnlyDictionary<string, string> Load(string slotId)
		{
			if(!IsValidSlotId(slotId)) return null;

			var path = Path.Combine(GetSlotDirectory(slotId), DataFileName);
			if(!File.Exists(path)) return null;

			try
			{
				return serializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
			}
			catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
			{
				Debug.LogError($"存檔讀取失敗：{slotId}，{exception.Message}");

				return null;
			}
		}

		/// <inheritdoc />
		public bool Delete(string slotId)
		{
			if(!IsValidSlotId(slotId)) return false;

			var directory = GetSlotDirectory(slotId);
			if(!Directory.Exists(directory)) return false;

			try
			{
				Directory.Delete(directory, true);

				return true;
			}
			catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
			{
				Debug.LogError($"存檔刪除失敗：{slotId}，{exception.Message}");

				return false;
			}
		}

		/// <inheritdoc />
		public IReadOnlyList<SaveSlotInfo> ListSlots()
		{
			var slots = new List<SaveSlotInfo>();
			var slotsRoot = Path.Combine(rootPath, SlotsFolderName);
			if(!Directory.Exists(slotsRoot)) return slots;

			foreach(var directory in Directory.GetDirectories(slotsRoot))
			{
				var path = Path.Combine(directory, MetaFileName);

				if(!File.Exists(path))
				{
					continue;
				}

				var info = ReadInfo(path);

				if(string.IsNullOrEmpty(info.SlotId))
				{
					continue;
				}

				slots.Add(info);
			}

			return slots;
		}
	#endregion

		// ponytail: 只擋檔名層級的非法輸入，槽 Id 由誰輸入是專案層的事，不在此清洗
		private static bool IsValidSlotId(string slotId)
		{
			if(string.IsNullOrEmpty(slotId)) return false;
			if(slotId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;

			return slotId != "." && slotId != "..";
		}

		private string GetSlotDirectory(string slotId)
		{
			return slotId == SaveSlot.GlobalId ? Path.Combine(rootPath, GlobalFolderName) : Path.Combine(rootPath, SlotsFolderName, slotId);
		}

		private SaveSlotInfo ReadInfo(string path)
		{
			try
			{
				var meta = serializer.Deserialize<SlotMeta>(File.ReadAllText(path));

				return meta == null ? default : new SaveSlotInfo(meta.SlotId, meta.SavedAt, meta.Description);
			}
			catch(Exception exception) when(exception is IOException or UnauthorizedAccessException)
			{
				Debug.LogError($"中繼資料讀取失敗：{path}，{exception.Message}");

				return default;
			}
		}

	#region Nested type: SlotMeta
		// ponytail: 中繼資料檔的落地格式，SaveSlotInfo 是唯讀 struct，序列化器一律靠可寫屬性還原
		private class SlotMeta
		{
			public string SlotId { get; set; }

			public DateTime SavedAt { get; set; }

			public string Description { get; set; }

			public static SlotMeta From(SaveSlotInfo info)
			{
				return new()
				{
					SlotId = info.SlotId,
					SavedAt = info.SavedAt,
					Description = info.Description
				};
			}
		}
	#endregion
	}
}