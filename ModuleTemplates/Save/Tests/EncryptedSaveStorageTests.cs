using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Sumorin.Save.Tests
{
	[TestFixture]
	public class EncryptedSaveStorageTests
	{
		private static readonly DateTime SavedAt = new(2026, 1, 2, 3, 4, 5);

		private FakeSaveStorage inner;
		private EncryptedSaveStorage storage;

		[SetUp]
		public void Setup()
		{
			inner = new();
			storage = new(inner, "passphrase-1");
		}

		[TestCase("plain-ascii", TestName = "純 ASCII 內容往返")]
		[TestCase("中文描述", TestName = "中文內容往返")]
		[TestCase("emoji 🎮", TestName = "emoji 內容往返")]
		public void Load_AfterSave_ReturnsTheSameData(string content)
		{
			var source = new Dictionary<string, string> { ["profile"] = content };

			storage.Save(Info(), source);

			storage.Load("slot-1").Should().BeEquivalentTo(source);
		}

		[Test]
		public void Save_WithTheSameDataTwice_ProducesDifferentCipherText()
		{
			storage.Save(Info(), Payload());
			var first = inner.Stored["profile"];

			storage.Save(Info(), Payload());

			inner.Stored["profile"].Should().NotBe(first);
		}

		[Test]
		public void Load_WithTamperedData_ReturnsNull()
		{
			storage.Save(Info(), Payload());
			inner.Stored = Tamper(inner.Stored);

			storage.Load("slot-1").Should().BeNull();
		}

		[Test]
		public void Load_WithDataFromAnotherPassphrase_ReturnsNull()
		{
			storage.Save(Info(), Payload());

			new EncryptedSaveStorage(inner, "passphrase-2").Load("slot-1").Should().BeNull();
		}

		[Test]
		public void Load_WithMissingSlot_ReturnsNull()
		{
			storage.Load("slot-1").Should().BeNull();
		}

		[Test]
		public void Delete_DelegatesToInnerStorage()
		{
			storage.Delete("slot-1").Should().BeTrue();

			inner.DeletedSlotId.Should().Be("slot-1");
		}

		[Test]
		public void ListSlots_DelegatesToInnerStorage()
		{
			inner.Slots = new[] { Info() };

			storage.ListSlots().Should().BeEquivalentTo(new[] { Info() });
		}

		[TestCase(null, TestName = "通行碼為 null 拋錯")]
		[TestCase("", TestName = "通行碼為空字串拋錯")]
		public void Constructor_WithEmptyPassphrase_Throws(string passphrase)
		{
			Action act = () => _ = new EncryptedSaveStorage(inner, passphrase);

			act.Should().ThrowExactly<ArgumentException>().WithParameterName("passphrase");
		}

		private static SaveSlotInfo Info() => new("slot-1", SavedAt, "slot-1 的存檔");

		private static Dictionary<string, string> Payload()
		{
			return new()
			{
				["profile"] = "{\"PlayerName\":\"Sumorin\",\"Level\":42}"
			};
		}

		private static Dictionary<string, string> Tamper(IReadOnlyDictionary<string, string> data)
		{
			var result = new Dictionary<string, string>();

			foreach(var pair in data)
			{
				var characters = pair.Value.ToCharArray();
				characters[0] = characters[0] == 'A' ? 'B' : 'A';
				result[pair.Key] = new(characters);
			}

			return result;
		}

	#region Nested type: FakeSaveStorage
		// 內層替身，把資料留在記憶體，讓測試看得到實際交出去的內容
		private class FakeSaveStorage: ISaveStorage
		{
			public IReadOnlyDictionary<string, string> Stored { get; set; }

			public IReadOnlyList<SaveSlotInfo> Slots { get; set; } = new List<SaveSlotInfo>();

			public string DeletedSlotId { get; private set; }

		#region ISaveStorage Members
			public bool Save(SaveSlotInfo info, IReadOnlyDictionary<string, string> data)
			{
				Stored = data;

				return true;
			}

			public IReadOnlyDictionary<string, string> Load(string slotId) => Stored;

			public bool Delete(string slotId)
			{
				DeletedSlotId = slotId;

				return true;
			}

			public IReadOnlyList<SaveSlotInfo> ListSlots() => Slots;
		#endregion
		}
	#endregion
	}
}