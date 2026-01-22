using System.Collections.Generic;
using NUnit.Framework;
using UniRx;

namespace Rino.GameFramework.AttributeSystem.Tests
{
    [TestFixture]
    public class AttributeControllerTests
    {
        private AttributeRepository repository;
        private AttributeController controller;

        [SetUp]
        public void SetUp()
        {
            repository = new AttributeRepository();
            controller = new AttributeController(repository);

            var configs = new List<AttributeConfig>
            {
                new() { AttributeName = "Health", Min = 0, Max = 999 },
                new() { AttributeName = "Attack", Min = 0, Max = 999 },
                new() { AttributeName = "MaxHealth", Min = 1, Max = 9999 },
                new() { AttributeName = "Shield", Min = 0, Max = 9999 },
                new() { AttributeName = "MinShield", Min = 0, Max = 9999 },
                new() { AttributeName = "CritRate", Min = 0, Max = 100, Ratio = 100 }
            };
            controller.RegisterConfigs(configs);
        }

        [Test]
        public void GetValue_ExistingAttribute_ReturnsValue()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            var value = controller.GetValue("owner-1", "Health");

            Assert.AreEqual(100, value);
        }

        [Test]
        public void GetValue_NonExistentAttribute_ReturnsZero()
        {
            var value = controller.GetValue("owner-1", "Health");

            Assert.AreEqual(0, value);
        }

        [Test]
        public void SetBaseValue_UpdatesAttributeValue()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            controller.SetBaseValue("owner-1", "Health", 150);

            Assert.AreEqual(150, controller.GetValue("owner-1", "Health"));
        }

        [Test]
        public void SetBaseValue_NotifiesObservers()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

			Assert.AreEqual(100, lastInfo.OldValue);
			Assert.AreEqual(100, lastInfo.NewValue);

            controller.SetBaseValue("owner-1", "Health", 150);

            Assert.AreEqual(100, lastInfo.OldValue);
            Assert.AreEqual(150, lastInfo.NewValue);
        }

        [Test]
        public void SetBaseValue_SameValue_DoesNotNotify()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            int notifyCount = 0;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(_ => notifyCount++);
            // ReactiveProperty 訂閱時會收到初始值，所以 notifyCount = 1

            controller.SetBaseValue("owner-1", "Health", 100);

            // 設定相同值不會再通知，維持 1
            Assert.AreEqual(1, notifyCount);
        }

        [Test]
        public void ObserveAttribute_SubscriptionReceivesCurrentValue()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo receivedInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => receivedInfo = info);

            // ReactiveProperty 訂閱時會立即收到當前值，初始時 OldValue = NewValue
            Assert.AreEqual(100, receivedInfo.NewValue);
            Assert.AreEqual(100, receivedInfo.OldValue);
            Assert.AreEqual("owner-1", receivedInfo.OwnerId);
            Assert.AreEqual("Health", receivedInfo.AttributeName);
            Assert.AreEqual(0, receivedInfo.MinValue);
            Assert.AreEqual(999, receivedInfo.MaxValue);
        }

        [Test]
        public void ObserveAttribute_NonExistentAttribute_ReturnsEmptyObservable()
        {
            var receivedCount = 0;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(_ => receivedCount++);

            Assert.AreEqual(0, receivedCount);
        }

        [Test]
        public void SetMinValue_UpdatesMinValueAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.SetMinValue("owner-1", "Health", 50);

            Assert.AreEqual(50, repository.Get("owner-1", "Health").MinValue);
            Assert.AreEqual(50, lastInfo.MinValue);
        }

        [Test]
        public void SetMaxValue_UpdatesMaxValueAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.SetMaxValue("owner-1", "Health", 500);

            Assert.AreEqual(500, repository.Get("owner-1", "Health").MaxValue);
            Assert.AreEqual(500, lastInfo.MaxValue);
        }

        [Test]
        public void SetMaxValue_ClampsValueAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.SetMaxValue("owner-1", "Health", 50);

            Assert.AreEqual(50, controller.GetValue("owner-1", "Health"));
            Assert.AreEqual(100, lastInfo.OldValue);
            Assert.AreEqual(50, lastInfo.NewValue);
            Assert.AreEqual(50, lastInfo.MaxValue);
        }

        [Test]
        public void SetMinValue_ClampsValueAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.SetMinValue("owner-1", "Health", 150);

            Assert.AreEqual(150, controller.GetValue("owner-1", "Health"));
            Assert.AreEqual(100, lastInfo.OldValue);
            Assert.AreEqual(150, lastInfo.NewValue);
            Assert.AreEqual(150, lastInfo.MinValue);
        }

        [Test]
        public void SetBaseValue_WithRelationMax_UpdatesDependentAttribute()
        {
            var configs = new List<AttributeConfig>
            {
                new() { AttributeName = "Health", Min = 0, Max = 9999, RelationMax = "MaxHealth" },
                new() { AttributeName = "MaxHealth", Min = 1, Max = 9999 }
            };
            controller.RegisterConfigs(configs);

            controller.CreateAttribute("owner-1", "Health", 80);
            controller.CreateAttribute("owner-1", "MaxHealth", 100);
            Assert.AreEqual(100, repository.Get("owner-1", "Health").MaxValue);

            controller.SetBaseValue("owner-1", "MaxHealth", 150);
            Assert.AreEqual(150, repository.Get("owner-1", "Health").MaxValue);
        }

        [Test]
        public void SetBaseValue_WithRelationMin_UpdatesDependentAttribute()
        {
            var configs = new List<AttributeConfig>
            {
                new() { AttributeName = "Shield", Min = 0, Max = 9999, RelationMin = "MinShield" },
                new() { AttributeName = "MinShield", Min = 0, Max = 9999 }
            };
            controller.RegisterConfigs(configs);

            controller.CreateAttribute("owner-1", "MinShield", 0);
            controller.CreateAttribute("owner-1", "Shield", 50);
            Assert.AreEqual(0, repository.Get("owner-1", "Shield").MinValue);

            controller.SetBaseValue("owner-1", "MinShield", 30);

            Assert.AreEqual(30, repository.Get("owner-1", "Shield").MinValue);
        }

        [Test]
        public void AddModifier_ExistingAttribute_AddsModifier()
        {
            controller.CreateAttribute("owner-1", "Health", 100);
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");

            controller.AddModifier("owner-1", "Health", modifier);

            Assert.AreEqual(150, controller.GetValue("owner-1", "Health"));
        }

        [Test]
        public void AddModifier_NonExistentAttribute_CreatesAttributeWithModifier()
        {
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");

            controller.AddModifier("owner-1", "Health", modifier);

            Assert.AreEqual(50, controller.GetValue("owner-1", "Health"));
        }

        [Test]
        public void AddModifier_NotifiesObservers()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            controller.AddModifier("owner-1", "Health", modifier);

            Assert.AreEqual(100, lastInfo.OldValue);
            Assert.AreEqual(150, lastInfo.NewValue);
        }

        [Test]
        public void RemoveModifierById_RemovesModifierAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            controller.AddModifier("owner-1", "Health", modifier);

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.RemoveModifierById("owner-1", "Health", "mod-1");

            Assert.AreEqual(100, controller.GetValue("owner-1", "Health"));
            Assert.AreEqual(150, lastInfo.OldValue);
            Assert.AreEqual(100, lastInfo.NewValue);
        }

        [Test]
        public void RemoveModifierById_NonExistentAttribute_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => controller.RemoveModifierById("owner-1", "Health", "mod-1"));
        }

        [Test]
        public void RemoveModifiersBySource_RemovesAllFromSourceAndNotifies()
        {
            controller.CreateAttribute("owner-1", "Health", 100);
            controller.AddModifier("owner-1", "Health", new Modifier("mod-1", ModifyType.Flat, 30, "sword-1"));
            controller.AddModifier("owner-1", "Health", new Modifier("mod-2", ModifyType.Flat, 20, "sword-1"));
            controller.AddModifier("owner-1", "Health", new Modifier("mod-3", ModifyType.Flat, 10, "armor-1"));

            AttributeChangedInfo lastInfo = default;
            controller.ObserveAttribute("owner-1", "Health").Subscribe(info => lastInfo = info);

            controller.RemoveModifiersBySource("owner-1", "Health", "sword-1");

            Assert.AreEqual(110, controller.GetValue("owner-1", "Health"));
            Assert.AreEqual(160, lastInfo.OldValue);
            Assert.AreEqual(110, lastInfo.NewValue);
        }

        [Test]
        public void RemoveModifiersBySource_NonExistentAttribute_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => controller.RemoveModifiersBySource("owner-1", "Health", "source-1"));
        }

        [Test]
        public void ObserveAttribute_ByAttributeName_ReceivesChangesFromAllOwners()
        {
            controller.CreateAttribute("owner-1", "Health", 100);
            controller.CreateAttribute("owner-2", "Health", 200);

            var receivedInfos = new List<AttributeChangedInfo>();
            controller.ObserveAttribute("Health").Subscribe(info => receivedInfos.Add(info));

            controller.SetBaseValue("owner-1", "Health", 150);
            controller.SetBaseValue("owner-2", "Health", 250);

            Assert.AreEqual(2, receivedInfos.Count);
            Assert.AreEqual("owner-1", receivedInfos[0].OwnerId);
            Assert.AreEqual(150, receivedInfos[0].NewValue);
            Assert.AreEqual("owner-2", receivedInfos[1].OwnerId);
            Assert.AreEqual(250, receivedInfos[1].NewValue);
        }

        [Test]
        public void CreateAttribute_WithConfig_SetsCorrectMinMax()
        {
            var attribute = controller.CreateAttribute("owner-1", "CritRate", 20);

            Assert.AreEqual(20, attribute.BaseValue);
            Assert.AreEqual(0, attribute.MinValue);
            Assert.AreEqual(100, attribute.MaxValue);
        }

        [Test]
        public void CreateAttribute_WithUnlimitedConfig_UsesMinMaxIntValues()
        {
            var configs = new List<AttributeConfig>
            {
                new() { AttributeName = "Unlimited", Min = int.MinValue, Max = int.MaxValue }
            };
            controller.RegisterConfigs(configs);

            var attribute = controller.CreateAttribute("owner-1", "Unlimited", 100);

            Assert.AreEqual(int.MinValue, attribute.MinValue);
            Assert.AreEqual(int.MaxValue, attribute.MaxValue);
        }

        [Test]
        public void CreateAttribute_SavesAndReturnsAttribute()
        {
            var attribute = controller.CreateAttribute("owner-1", "Health", 100);

            Assert.NotNull(attribute);
            Assert.AreEqual(100, controller.GetValue("owner-1", "Health"));
        }

        [Test]
        public void RemoveAttribute_ExistingAttribute_RemovesFromRepository()
        {
            controller.CreateAttribute("owner-1", "Health", 100);

            controller.RemoveAttribute("owner-1", "Health");

            Assert.AreEqual(0, controller.GetValue("owner-1", "Health"));
        }

        [Test]
        public void RemoveAttribute_NonExistent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => controller.RemoveAttribute("owner-1", "Health"));
        }

        [Test]
        public void RemoveAttributesByOwner_RemovesAllOwnerAttributes()
        {
            controller.CreateAttribute("owner-1", "Health", 100);
            controller.CreateAttribute("owner-1", "Attack", 50);
            controller.CreateAttribute("owner-2", "Health", 200);

            controller.RemoveAttributesByOwner("owner-1");

            Assert.AreEqual(0, controller.GetValue("owner-1", "Health"));
            Assert.AreEqual(0, controller.GetValue("owner-1", "Attack"));
            Assert.AreEqual(200, controller.GetValue("owner-2", "Health"));
        }

        [Test]
        public void RemoveAttributesByOwner_NonExistentOwner_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => controller.RemoveAttributesByOwner("non-existent"));
        }
    }
}
