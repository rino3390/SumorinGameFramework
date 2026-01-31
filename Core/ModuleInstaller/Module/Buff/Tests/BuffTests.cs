using System;
using FluentAssertions;
using NUnit.Framework;
using UniRx;

namespace Rino.GameFramework.BuffSystem.Tests
{
	[TestFixture]
	public class BuffTests
	{
	#region Constructor Tests
		[Test]
		public void Constructor_WithTimeBased_SetsAllProperties()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 5, LifetimeType.TimeBased, 10f);

			Assert.AreEqual("buff-1", buff.Id);
			Assert.AreEqual("Poison", buff.BuffName);
			Assert.AreEqual("owner-1", buff.OwnerId);
			Assert.AreEqual("source-1", buff.SourceId);
			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(5, buff.MaxStack);
			Assert.AreEqual(LifetimeType.TimeBased, buff.LifetimeType);
			Assert.AreEqual(10f, buff.RemainingLifetime);
			Assert.IsEmpty(buff.ModifierRecords);
			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void Constructor_WithTurnBased_SetsAllProperties()
		{
			var buff = new Buff("buff-1", "Shield", "owner-1", "source-1", -1, LifetimeType.TurnBased, 3f);

			Assert.AreEqual(LifetimeType.TurnBased, buff.LifetimeType);
			Assert.AreEqual(3f, buff.RemainingLifetime);
			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void Constructor_WithPermanent_SetsLifetimeTypeOnly()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			Assert.AreEqual(LifetimeType.Permanent, buff.LifetimeType);
			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void Constructor_WithNegativeOneMaxStack_MeansUnlimited()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.TimeBased, 10f);

			Assert.AreEqual(-1, buff.MaxStack);
		}

		[TestCase(null, "Poison", "owner-1", "source-1", typeof(ArgumentNullException), "id", TestName = "Null id throws ArgumentNullException")]
		[TestCase("", "Poison", "owner-1", "source-1", typeof(ArgumentException), "id", TestName = "Empty id throws ArgumentException")]
		[TestCase("buff-1", null, "owner-1", "source-1", typeof(ArgumentException), "buffName", TestName = "Null buffName throws ArgumentException")]
		[TestCase("buff-1", "", "owner-1", "source-1", typeof(ArgumentException), "buffName", TestName = "Empty buffName throws ArgumentException")]
		[TestCase("buff-1", "Poison", null, "source-1", typeof(ArgumentException), "ownerId", TestName = "Null ownerId throws ArgumentException")]
		[TestCase("buff-1", "Poison", "", "source-1", typeof(ArgumentException), "ownerId", TestName = "Empty ownerId throws ArgumentException")]
		[TestCase("buff-1", "Poison", "owner-1", null, typeof(ArgumentException), "sourceId", TestName = "Null sourceId throws ArgumentException")]
		[TestCase("buff-1", "Poison", "owner-1", "", typeof(ArgumentException), "sourceId", TestName = "Empty sourceId throws ArgumentException")]
		public void Constructor_WithInvalidStringParameter_ThrowsException(string id, string buffName, string ownerId, string sourceId, Type exceptionType,
																		   string paramName)
		{
			Assert.That(
				() => new Buff(id, buffName, ownerId, sourceId, -1, LifetimeType.Permanent, 0f), Throws.TypeOf(exceptionType).With.Property("ParamName").EqualTo(paramName)
			);
		}
	#endregion

	#region ChangeStack Tests
		[TestCase(-1, 1, 2, TestName = "Positive delta increases stack")]
		[TestCase(-1, 3, 4, TestName = "Positive delta increases stack by count")]
		[TestCase(3, 5, 3, TestName = "Positive delta clamps to max stack")]
		[TestCase(-1, -1, 0, TestName = "Negative delta decreases stack")]
		[TestCase(-1, -5, 0, TestName = "Negative delta clamps to zero")]
		public void ChangeStack_UpdatesStackCount(int maxStack, int delta, int expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", maxStack, LifetimeType.Permanent, 0f);

			buff.ChangeStack(delta);

			Assert.AreEqual(expected, buff.StackCount);
		}

		[TestCase(1, 1, 2, TestName = "Increase triggers event")]
		[TestCase(-1, 2, 1, TestName = "Decrease triggers event")]
		public void ChangeStack_TriggersOnStackChangedEvent(int delta, int expectedOld, int expectedNew)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
			if (expectedOld > 1) buff.ChangeStack(expectedOld - 1);
			BuffStackChangedInfo? receivedInfo = null;
			buff.OnStackChanged.Subscribe(info => receivedInfo = info);

			buff.ChangeStack(delta);

			receivedInfo.Should().NotBeNull();
			receivedInfo!.Value.Should().BeEquivalentTo(new BuffStackChangedInfo("buff-1", "owner-1", "Poison", expectedOld, expectedNew));
		}

		[TestCase(1, 0, TestName = "At max stack with positive delta")]
		[TestCase(-1, 0, TestName = "Zero delta")]
		public void ChangeStack_WithNoChange_DoesNotTriggerEvent(int maxStack, int delta)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", maxStack, LifetimeType.Permanent, 0f);
			var eventTriggered = false;
			buff.OnStackChanged.Subscribe(_ => eventTriggered = true);

			buff.ChangeStack(delta);

			Assert.IsFalse(eventTriggered);
		}
	#endregion

	#region RefreshLifetime Tests
		[Test]
		public void RefreshLifetime_UpdatesRemainingLifetime()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.TimeBased, 5f);

			buff.RefreshLifetime(10f);

			Assert.AreEqual(10f, buff.RemainingLifetime);
		}

		[Test]
		public void RefreshLifetime_WithPermanent_DoesNothing()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			buff.RefreshLifetime(10f);

			Assert.AreEqual(0f, buff.RemainingLifetime);
		}
	#endregion

	#region AdjustLifetime Tests
		[TestCase(3f, 13f, TestName = "Positive delta increases lifetime")]
		[TestCase(-3f, 7f, TestName = "Negative delta decreases lifetime")]
		public void AdjustLifetime_UpdatesRemainingLifetime(float delta, float expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.TimeBased, 10f);

			buff.AdjustLifetime(delta);

			Assert.AreEqual(expected, buff.RemainingLifetime);
		}

		[Test]
		public void AdjustLifetime_WithPermanent_DoesNothing()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			buff.AdjustLifetime(3f);

			Assert.AreEqual(0f, buff.RemainingLifetime);
		}

		[Test]
		public void AdjustLifetime_WithTurnBased_UpdatesLifetime()
		{
			var buff = new Buff("buff-1", "Shield", "owner-1", "source-1", -1, LifetimeType.TurnBased, 5f);

			buff.AdjustLifetime(-2f);

			Assert.AreEqual(3f, buff.RemainingLifetime);
		}
	#endregion

	#region IsExpired Tests
		[TestCase(LifetimeType.TimeBased, 0f, true, TestName = "TimeBased zero returns true")]
		[TestCase(LifetimeType.TimeBased, -1f, true, TestName = "TimeBased negative returns true")]
		[TestCase(LifetimeType.TurnBased, 0f, true, TestName = "TurnBased zero returns true")]
		[TestCase(LifetimeType.TurnBased, -1f, true, TestName = "TurnBased negative returns true")]
		[TestCase(LifetimeType.TimeBased, 10f, false, TestName = "TimeBased positive returns false")]
		[TestCase(LifetimeType.TurnBased, 5f, false, TestName = "TurnBased positive returns false")]
		[TestCase(LifetimeType.Permanent, 0f, false, TestName = "Permanent with zero returns false")]
		[TestCase(LifetimeType.Permanent, -1f, false, TestName = "Permanent with negative returns false")]
		public void IsExpired_ReturnsExpectedResult(LifetimeType type, float lifetime, bool expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, type, lifetime);

			Assert.AreEqual(expected, buff.IsExpired);
		}

		[Test]
		public void IsExpired_WithStackCountZero_ReturnsTrue()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			buff.ChangeStack(-1);

			Assert.IsTrue(buff.IsExpired);
		}
	#endregion

	#region ModifierRecord Tests
		[Test]
		public void RecordModifier_AddsRecordToList()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			buff.RecordModifier("Health", "mod-1");

			buff.ModifierRecords.Should().BeEquivalentTo(new[] { new ModifierRecord("Health", "mod-1") });
		}

		[Test]
		public void RemoveLastModifierRecord_RemovesAndReturnsLastRecord()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
			buff.RecordModifier("Health", "mod-1");
			buff.RecordModifier("Defense", "mod-2");

			var removed = buff.RemoveLastModifierRecord();

			removed.Should().BeEquivalentTo(new ModifierRecord("Defense", "mod-2"));
			buff.ModifierRecords.Should().BeEquivalentTo(new[] { new ModifierRecord("Health", "mod-1") });
		}

		[Test]
		public void RemoveLastModifierRecord_WithEmptyList_ReturnsNull()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

			var removed = buff.RemoveLastModifierRecord();

			Assert.IsNull(removed);
		}
	#endregion

	#region Edge Cases
		[Test]
		public void AdjustLifetime_WithLargePositiveDelta_IncreasesLifetime()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.TimeBased, 10f);

			buff.AdjustLifetime(float.MaxValue);

			Assert.IsTrue(buff.RemainingLifetime > 0);
		}

		[Test]
		public void AdjustLifetime_WithNegativeInfinity_SetsToNegativeInfinity()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.TimeBased, 10f);

			buff.AdjustLifetime(float.NegativeInfinity);

			Assert.AreEqual(float.NegativeInfinity, buff.RemainingLifetime);
		}
	#endregion
	}
}
