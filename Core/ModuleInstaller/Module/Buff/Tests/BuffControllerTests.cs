using System.Collections.Generic;
using NUnit.Framework;
using Rino.GameFramework.AttributeSystem;
using Rino.GameFramework.DDDCore;
using UniRx;
using Zenject;

namespace Rino.GameFramework.BuffSystem.Tests
{
    [TestFixture]
    public class BuffControllerTests : ZenjectUnitTestFixture
    {
        private BuffRepository repository;
        private BuffController controller;
        private List<BuffApplied> appliedEvents;
        private List<BuffRemoved> removedEvents;
        private List<BuffStackChanged> stackChangedEvents;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // 安裝 DDDCore
            DDDCoreInstaller.Install(Container);

            // 建立 Repository 和 Controller
            repository = new BuffRepository();
            var publisher = Container.Resolve<Publisher>();
            controller = new BuffController(repository, publisher);

            // 註冊配置
            var configs = new List<BuffConfig>
            {
                new()
                {
                    BuffName = "Poison",
                    Duration = 10f,
                    Turns = null,
                    StackBehavior = StackBehavior.IncreaseStack,
                    MaxStack = 5,
                    MutualExclusionGroup = null,
                    Priority = 0,
                    Effects = new List<BuffEffectConfig>
                    {
                        new() { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = -5 }
                    }
                },
                new()
                {
                    BuffName = "Burn",
                    Duration = 5f,
                    Turns = null,
                    StackBehavior = StackBehavior.RefreshDuration,
                    MaxStack = null,
                    MutualExclusionGroup = null,
                    Priority = 0,
                    Effects = new List<BuffEffectConfig>
                    {
                        new() { AttributeName = "Defense", ModifyType = ModifyType.Percent, Value = -20 }
                    }
                },
                new()
                {
                    BuffName = "SpeedUp",
                    Duration = 8f,
                    Turns = null,
                    StackBehavior = StackBehavior.RefreshDuration,
                    MaxStack = null,
                    MutualExclusionGroup = "Movement",
                    Priority = 1,
                    Effects = new List<BuffEffectConfig>
                    {
                        new() { AttributeName = "Speed", ModifyType = ModifyType.Percent, Value = 30 }
                    }
                },
                new()
                {
                    BuffName = "SpeedDown",
                    Duration = 8f,
                    Turns = null,
                    StackBehavior = StackBehavior.RefreshDuration,
                    MaxStack = null,
                    MutualExclusionGroup = "Movement",
                    Priority = 1,
                    Effects = new List<BuffEffectConfig>
                    {
                        new() { AttributeName = "Speed", ModifyType = ModifyType.Percent, Value = -30 }
                    }
                },
                new()
                {
                    BuffName = "Invincible",
                    Duration = null,
                    Turns = 2,
                    StackBehavior = StackBehavior.Replace,
                    MaxStack = null,
                    MutualExclusionGroup = null,
                    Priority = 0,
                    Effects = new List<BuffEffectConfig>
                    {
                        new() { AttributeName = "InvincibleCount", ModifyType = ModifyType.Flat, Value = 1 }
                    }
                },
                new()
                {
                    BuffName = "Independent",
                    Duration = 5f,
                    Turns = null,
                    StackBehavior = StackBehavior.Independent,
                    MaxStack = null,
                    MutualExclusionGroup = null,
                    Priority = 0,
                    Effects = new List<BuffEffectConfig>()
                }
            };
            controller.RegisterConfigs(configs);

            // 訂閱事件
            appliedEvents = new List<BuffApplied>();
            removedEvents = new List<BuffRemoved>();
            stackChangedEvents = new List<BuffStackChanged>();

            var subscriber = Container.Resolve<Subscriber>();
            subscriber.Subscribe<BuffApplied>(e => appliedEvents.Add(e));
            subscriber.Subscribe<BuffRemoved>(e => removedEvents.Add(e));
            subscriber.Subscribe<BuffStackChanged>(e => stackChangedEvents.Add(e));
        }

        #region AddBuff Tests

        [Test]
        public void AddBuff_WithValidParameters_CreatesBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

            Assert.IsNotNull(buffId);
            var buff = controller.GetBuff(buffId);
            Assert.AreEqual("Poison", buff.BuffName);
            Assert.AreEqual("owner-1", buff.OwnerId);
            Assert.AreEqual("source-1", buff.SourceId);
        }

        [Test]
        public void AddBuff_PublishesBuffAppliedEvent()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

            Assert.AreEqual(1, appliedEvents.Count);
            Assert.AreEqual(buffId, appliedEvents[0].BuffId);
            Assert.AreEqual("owner-1", appliedEvents[0].OwnerId);
            Assert.AreEqual("Poison", appliedEvents[0].BuffName);
            Assert.AreEqual("source-1", appliedEvents[0].SourceId);
        }

        [Test]
        public void AddBuff_WithIncreaseStack_IncreasesStackCount()
        {
            var buffId1 = controller.AddBuff("owner-1", "Poison", "source-1");
            var buffId2 = controller.AddBuff("owner-1", "Poison", "source-2");

            Assert.AreEqual(buffId1, buffId2);
            var buff = controller.GetBuff(buffId1);
            Assert.AreEqual(2, buff.StackCount);
        }

        [Test]
        public void AddBuff_WithRefreshDuration_RefreshesDuration()
        {
            var buffId = controller.AddBuff("owner-1", "Burn", "source-1");
            var buff = controller.GetBuff(buffId);
            buff.AdjustDuration(3f);

            controller.AddBuff("owner-1", "Burn", "source-2");

            Assert.AreEqual(5f, buff.RemainingDuration);
        }

        [Test]
        public void AddBuff_WithReplace_ReplacesExistingBuff()
        {
            var buffId1 = controller.AddBuff("owner-1", "Invincible", "source-1");
            var buffId2 = controller.AddBuff("owner-1", "Invincible", "source-2");

            Assert.AreNotEqual(buffId1, buffId2);
            Assert.IsNull(controller.GetBuff(buffId1));
            Assert.IsNotNull(controller.GetBuff(buffId2));
        }

        [Test]
        public void AddBuff_WithIndependent_CreatesMultipleBuffs()
        {
            var buffId1 = controller.AddBuff("owner-1", "Independent", "source-1");
            var buffId2 = controller.AddBuff("owner-1", "Independent", "source-2");

            Assert.AreNotEqual(buffId1, buffId2);
            Assert.IsNotNull(controller.GetBuff(buffId1));
            Assert.IsNotNull(controller.GetBuff(buffId2));
            Assert.AreEqual(2, controller.GetBuffsByOwner("owner-1").Count);
        }

        #endregion

        #region Mutual Exclusion Tests

        [Test]
        public void AddBuff_WithMutualExclusion_RemovesConflictingBuff()
        {
            var buffId1 = controller.AddBuff("owner-1", "SpeedDown", "source-1");
            var buffId2 = controller.AddBuff("owner-1", "SpeedUp", "source-2");

            Assert.IsNull(controller.GetBuff(buffId1));
            Assert.IsNotNull(controller.GetBuff(buffId2));
        }

        [Test]
        public void AddBuff_WithSameMutualExclusionSameName_DoesNotRemove()
        {
            var buffId1 = controller.AddBuff("owner-1", "SpeedUp", "source-1");
            var buffId2 = controller.AddBuff("owner-1", "SpeedUp", "source-2");

            Assert.AreEqual(buffId1, buffId2);
            Assert.IsNotNull(controller.GetBuff(buffId1));
        }

        #endregion

        #region RemoveBuff Tests

        [Test]
        public void RemoveBuff_WithExistingBuff_RemovesBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

            controller.RemoveBuff(buffId);

            Assert.IsNull(controller.GetBuff(buffId));
        }

        [Test]
        public void RemoveBuff_PublishesBuffRemovedEvent()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

            controller.RemoveBuff(buffId);

            Assert.AreEqual(1, removedEvents.Count);
            Assert.AreEqual(buffId, removedEvents[0].BuffId);
            Assert.AreEqual("owner-1", removedEvents[0].OwnerId);
            Assert.AreEqual("Poison", removedEvents[0].BuffName);
            Assert.AreEqual("Manual", removedEvents[0].Reason);
        }

        [Test]
        public void RemoveBuff_WithNonExistingBuff_DoesNothing()
        {
            controller.RemoveBuff("non-existing");

            Assert.AreEqual(0, removedEvents.Count);
        }

        #endregion

        #region RemoveBuffsBySource Tests

        [Test]
        public void RemoveBuffsBySource_RemovesAllBuffsFromSource()
        {
            controller.AddBuff("owner-1", "Poison", "source-1");
            controller.AddBuff("owner-1", "Burn", "source-1");
            controller.AddBuff("owner-1", "Independent", "source-2");

            controller.RemoveBuffsBySource("owner-1", "source-1");

            Assert.AreEqual(1, controller.GetBuffsByOwner("owner-1").Count);
            Assert.AreEqual(2, removedEvents.Count);
        }

        #endregion

        #region RemoveBuffsByName Tests

        [Test]
        public void RemoveBuffsByName_RemovesAllBuffsWithName()
        {
            controller.AddBuff("owner-1", "Independent", "source-1");
            controller.AddBuff("owner-1", "Independent", "source-2");
            controller.AddBuff("owner-1", "Poison", "source-3");

            controller.RemoveBuffsByName("owner-1", "Independent");

            Assert.AreEqual(1, controller.GetBuffsByOwner("owner-1").Count);
            Assert.AreEqual(2, removedEvents.Count);
        }

        #endregion

        #region TickTime Tests

        [Test]
        public void TickTime_WithExpiredBuff_RemovesBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

            controller.TickTime(6f);

            Assert.IsNull(controller.GetBuff(buffId));
            Assert.AreEqual(1, removedEvents.Count);
            Assert.AreEqual("Expired", removedEvents[0].Reason);
        }

        [Test]
        public void TickTime_WithNonExpiredBuff_KeepsBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

            controller.TickTime(3f);

            Assert.IsNotNull(controller.GetBuff(buffId));
            Assert.AreEqual(2f, controller.GetBuff(buffId).RemainingDuration);
        }

        #endregion

        #region TickTurn Tests

        [Test]
        public void TickTurn_WithExpiredBuff_RemovesBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Invincible", "source-1");

            controller.TickTurn("owner-1");
            controller.TickTurn("owner-1");

            Assert.IsNull(controller.GetBuff(buffId));
            Assert.AreEqual(1, removedEvents.Count);
            Assert.AreEqual("Expired", removedEvents[0].Reason);
        }

        #endregion

        #region ObserveBuffs Tests

        [Test]
        public void ObserveBuffs_NotifiesOnBuffAdded()
        {
            List<BuffInfo> receivedBuffs = null;
            controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

            controller.AddBuff("owner-1", "Poison", "source-1");

            Assert.IsNotNull(receivedBuffs);
            Assert.AreEqual(1, receivedBuffs.Count);
            Assert.AreEqual("Poison", receivedBuffs[0].BuffName);
        }

        [Test]
        public void ObserveBuffs_NotifiesOnBuffRemoved()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
            List<BuffInfo> receivedBuffs = null;
            controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

            controller.RemoveBuff(buffId);

            Assert.IsNotNull(receivedBuffs);
            Assert.IsEmpty(receivedBuffs);
        }

        #endregion

        #region RecordModifier Tests

        [Test]
        public void RecordModifier_AddsRecordToBuff()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

            controller.RecordModifier(buffId, "Health", "mod-1");

            var buff = controller.GetBuff(buffId);
            Assert.AreEqual(1, buff.ModifierRecords.Count);
            Assert.AreEqual("Health", buff.ModifierRecords[0].AttributeName);
            Assert.AreEqual("mod-1", buff.ModifierRecords[0].ModifierId);
        }

        [Test]
        public void RemoveLastModifierRecord_RemovesAndReturnsRecord()
        {
            var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
            controller.RecordModifier(buffId, "Health", "mod-1");

            var record = controller.RemoveLastModifierRecord(buffId);

            Assert.AreEqual("Health", record.AttributeName);
            Assert.AreEqual("mod-1", record.ModifierId);
            Assert.IsEmpty(controller.GetBuff(buffId).ModifierRecords);
        }

        #endregion

        #region StackChanged Event Tests

        [Test]
        public void AddBuff_WithIncreaseStack_PublishesStackChangedEvent()
        {
            controller.AddBuff("owner-1", "Poison", "source-1");
            controller.AddBuff("owner-1", "Poison", "source-2");

            Assert.AreEqual(1, stackChangedEvents.Count);
            Assert.AreEqual(1, stackChangedEvents[0].OldStack);
            Assert.AreEqual(2, stackChangedEvents[0].NewStack);
        }

        #endregion
    }
}
