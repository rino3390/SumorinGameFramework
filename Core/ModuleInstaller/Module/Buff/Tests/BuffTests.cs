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

        [Test]
        public void Constructor_WithNullId_ThrowsArgumentNullException()
        {
            Assert.That(() => new Buff(null, "Poison", "owner-1", "source-1", null, null, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("id"));
        }

        [Test]
        public void Constructor_WithEmptyId_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("", "Poison", "owner-1", "source-1", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("id"));
        }

        [Test]
        public void Constructor_WithNullBuffName_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", null, "owner-1", "source-1", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("buffName"));
        }

        [Test]
        public void Constructor_WithEmptyBuffName_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", "", "owner-1", "source-1", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("buffName"));
        }

        [Test]
        public void Constructor_WithNullOwnerId_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", "Poison", null, "source-1", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("ownerId"));
        }

        [Test]
        public void Constructor_WithEmptyOwnerId_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", "Poison", "", "source-1", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("ownerId"));
        }

        [Test]
        public void Constructor_WithNullSourceId_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", "Poison", "owner-1", null, null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("sourceId"));
        }

        [Test]
        public void Constructor_WithEmptySourceId_ThrowsArgumentException()
        {
            Assert.That(() => new Buff("buff-1", "Poison", "owner-1", "", null, null, null),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("sourceId"));
        }

        #endregion

        #region CanAddStack Tests

        [Test]
        public void CanAddStack_WithNoMaxStack_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            Assert.IsTrue(buff.CanAddStack());
        }

        [Test]
        public void CanAddStack_WithStackBelowMax_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 5, null, null);

            Assert.IsTrue(buff.CanAddStack());
        }

        [Test]
        public void CanAddStack_WithStackAtMax_ReturnsFalse()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 1, null, null);

            Assert.IsFalse(buff.CanAddStack());
        }

        #endregion

        #region AddStack Tests

        [Test]
        public void AddStack_WithDefaultCount_IncreasesStackByOne()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.AddStack();

            Assert.AreEqual(2, buff.StackCount);
        }

        [Test]
        public void AddStack_WithSpecificCount_IncreasesStackByCount()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.AddStack(3);

            Assert.AreEqual(4, buff.StackCount);
        }

        [Test]
        public void AddStack_WithMaxStack_ClampsToMaxStack()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 3, null, null);

            buff.AddStack(5);

            Assert.AreEqual(3, buff.StackCount);
        }

        [Test]
        public void AddStack_TriggersOnStackChangedEvent()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            BuffStackChangedInfo? receivedInfo = null;
            buff.OnStackChanged.Subscribe(info => receivedInfo = info);

            buff.AddStack();

            Assert.IsNotNull(receivedInfo);
            Assert.AreEqual("buff-1", receivedInfo.Value.BuffId);
            Assert.AreEqual("owner-1", receivedInfo.Value.OwnerId);
            Assert.AreEqual("Poison", receivedInfo.Value.BuffName);
            Assert.AreEqual(1, receivedInfo.Value.OldStack);
            Assert.AreEqual(2, receivedInfo.Value.NewStack);
        }

        [Test]
        public void AddStack_WithNoChange_DoesNotTriggerEvent()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", 1, null, null);
            var eventTriggered = false;
            buff.OnStackChanged.Subscribe(_ => eventTriggered = true);

            buff.AddStack();

            Assert.IsFalse(eventTriggered);
        }

        #endregion

        #region RemoveStack Tests

        [Test]
        public void RemoveStack_WithDefaultCount_DecreasesStackByOne()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            buff.AddStack(2);

            buff.RemoveStack();

            Assert.AreEqual(2, buff.StackCount);
        }

        [Test]
        public void RemoveStack_WithSpecificCount_DecreasesStackByCount()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            buff.AddStack(4);

            buff.RemoveStack(2);

            Assert.AreEqual(3, buff.StackCount);
        }

        [Test]
        public void RemoveStack_ClampsToZero()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.RemoveStack(5);

            Assert.AreEqual(0, buff.StackCount);
        }

        [Test]
        public void RemoveStack_TriggersOnStackChangedEvent()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            buff.AddStack();
            BuffStackChangedInfo? receivedInfo = null;
            buff.OnStackChanged.Subscribe(info => receivedInfo = info);

            buff.RemoveStack();

            Assert.IsNotNull(receivedInfo);
            Assert.AreEqual(2, receivedInfo.Value.OldStack);
            Assert.AreEqual(1, receivedInfo.Value.NewStack);
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

        [Test]
        public void RefreshDuration_TriggersOnDurationRefreshedEvent()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 5f, null);
            BuffDurationRefreshedInfo? receivedInfo = null;
            buff.OnDurationRefreshed.Subscribe(info => receivedInfo = info);

            buff.RefreshDuration(10f);

            Assert.IsNotNull(receivedInfo);
            Assert.AreEqual("buff-1", receivedInfo.Value.BuffId);
            Assert.AreEqual("owner-1", receivedInfo.Value.OwnerId);
            Assert.AreEqual("Poison", receivedInfo.Value.BuffName);
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

        #region TickTime Tests

        [Test]
        public void TickTime_WithDuration_DecreasesDuration()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

            buff.TickTime(3f);

            Assert.AreEqual(7f, buff.RemainingDuration);
        }

        [Test]
        public void TickTime_WithNullDuration_DoesNothing()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.TickTime(3f);

            Assert.IsNull(buff.RemainingDuration);
        }

        #endregion

        #region TickTurn Tests

        [Test]
        public void TickTurn_WithTurns_DecreasesTurns()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 5);

            buff.TickTurn();

            Assert.AreEqual(4, buff.RemainingTurns);
        }

        [Test]
        public void TickTurn_WithSpecificCount_DecreasesTurnsByCount()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 5);

            buff.TickTurn(2);

            Assert.AreEqual(3, buff.RemainingTurns);
        }

        [Test]
        public void TickTurn_WithNullTurns_DoesNothing()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.TickTurn();

            Assert.IsNull(buff.RemainingTurns);
        }

        #endregion

        #region IsExpired Tests

        [Test]
        public void IsExpired_WithDurationZero_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 0f, null);

            Assert.IsTrue(buff.IsExpired);
        }

        [Test]
        public void IsExpired_WithDurationNegative_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, -1f, null);

            Assert.IsTrue(buff.IsExpired);
        }

        [Test]
        public void IsExpired_WithTurnsZero_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, 0);

            Assert.IsTrue(buff.IsExpired);
        }

        [Test]
        public void IsExpired_WithTurnsNegative_ReturnsTrue()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, -1);

            Assert.IsTrue(buff.IsExpired);
        }

        [Test]
        public void IsExpired_WithValidDurationAndTurns_ReturnsFalse()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, 5);

            Assert.IsFalse(buff.IsExpired);
        }

        [Test]
        public void IsExpired_WithNullDurationAndTurns_ReturnsFalse()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            Assert.IsFalse(buff.IsExpired);
        }

        #endregion

        #region ModifierRecord Tests

        [Test]
        public void RecordModifier_AddsRecordToList()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            buff.RecordModifier("Health", "mod-1");

            Assert.AreEqual(1, buff.ModifierRecords.Count);
            Assert.AreEqual("Health", buff.ModifierRecords[0].AttributeName);
            Assert.AreEqual("mod-1", buff.ModifierRecords[0].ModifierId);
        }

        [Test]
        public void RemoveLastModifierRecord_RemovesAndReturnsLastRecord()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            buff.RecordModifier("Health", "mod-1");
            buff.RecordModifier("Defense", "mod-2");

            var removed = buff.RemoveLastModifierRecord();

            Assert.AreEqual("Defense", removed.AttributeName);
            Assert.AreEqual("mod-2", removed.ModifierId);
            Assert.AreEqual(1, buff.ModifierRecords.Count);
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

        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void AddStack_WithExtremeValues_HandlesCorrectly(int count)
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            if (count > 0)
            {
                buff.AddStack(count);
                Assert.AreEqual(1 + count, buff.StackCount);
            }
        }

        [Test]
        public void TickTime_WithLargeNegativeDelta_DoesNotCrash()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

            buff.TickTime(float.MinValue);

            Assert.IsTrue(buff.RemainingDuration > 0);
        }

        [Test]
        public void TickTime_WithInfinity_SetsToNegativeInfinity()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, 10f, null);

            buff.TickTime(float.PositiveInfinity);

            Assert.AreEqual(float.NegativeInfinity, buff.RemainingDuration);
        }

        #endregion
    }
}
