using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace Sumorin.Save.Tests
{
	[TestFixture]
	public class LocalFileSaveStorageTests
	{
		private static readonly DateTime SavedAt = new(2026, 1, 2, 3, 4, 5);

		private string root;
		private LocalFileSaveStorage storage;

		[SetUp]
		public void Setup()
		{
			root = Path.Combine(Path.GetTempPath(), "SumorinSaveTests", Path.GetRandomFileName());
			storage = new(new NewtonsoftSaveSerializer(), root);
		}

		[TearDown]
		public void Teardown()
		{
			if(Directory.Exists(root))
			{
				Directory.Delete(root, true);
			}
		}

		[Test]
		public void Save_WithSlotData_WritesDataAndMetaIntoSlotFolder()
		{
			var result = storage.Save(Info("slot-1"), Payload());

			result.Should().BeTrue();
			var directory = Path.Combine(root, "Saves", "slot-1");
			File.Exists(Path.Combine(directory, "data.json")).Should().BeTrue();
			File.Exists(Path.Combine(directory, "meta.json")).Should().BeTrue();
		}

		[TestCase("bad/slot", TestName = "含路徑分隔字元回傳失敗")]
		[TestCase("..", TestName = "上層目錄回傳失敗")]
		[TestCase("", TestName = "空字串回傳失敗")]
		public void Save_WithInvalidSlotId_ReturnsFalse(string slotId)
		{
			var result = storage.Save(Info(slotId), Payload());

			result.Should().BeFalse();
			Directory.Exists(Path.Combine(root, "Saves")).Should().BeFalse();
		}

		[Test]
		public void Load_AfterSave_ReturnsTheSameData()
		{
			storage.Save(Info("slot-1"), Payload());

			var result = storage.Load("slot-1");

			result.Should().BeEquivalentTo(Payload());
		}

		[Test]
		public void Load_AfterSaveGlobalSlot_ReturnsTheSameData()
		{
			storage.Save(Info(SaveSlot.GlobalId), Payload());

			var result = storage.Load(SaveSlot.GlobalId);

			result.Should().BeEquivalentTo(Payload());
		}

		[Test]
		public void Load_WithMissingSlot_ReturnsNull()
		{
			storage.Load("slot-1").Should().BeNull();
		}

		[Test]
		public void Delete_WithExistingSlot_RemovesSlotFolderAndReturnsTrue()
		{
			storage.Save(Info("slot-1"), Payload());

			var result = storage.Delete("slot-1");

			result.Should().BeTrue();
			Directory.Exists(Path.Combine(root, "Saves", "slot-1")).Should().BeFalse();
		}

		[Test]
		public void Delete_WithMissingSlot_ReturnsFalse()
		{
			storage.Delete("slot-1").Should().BeFalse();
		}

		[Test]
		public void ListSlots_WithSavedSlots_ReturnsTheirMetadata()
		{
			storage.Save(Info("slot-1"), Payload());
			storage.Save(Info("slot-2"), Payload());

			var result = storage.ListSlots();

			result.Should()
				  .BeEquivalentTo(
					  new[]
					  {
						  new
						  {
							  SlotId = "slot-1",
							  SavedAt,
							  Description = "slot-1 的存檔"
						  },
						  new
						  {
							  SlotId = "slot-2",
							  SavedAt,
							  Description = "slot-2 的存檔"
						  }
					  }
				  );
		}

		[Test]
		public void ListSlots_WithFolderMissingMeta_SkipsThatFolder()
		{
			storage.Save(Info("slot-1"), Payload());
			Directory.CreateDirectory(Path.Combine(root, "Saves", "orphan"));

			var result = storage.ListSlots();

			result.Should().BeEquivalentTo(new[] { new { SlotId = "slot-1" } });
		}

		[Test]
		public void ListSlots_WithUnreadableMeta_SkipsThatFolder()
		{
			storage.Save(Info("slot-1"), Payload());
			var broken = Directory.CreateDirectory(Path.Combine(root, "Saves", "broken"));
			File.WriteAllText(Path.Combine(broken.FullName, "meta.json"), "not json");

			var result = storage.ListSlots();

			result.Should().BeEquivalentTo(new[] { new { SlotId = "slot-1" } });
		}

		[Test]
		public void ListSlots_WithGlobalSlotSaved_DoesNotIncludeIt()
		{
			storage.Save(Info(SaveSlot.GlobalId), Payload());

			storage.ListSlots().Should().BeEmpty();
		}

		[Test]
		public void ListSlots_WithoutAnySave_ReturnsEmpty()
		{
			storage.ListSlots().Should().BeEmpty();
		}

		private static Dictionary<string, string> Payload()
		{
			return new()
			{
				["attributes"] = "[{\"Id\":\"a-1\"}]",
				["settings"] = "{\"Volume\":0.5}"
			};
		}

		private static SaveSlotInfo Info(string slotId) => new(slotId, SavedAt, $"{slotId} 的存檔");
	}
}