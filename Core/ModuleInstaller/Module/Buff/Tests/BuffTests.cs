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
		public void Constructor_WithValidParameters_SetsAllProperties()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 5, 10f, 3);

			Assert.AreEqual("buff-1", buff.Id);
			Assert.AreEqual("Poison", buff.BuffName);
			Assert.AreEqual("owner-1", buff.OwnerId);
			Assert.AreEqual("source-1", buff.SourceId);
			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(5, buff.MaxStack);
			Assert.AreEqual(10f, buff.RemainingDuration);
			Assert.AreEqual(3, buff.RemainingTurns);
			Assert.IsEmpty(buff.ModifierRecords);
			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void Constructor_WithNullMaxStack_SetsMaxStackToNull()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

			Assert.IsNull(buff.MaxStack);
		}

		[Test]
		public void Constructor_WithNullDuration_SetsDurationToNull()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			Assert.IsNull(buff.RemainingDuration);
			Assert.IsNull(buff.RemainingTurns);
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
				() => new Buff(id, buffName, ownerId, sourceId, null, null, null), Throws.TypeOf(exceptionType).With.Property("ParamName").EqualTo(paramName)
			);
		}
	#endregion

	#region ChangeStack Tests
		[TestCase(null, 1, 2, TestName = "Positive delta increases stack")]
		[TestCase(null, 3, 4, TestName = "Positive delta increases stack by count")]
		[TestCase(3, 5, 3, TestName = "Positive delta clamps to max stack")]
		[TestCase(null, -1, 0, TestName = "Negative delta decreases stack")]
		[TestCase(null, -5, 0, TestName = "Negative delta clamps to zero")]
		public void ChangeStack_UpdatesStackCount(int? maxStack, int delta, int expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", maxStack, null, null);

			buff.ChangeStack(delta);

			Assert.AreEqual(expected, buff.StackCount);
		}

		[TestCase(1, 1, 2, TestName = "Increase triggers event")]
		[TestCase(-1, 2, 1, TestName = "Decrease triggers event")]
		public void ChangeStack_TriggersOnStackChangedEvent(int delta, int expectedOld, int expectedNew)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
			if (expectedOld > 1) buff.ChangeStack(expectedOld - 1);
			BuffStackChangedInfo? receivedInfo = null;
			buff.OnStackChanged.Subscribe(info => receivedInfo = info);

			buff.ChangeStack(delta);

			receivedInfo.Should().NotBeNull();
			receivedInfo!.Value.Should().BeEquivalentTo(new BuffStackChangedInfo("buff-1", "owner-1", "Poison", expectedOld, expectedNew));
		}

		[TestCase(1, 0, TestName = "At max stack with positive delta")]
		[TestCase(null, 0, TestName = "Zero delta")]
		public void ChangeStack_WithNoChange_DoesNotTriggerEvent(int? maxStack, int delta)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", maxStack, null, null);
			var eventTriggered = false;
			buff.OnStackChanged.Subscribe(_ => eventTriggered = true);

			buff.ChangeStack(delta);

			Assert.IsFalse(eventTriggered);
		}
	#endregion

	#region RefreshDuration Tests
		[Test]
		public void RefreshDuration_UpdatesDuration()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 5f, null);

			buff.RefreshDuration(10f);

			Assert.AreEqual(10f, buff.RemainingDuration);
		}

	#endregion

	#region RefreshTurns Tests
		[Test]
		public void RefreshTurns_UpdatesTurns()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 3);

			buff.RefreshTurns(5);

			Assert.AreEqual(5, buff.RemainingTurns);
		}
	#endregion

	#region AdjustDuration Tests
		[TestCase(3f, 7f, TestName = "Positive delta decreases duration")]
		[TestCase(-3f, 13f, TestName = "Negative delta increases duration")]
		public void AdjustDuration_UpdatesDuration(float delta, float expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

			buff.AdjustDuration(delta);

			Assert.AreEqual(expected, buff.RemainingDuration);
		}

		[Test]
		public void AdjustDuration_WithNullDuration_DoesNothing()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			buff.AdjustDuration(3f);

			Assert.IsNull(buff.RemainingDuration);
		}

		[Test]
		public void AdjustDuration_WhenBecomesExpired_TriggersOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 5f, null);
			BuffExpiredInfo? receivedInfo = null;
			buff.OnExpired.Subscribe(info => receivedInfo = info);

			buff.AdjustDuration(5f);

			receivedInfo.Should().NotBeNull();
			receivedInfo!.Value.Should().BeEquivalentTo(new BuffExpiredInfo("buff-1", "owner-1", "Poison"));
		}

		[Test]
		public void AdjustDuration_WhenAlreadyExpired_DoesNotTriggerOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 0f, null);
			var eventTriggered = false;
			buff.OnExpired.Subscribe(_ => eventTriggered = true);

			buff.AdjustDuration(1f);

			Assert.IsFalse(eventTriggered);
		}

		[Test]
		public void AdjustDuration_WhenNotExpired_DoesNotTriggerOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);
			var eventTriggered = false;
			buff.OnExpired.Subscribe(_ => eventTriggered = true);

			buff.AdjustDuration(3f);

			Assert.IsFalse(eventTriggered);
		}
	#endregion

	#region AdjustTurns Tests
		[TestCase(1, 4, TestName = "Positive delta decreases turns")]
		[TestCase(2, 3, TestName = "Positive delta decreases turns by count")]
		[TestCase(-2, 7, TestName = "Negative delta increases turns")]
		public void AdjustTurns_UpdatesTurns(int delta, int expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 5);

			buff.AdjustTurns(delta);

			Assert.AreEqual(expected, buff.RemainingTurns);
		}

		[Test]
		public void AdjustTurns_WithNullTurns_DoesNothing()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			buff.AdjustTurns(1);

			Assert.IsNull(buff.RemainingTurns);
		}

		[Test]
		public void AdjustTurns_WhenBecomesExpired_TriggersOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 1);
			BuffExpiredInfo? receivedInfo = null;
			buff.OnExpired.Subscribe(info => receivedInfo = info);

			buff.AdjustTurns(1);

			receivedInfo.Should().NotBeNull();
			receivedInfo!.Value.Should().BeEquivalentTo(new BuffExpiredInfo("buff-1", "owner-1", "Poison"));
		}

		[Test]
		public void AdjustTurns_WhenAlreadyExpired_DoesNotTriggerOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 0);
			var eventTriggered = false;
			buff.OnExpired.Subscribe(_ => eventTriggered = true);

			buff.AdjustTurns(1);

			Assert.IsFalse(eventTriggered);
		}

		[Test]
		public void AdjustTurns_WhenNotExpired_DoesNotTriggerOnExpiredEvent()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 5);
			var eventTriggered = false;
			buff.OnExpired.Subscribe(_ => eventTriggered = true);

			buff.AdjustTurns(1);

			Assert.IsFalse(eventTriggered);
		}
	#endregion

	#region IsExpired Tests
		[TestCase(0f, null, true, TestName = "Duration zero returns true")]
		[TestCase(-1f, null, true, TestName = "Duration negative returns true")]
		[TestCase(null, 0, true, TestName = "Turns zero returns true")]
		[TestCase(null, -1, true, TestName = "Turns negative returns true")]
		[TestCase(10f, 5, false, TestName = "Valid duration and turns returns false")]
		[TestCase(null, null, false, TestName = "Null duration and turns returns false")]
		public void IsExpired_ReturnsExpectedResult(float? duration, int? turns, bool expected)
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, duration, turns);

			Assert.AreEqual(expected, buff.IsExpired);
		}

		[Test]
		public void IsExpired_WithStackCountZero_ReturnsTrue()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			buff.ChangeStack(-1);

			Assert.IsTrue(buff.IsExpired);
		}
	#endregion

	#region ModifierRecord Tests
		[Test]
		public void RecordModifier_AddsRecordToList()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			buff.RecordModifier("Health", "mod-1");

			buff.ModifierRecords.Should().BeEquivalentTo(new[] { new ModifierRecord("Health", "mod-1") });
		}

		[Test]
		public void RemoveLastModifierRecord_RemovesAndReturnsLastRecord()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
			buff.RecordModifier("Health", "mod-1");
			buff.RecordModifier("Defense", "mod-2");

			var removed = buff.RemoveLastModifierRecord();

			removed.Should().BeEquivalentTo(new ModifierRecord("Defense", "mod-2"));
			buff.ModifierRecords.Should().BeEquivalentTo(new[] { new ModifierRecord("Health", "mod-1") });
		}

		[Test]
		public void RemoveLastModifierRecord_WithEmptyList_ReturnsNull()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

			var removed = buff.RemoveLastModifierRecord();

			Assert.IsNull(removed);
		}
	#endregion

	#region Edge Cases
		[Test]
		public void AdjustDuration_WithLargeNegativeDelta_IncreasesDuration()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

			buff.AdjustDuration(float.MinValue);

			Assert.IsTrue(buff.RemainingDuration > 0);
		}

		[Test]
		public void AdjustDuration_WithInfinity_SetsToNegativeInfinity()
		{
			var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

			buff.AdjustDuration(float.PositiveInfinity);

			Assert.AreEqual(float.NegativeInfinity, buff.RemainingDuration);
		}
	#endregion
	}
}