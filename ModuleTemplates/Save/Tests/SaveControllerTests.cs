using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.DDDCore;

namespace Sumorin.Save.Tests
{
	[TestFixture]
	public class SaveControllerTests
	{
		private ISaveStorage storage;
		private IPublisher publisher;

		[SetUp]
		public void Setup()
		{
			storage = Substitute.For<ISaveStorage>();
			publisher = Substitute.For<IPublisher>();
			storage.Save(Arg.Any<SaveSlotInfo>(), Arg.Any<IReadOnlyDictionary<string, string>>()).Returns(true);
			storage.Delete(Arg.Any<string>()).Returns(true);
		}

		[Test]
		public void Constructor_WithDuplicatedSaveKeys_Throws()
		{
			var participants = new List<ISaveParticipant>
			{
				CreateParticipant("progress", 0, false, "a"),
				CreateParticipant("progress", 0, false, "b")
			};

			Action act = () => new SaveController(participants, storage, publisher);

			act.Should().ThrowExactly<InvalidOperationException>();
		}

		[Test]
		public void Constructor_WithoutRegisteredParticipants_TreatsThemAsEmpty()
		{
			var controller = new SaveController(null, storage, publisher);
			IReadOnlyDictionary<string, string> capturedData = null;
			storage.Save(Arg.Any<SaveSlotInfo>(), Arg.Do<IReadOnlyDictionary<string, string>>(data => capturedData = data)).Returns(true);

			var result = controller.Save("slot-1", "空存檔");

			result.IsSuccess.Should().BeTrue();
			capturedData.Should().BeEmpty();
		}

		[Test]
		public void Save_WithSlotScopedParticipants_WritesDataKeyedBySaveKey()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var controller = CreateController(progress, settings);
			IReadOnlyDictionary<string, string> capturedData = null;
			var capturedInfo = default(SaveSlotInfo);
			storage.Save(Arg.Do<SaveSlotInfo>(info => capturedInfo = info), Arg.Do<IReadOnlyDictionary<string, string>>(data => capturedData = data))
				   .Returns(true);

			var result = controller.Save("slot-1", "第一章");

			result.IsSuccess.Should().BeTrue();
			capturedData.Should().BeEquivalentTo(new Dictionary<string, string> { ["progress"] = "progress-data" });
			capturedInfo.Should()
						.BeEquivalentTo(
							new
							{
								SlotId = "slot-1",
								Description = "第一章"
							}
						);
			capturedInfo.SavedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
			settings.DidNotReceive().Export();
		}

		[Test]
		public void Save_WithoutParticipants_WritesEmptyData()
		{
			var controller = CreateController();
			IReadOnlyDictionary<string, string> capturedData = null;
			storage.Save(Arg.Any<SaveSlotInfo>(), Arg.Do<IReadOnlyDictionary<string, string>>(data => capturedData = data)).Returns(true);

			var result = controller.Save("slot-1", "空存檔");

			result.IsSuccess.Should().BeTrue();
			capturedData.Should().BeEmpty();
		}

		[Test]
		public void Save_WhenStorageRejects_Fails()
		{
			var controller = CreateController(CreateParticipant("progress", 0, false, "progress-data"));
			storage.Save(Arg.Any<SaveSlotInfo>(), Arg.Any<IReadOnlyDictionary<string, string>>()).Returns(false);

			var result = controller.Save("slot-1", "第一章");

			result.IsSuccess.Should().BeFalse();
			result.FailureReason.Should().NotBeNullOrEmpty();
		}

		[TestCase(null, TestName = "存檔槽 Id 為 null 回傳失敗")]
		[TestCase("", TestName = "存檔槽 Id 為空字串回傳失敗")]
		[TestCase(SaveSlot.GlobalId, TestName = "以保留 Id 存檔回傳失敗")]
		public void Save_WithInvalidSlotId_FailsWithoutTouchingStorage(string slotId)
		{
			var controller = CreateController(CreateParticipant("progress", 0, false, "progress-data"));

			var result = controller.Save(slotId, "第一章");

			result.IsSuccess.Should().BeFalse();
			storage.DidNotReceive().Save(Arg.Any<SaveSlotInfo>(), Arg.Any<IReadOnlyDictionary<string, string>>());
		}

		[Test]
		public void Load_WithExistingSlot_ClearsImportsAndPublishesGameLoaded()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var controller = CreateController(progress);
			storage.Load("slot-1").Returns(new Dictionary<string, string> { ["progress"] = "stored" });

			var result = controller.Load("slot-1");

			result.IsSuccess.Should().BeTrue();
			Received.InOrder(() =>
				{
					progress.Clear();
					progress.Import("stored");
				}
			);
			publisher.Received(1).Publish(Arg.Is<GameLoaded>(evt => evt.SlotId == "slot-1"));
		}

		[Test]
		public void Load_WithoutMatchingSaveKey_ClearsWithoutImporting()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var controller = CreateController(progress);
			storage.Load("slot-1").Returns(new Dictionary<string, string> { ["other"] = "stored" });

			var result = controller.Load("slot-1");

			result.IsSuccess.Should().BeTrue();
			progress.Received(1).Clear();
			progress.DidNotReceive().Import(Arg.Any<string>());
		}

		[Test]
		public void Load_WithMultipleParticipants_ProcessesInDescendingLoadOrder()
		{
			var first = CreateParticipant("first", 10, false, "first-data");
			var second = CreateParticipant("second", 5, false, "second-data");
			var controller = CreateController(second, first);
			storage.Load("slot-1")
				   .Returns(
					   new Dictionary<string, string>
					   {
						   ["first"] = "first-stored",
						   ["second"] = "second-stored"
					   }
				   );

			controller.Load("slot-1");

			Received.InOrder(() =>
				{
					first.Clear();
					first.Import("first-stored");
					second.Clear();
					second.Import("second-stored");
				}
			);
		}

		[Test]
		public void Load_WithGlobalParticipant_LeavesItUntouched()
		{
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var controller = CreateController(settings);
			storage.Load("slot-1").Returns(new Dictionary<string, string> { ["settings"] = "stored" });

			controller.Load("slot-1");

			settings.DidNotReceive().Clear();
			settings.DidNotReceive().Import(Arg.Any<string>());
		}

		[Test]
		public void Load_WithMissingSlot_FailsWithoutClearingOrPublishing()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var controller = CreateController(progress);
			storage.Load("slot-1").Returns((IReadOnlyDictionary<string, string>)null);

			var result = controller.Load("slot-1");

			result.IsSuccess.Should().BeFalse();
			progress.DidNotReceive().Clear();
			publisher.DidNotReceive().Publish(Arg.Any<GameLoaded>());
		}

		[TestCase(null, TestName = "載入 null Id 回傳失敗")]
		[TestCase("", TestName = "載入空字串 Id 回傳失敗")]
		[TestCase(SaveSlot.GlobalId, TestName = "以保留 Id 載入回傳失敗")]
		public void Load_WithInvalidSlotId_FailsWithoutTouchingStorage(string slotId)
		{
			var controller = CreateController(CreateParticipant("progress", 0, false, "progress-data"));

			var result = controller.Load(slotId);

			result.IsSuccess.Should().BeFalse();
			storage.DidNotReceive().Load(Arg.Any<string>());
		}

		[Test]
		public void Delete_WithExistingSlot_RemovesIt()
		{
			var controller = CreateController();

			var result = controller.Delete("slot-1");

			result.IsSuccess.Should().BeTrue();
			storage.Received(1).Delete("slot-1");
		}

		[Test]
		public void Delete_WithMissingSlot_Fails()
		{
			var controller = CreateController();
			storage.Delete("slot-1").Returns(false);

			var result = controller.Delete("slot-1");

			result.IsSuccess.Should().BeFalse();
		}

		[TestCase(null, TestName = "刪除 null Id 回傳失敗")]
		[TestCase("", TestName = "刪除空字串 Id 回傳失敗")]
		[TestCase(SaveSlot.GlobalId, TestName = "刪除保留 Id 回傳失敗")]
		public void Delete_WithInvalidSlotId_FailsWithoutTouchingStorage(string slotId)
		{
			var controller = CreateController();

			var result = controller.Delete(slotId);

			result.IsSuccess.Should().BeFalse();
			storage.DidNotReceive().Delete(Arg.Any<string>());
		}

		[Test]
		public void NewGame_ClearsSlotScopedParticipantsOnly()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var controller = CreateController(progress, settings);

			var result = controller.NewGame();

			result.IsSuccess.Should().BeTrue();
			progress.Received(1).Clear();
			settings.DidNotReceive().Clear();
		}

		[Test]
		public void SaveGlobal_WritesGlobalParticipantsToReservedSlot()
		{
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var controller = CreateController(progress, settings);
			IReadOnlyDictionary<string, string> capturedData = null;
			var capturedInfo = default(SaveSlotInfo);
			storage.Save(Arg.Do<SaveSlotInfo>(info => capturedInfo = info), Arg.Do<IReadOnlyDictionary<string, string>>(data => capturedData = data))
				   .Returns(true);

			var result = controller.SaveGlobal();

			result.IsSuccess.Should().BeTrue();
			capturedInfo.SlotId.Should().Be(SaveSlot.GlobalId);
			capturedData.Should().BeEquivalentTo(new Dictionary<string, string> { ["settings"] = "settings-data" });
			progress.DidNotReceive().Export();
		}

		[Test]
		public void LoadGlobal_WithExistingData_RestoresWithoutPublishingEvent()
		{
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var progress = CreateParticipant("progress", 0, false, "progress-data");
			var controller = CreateController(settings, progress);
			storage.Load(SaveSlot.GlobalId).Returns(new Dictionary<string, string> { ["settings"] = "stored" });

			var result = controller.LoadGlobal();

			result.IsSuccess.Should().BeTrue();
			Received.InOrder(() =>
				{
					settings.Clear();
					settings.Import("stored");
				}
			);
			progress.DidNotReceive().Clear();
			publisher.DidNotReceive().Publish(Arg.Any<GameLoaded>());
		}

		[Test]
		public void LoadGlobal_WithoutData_SucceedsWithoutClearing()
		{
			var settings = CreateParticipant("settings", 0, true, "settings-data");
			var controller = CreateController(settings);
			storage.Load(SaveSlot.GlobalId).Returns((IReadOnlyDictionary<string, string>)null);

			var result = controller.LoadGlobal();

			result.IsSuccess.Should().BeTrue();
			settings.DidNotReceive().Clear();
			settings.DidNotReceive().Import(Arg.Any<string>());
		}

		[Test]
		public void GetSlots_ExcludesGlobalSlot()
		{
			var controller = CreateController();
			storage.ListSlots()
				   .Returns(
					   new List<SaveSlotInfo>
					   {
						   new("slot-1", new(2026, 8, 17, 12, 0, 0), "第一章"),
						   new(SaveSlot.GlobalId, new(2026, 8, 17, 12, 0, 0), string.Empty)
					   }
				   );

			var slots = controller.GetSlots();

			slots.Should().BeEquivalentTo(new[] { new { SlotId = "slot-1", Description = "第一章" } });
		}

		private SaveController CreateController(params ISaveParticipant[] participants)
		{
			return new(new List<ISaveParticipant>(participants), storage, publisher);
		}

		private static ISaveParticipant CreateParticipant(string saveKey, int loadOrder, bool isGlobal, string exported)
		{
			var participant = Substitute.For<ISaveParticipant>();
			participant.SaveKey.Returns(saveKey);
			participant.LoadOrder.Returns(loadOrder);
			participant.IsGlobal.Returns(isGlobal);
			participant.Export().Returns(exported);
			return participant;
		}
	}
}