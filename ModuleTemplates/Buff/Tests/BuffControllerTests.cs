using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.Attribute;
using Sumorin.DDDCore;
using Sumorin.SumorinUtility;
using Zenject;

namespace Sumorin.Buff.Tests
{
	[TestFixture]
	public class BuffControllerTests: ZenjectUnitTestFixture
	{
		private BuffRepository repository;
		private IPublisher publisher;
		private IAttributeController attributeController;

		[SetUp]
		public override void Setup()
		{
			base.Setup();
			repository = new BuffRepository();
			publisher = Substitute.For<IPublisher>();
			attributeController = Substitute.For<IAttributeController>();
		}

		private BuffController CreateController(List<IConfig> configs = null)
		{
			Container.Bind<IBuffRepository>().FromInstance(repository);
			Container.Bind<IPublisher>().FromInstance(publisher);
			Container.Bind<IAttributeController>().FromInstance(attributeController);
			Container.Bind<ConfigManager>().FromInstance(new ConfigManager(configs ?? DefaultConfigs()));
			return Container.Instantiate<BuffController>();
		}

		private static List<IConfig> DefaultConfigs() =>
			new()
			{
				new FakeBuffConfig(LifetimeType.TimeBased, 10f, StackBehavior.IncreaseStack, 5, tags: new List<string> { "Debuff", "DoT" }, id: "Poison"),
				new FakeBuffConfig(LifetimeType.TimeBased, 5f, tags: new List<string> { "Debuff", "DoT" }, id: "Burn"),
				new FakeBuffConfig(LifetimeType.TurnBased, 2f, StackBehavior.Replace, id: "Invincible"),
				new FakeBuffConfig(LifetimeType.Permanent, 0f, id: "Passive"),
				new FakeBuffConfig(LifetimeType.TimeBased, 6f, StackBehavior.Independent, id: "Echo"),
				new FakeBuffConfig(LifetimeType.TimeBased, 8f, mutualExclusionGroup: "Movement", priority: 1, id: "SpeedUp"),
				new FakeBuffConfig(LifetimeType.TimeBased, 8f, mutualExclusionGroup: "Movement", id: "SpeedDown")
			};

		[Test]
		public void AddBuff_WithRegisteredConfig_CreatesBuffAndPublishesEvent()
		{
			var controller = CreateController();

			var result = controller.AddBuff("owner-1", "Poison", "source-1");

			result.IsSuccess.Should().BeTrue();
			repository.Get(result.Value)
					  .Should()
					  .BeEquivalentTo(
						  new
						  {
							  ConfigId = "Poison",
							  OwnerId = "owner-1",
							  SourceId = "source-1",
							  StackCount = 1
						  }
					  );
			publisher.Received(1).Publish(Arg.Is<BuffApplied>(e => e.BuffId == result.Value && e.OwnerId == "owner-1" && e.ConfigId == "Poison"));
		}

		[Test]
		public void AddBuff_WithRegisteredConfig_AppliesFirstStackEffectsWithoutStackChangedEvent()
		{
			var controller = CreateController();

			var result = controller.AddBuff("owner-1", "Poison", "source-1");

			attributeController.Received(1).AddModifiers("owner-1", Arg.Any<List<ModifyEffectInfo>>(), result.Value, "Poison");
			publisher.DidNotReceive().Publish(Arg.Any<BuffStackChanged>());
		}

		[Test]
		public void AddBuff_WithUnknownConfig_FailsAndPublishesNothing()
		{
			var controller = CreateController();

			controller.AddBuff("owner-1", "Unknown", "source-1").IsSuccess.Should().BeFalse();
			publisher.DidNotReceive().Publish(Arg.Any<BuffApplied>());
		}

		[TestCase(null, TestName = "擁有者為 null 時失敗")]
		[TestCase("", TestName = "擁有者為空字串時失敗")]
		public void AddBuff_WithoutOwnerId_Fails(string ownerId)
		{
			CreateController().AddBuff(ownerId, "Poison", "source-1").IsSuccess.Should().BeFalse();
		}

		[Test]
		public void AddBuff_WhenSameBuffExistsWithIncreaseStack_AddsStackAndPublishesStackChanged()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			var result = controller.AddBuff("owner-1", "Poison", "source-2");

			result.Value.Should().Be(buffId);
			repository.Get(buffId).StackCount.Should().Be(2);
			publisher.Received(1).Publish(Arg.Is<BuffStackChanged>(e => e.OldStack == 1 && e.NewStack == 2));
		}

		[Test]
		public void AddBuff_WhenAtMaxStack_KeepsStackCount()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			for(var i = 0; i < 10; i++)
			{
				controller.AddBuff("owner-1", "Poison", "source-1");
			}

			repository.Get(buffId).StackCount.Should().Be(5);
		}

		[Test]
		public void AddBuff_WhenSameBuffExistsWithRefreshDuration_RefreshesLifetimeWithoutAddingStack()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1").Value;
			controller.AdjustBuffLifetime(buffId, -3f);

			var result = controller.AddBuff("owner-1", "Burn", "source-2");

			result.Value.Should().Be(buffId);
			repository.Get(buffId).Should().BeEquivalentTo(new { RemainingLifetime = 5f, StackCount = 1 });
		}

		[Test]
		public void AddBuff_WhenSameBuffExistsWithIndependent_CreatesSecondBuff()
		{
			var controller = CreateController();
			var firstId = controller.AddBuff("owner-1", "Echo", "source-1").Value;

			var secondId = controller.AddBuff("owner-1", "Echo", "source-2").Value;

			secondId.Should().NotBe(firstId);
			repository.GetByOwner("owner-1").Should().HaveCount(2);
		}

		[Test]
		public void AddBuff_WhenSameBuffExistsWithReplace_RemovesOldAndCreatesNew()
		{
			var controller = CreateController();
			var firstId = controller.AddBuff("owner-1", "Invincible", "source-1").Value;

			var secondId = controller.AddBuff("owner-1", "Invincible", "source-2").Value;

			secondId.Should().NotBe(firstId);
			repository.Get(firstId).Should().BeNull();
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == firstId && e.Reason == BuffRemoveReason.Replaced));
		}

		[Test]
		public void AddBuff_WhenBlockedByHigherPriorityInSameGroup_Fails()
		{
			var controller = CreateController();
			controller.AddBuff("owner-1", "SpeedUp", "source-1");

			controller.AddBuff("owner-1", "SpeedDown", "source-2").IsSuccess.Should().BeFalse();
			repository.GetByOwner("owner-1").Should().ContainSingle();
		}

		[Test]
		public void AddBuff_WhenReplacingLowerPriorityInSameGroup_RemovesTheOldOne()
		{
			var controller = CreateController();
			var lowerId = controller.AddBuff("owner-1", "SpeedDown", "source-1").Value;

			controller.AddBuff("owner-1", "SpeedUp", "source-2").IsSuccess.Should().BeTrue();

			repository.Get(lowerId).Should().BeNull();
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == lowerId && e.Reason == BuffRemoveReason.Replaced));
		}

		[Test]
		public void RemoveBuff_WithExistingBuff_RemovesModifiersAndPublishesEvent()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			controller.RemoveBuff(buffId).IsSuccess.Should().BeTrue();

			repository.Get(buffId).Should().BeNull();
			attributeController.Received(1).RemoveAllModifiersBySource("owner-1", buffId);
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.Reason == BuffRemoveReason.Manual));
		}

		[Test]
		public void RemoveBuffsBySource_WithMixedSources_RemovesOnlyThatSource()
		{
			var controller = CreateController();
			var poisonId = controller.AddBuff("owner-1", "Poison", "sword-1").Value;
			var burnId = controller.AddBuff("owner-1", "Burn", "staff-1").Value;

			controller.RemoveBuffsBySource("owner-1", "sword-1");

			repository.Get(poisonId).Should().BeNull();
			repository.Get(burnId).Should().NotBeNull();
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.Reason == BuffRemoveReason.SourceRemoved));
		}

		[Test]
		public void RemoveBuffsByOwner_WithMultipleOwners_RemovesOnlyThatOwner()
		{
			var controller = CreateController();
			controller.AddBuff("owner-1", "Poison", "source-1");
			controller.AddBuff("owner-1", "Burn", "source-1");
			controller.AddBuff("owner-2", "Poison", "source-1");

			controller.RemoveBuffsByOwner("owner-1");

			repository.GetByOwner("owner-1").Should().BeEmpty();
			repository.GetByOwner("owner-2").Should().ContainSingle();
		}

		[Test]
		public void RemoveBuffsByTag_WithMatchingTag_RemovesTaggedBuffs()
		{
			var controller = CreateController();
			controller.AddBuff("owner-1", "Poison", "source-1");
			var passiveId = controller.AddBuff("owner-1", "Passive", "source-1").Value;

			controller.RemoveBuffsByTag("owner-1", "DoT");

			repository.GetByOwner("owner-1").Should().BeEquivalentTo(new[] { new { Id = passiveId } });
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.Reason == BuffRemoveReason.TagRemoved));
		}

		[Test]
		public void TickTime_WithTimeBasedBuff_ReducesLifetime()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			controller.TickTime(3f).IsSuccess.Should().BeTrue();

			repository.Get(buffId).RemainingLifetime.Should().Be(7f);
		}

		[Test]
		public void TickTime_WhenLifetimeDepleted_RemovesBuffWithExpiredReason()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1").Value;

			controller.TickTime(5f);

			repository.Get(buffId).Should().BeNull();
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.Reason == BuffRemoveReason.Expired));
		}

		[Test]
		public void TickTime_WithNonTimeBasedBuffs_LeavesThemUntouched()
		{
			var controller = CreateController();
			var turnId = controller.AddBuff("owner-1", "Invincible", "source-1").Value;
			var permanentId = controller.AddBuff("owner-1", "Passive", "source-1").Value;

			controller.TickTime(100f);

			repository.Get(turnId).RemainingLifetime.Should().Be(2f);
			repository.Get(permanentId).RemainingLifetime.Should().Be(0f);
		}

		[Test]
		public void TickTurn_WithTurnBasedBuff_ReducesLifetimeForThatOwnerOnly()
		{
			var controller = CreateController();
			var ownerBuffId = controller.AddBuff("owner-1", "Invincible", "source-1").Value;
			var otherBuffId = controller.AddBuff("owner-2", "Invincible", "source-1").Value;

			controller.TickTurn("owner-1").IsSuccess.Should().BeTrue();

			repository.Get(ownerBuffId).RemainingLifetime.Should().Be(1f);
			repository.Get(otherBuffId).RemainingLifetime.Should().Be(2f);
		}

		[Test]
		public void AdjustStack_WithPositiveDelta_AppliesModifiersAndPublishesStackChanged()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;
			attributeController.ClearReceivedCalls();

			controller.AdjustStack(buffId, 1).IsSuccess.Should().BeTrue();

			repository.Get(buffId).StackCount.Should().Be(2);
			attributeController.Received(1).AddModifiers("owner-1", Arg.Any<List<ModifyEffectInfo>>(), buffId, "Poison");
			publisher.Received(1).Publish(Arg.Is<BuffStackChanged>(e => e.OldStack == 1 && e.NewStack == 2));
		}

		[Test]
		public void AdjustStack_WithNegativeDelta_RemovesModifiersAndPublishesStackChanged()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;
			controller.AdjustStack(buffId, 1);
			attributeController.ClearReceivedCalls();

			controller.AdjustStack(buffId, -1);

			repository.Get(buffId).StackCount.Should().Be(1);
			attributeController.Received(1).RemoveModifier("owner-1", Arg.Any<ModifyEffectInfo>(), buffId);
			publisher.Received(1).Publish(Arg.Is<BuffStackChanged>(e => e.OldStack == 2 && e.NewStack == 1));
		}

		[Test]
		public void AdjustStack_WhenLastStackRemoved_RemovesBuffWithExpiredReason()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			controller.AdjustStack(buffId, -1);

			repository.Get(buffId).Should().BeNull();
			publisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.Reason == BuffRemoveReason.Expired));
		}

		[Test]
		public void ObserveStackCount_WhenStackChanges_ReflectsNewCount()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;
			var observed = controller.ObserveStackCount(buffId);
			observed.Value.Should().Be(1);

			controller.AdjustStack(buffId, 1);

			observed.Value.Should().Be(2);
		}

		[Test]
		public void ObserveLifetime_WhenTicked_ReflectsRemainingLifetime()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;
			var observed = controller.ObserveLifetime(buffId);
			observed.Value.Should().Be(10f);

			controller.TickTime(3f);

			observed.Value.Should().Be(7f);
		}

		[TestCase(TestName = "訂閱不存在的 Buff 回傳 null")]
		public void ObserveValues_WithUnknownBuff_ReturnNull()
		{
			var controller = CreateController();

			controller.ObserveStackCount("buff-404").Should().BeNull();
			controller.ObserveLifetime("buff-404").Should().BeNull();
		}

		[Test]
		public void GetBuffInfo_WithExistingBuff_ReturnsSnapshot()
		{
			var controller = CreateController();
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1").Value;

			controller.GetBuffInfo(buffId).Should().Be(new BuffInfo(buffId, "Poison", 1, LifetimeType.TimeBased, 10f));
		}

		[Test]
		public void GetBuffInfo_WithUnknownBuff_ReturnsNull()
		{
			CreateController().GetBuffInfo("buff-404").Should().BeNull();
		}

		[Test]
		public void RemoveBuff_OnUnknownBuff_FailsInsteadOfIgnoring()
		{
			CreateController().RemoveBuff("buff-404").IsSuccess.Should().BeFalse();
		}
	}
}