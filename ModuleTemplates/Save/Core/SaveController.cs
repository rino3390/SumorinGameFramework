using System;
using System.Collections.Generic;
using System.Linq;
using Sumorin.DDDCore;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔 Controller，協調存讀檔流程
	/// </summary>
	/// <remarks>
	///     只負責收集與分派參與者資料，序列化邏輯全在參與者，落盤由存取媒介負責。
	///     存檔槽與全域槽的參與者互不干擾，每個命令只處理同一範圍。
	/// </remarks>
	public class SaveController: ISaveController
	{
		private readonly IReadOnlyList<ISaveParticipant> participants;
		private readonly ISaveStorage storage;
		private readonly IPublisher publisher;

		/// <summary>
		///     建立存檔 Controller
		/// </summary>
		/// <param name="participants">所有已註冊的參與者，沒有任何參與者時容器給空集合</param>
		/// <param name="storage">存取媒介</param>
		/// <param name="publisher">事件發布者</param>
		/// <exception cref="InvalidOperationException">存檔鍵重複時拋出</exception>
		public SaveController(IReadOnlyList<ISaveParticipant> participants, ISaveStorage storage, IPublisher publisher)
		{
			this.participants = participants ?? new List<ISaveParticipant>();
			this.storage = storage;
			this.publisher = publisher;
			AssertUniqueSaveKeys();
		}

	#region ISaveController Members
		/// <inheritdoc />
		public CommandResult Save(string slotId, string description)
		{
			var validation = ValidateSlotId(slotId);
			if(!validation.IsSuccess) return validation;

			return Write(slotId, description, false);
		}

		/// <inheritdoc />
		public CommandResult Load(string slotId)
		{
			var validation = ValidateSlotId(slotId);
			if(!validation.IsSuccess) return validation;

			var data = storage.Load(slotId);
			if(data == null) return CommandResult.Fail($"存檔槽不存在：{slotId}");

			Restore(data, false);
			publisher.Publish(new GameLoaded(slotId));
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult Delete(string slotId)
		{
			var validation = ValidateSlotId(slotId);
			if(!validation.IsSuccess) return validation;

			return storage.Delete(slotId) ? CommandResult.Ok() : CommandResult.Fail($"存檔槽不存在：{slotId}");
		}

		/// <inheritdoc />
		public CommandResult NewGame()
		{
			foreach(var participant in participants)
			{
				if(participant.IsGlobal)
				{
					continue;
				}

				participant.Clear();
			}

			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public CommandResult SaveGlobal() => Write(SaveSlot.GlobalId, string.Empty, true);

		/// <inheritdoc />
		public CommandResult LoadGlobal()
		{
			var data = storage.Load(SaveSlot.GlobalId);
			if(data == null) return CommandResult.Ok();

			Restore(data, true);
			return CommandResult.Ok();
		}

		/// <inheritdoc />
		public IReadOnlyList<SaveSlotInfo> GetSlots()
		{
			return storage.ListSlots().Where(slot => slot.SlotId != SaveSlot.GlobalId).ToList();
		}
	#endregion

		private static CommandResult ValidateSlotId(string slotId)
		{
			if(string.IsNullOrEmpty(slotId)) return CommandResult.Fail("存檔槽 Id 不得為空");
			if(slotId == SaveSlot.GlobalId) return CommandResult.Fail($"不可使用保留 Id：{SaveSlot.GlobalId}");

			return CommandResult.Ok();
		}

		private CommandResult Write(string slotId, string description, bool isGlobal)
		{
			var data = new Dictionary<string, string>();

			foreach(var participant in participants)
			{
				if(participant.IsGlobal != isGlobal)
				{
					continue;
				}

				data[participant.SaveKey] = participant.Export();
			}

			var info = new SaveSlotInfo(slotId, DateTime.Now, description);
			return storage.Save(info, data) ? CommandResult.Ok() : CommandResult.Fail($"存檔失敗：{slotId}");
		}

		private void Restore(IReadOnlyDictionary<string, string> data, bool isGlobal)
		{
			var ordered = participants.Where(participant => participant.IsGlobal == isGlobal).OrderByDescending(participant => participant.LoadOrder);

			foreach(var participant in ordered)
			{
				participant.Clear();

				if(data.TryGetValue(participant.SaveKey, out var payload))
				{
					participant.Import(payload);
				}
			}
		}

		private void AssertUniqueSaveKeys()
		{
			var duplicated = participants.GroupBy(participant => participant.SaveKey).FirstOrDefault(group => group.Count() > 1);

			if(duplicated != null)
			{
				throw new InvalidOperationException($"存檔鍵重複：{duplicated.Key}");
			}
		}
	}
}