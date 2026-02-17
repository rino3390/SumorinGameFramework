using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.GameFramework.AttributeSystem;
using Sumorin.GameFramework.DDDCore;
using UniRx;

namespace Sumorin.GameFramework.BuffSystem.Tests
{
	[TestFixture]
	public class BuffControllerTests
	{
		private BuffController controller;
		private BuffRepository repository;
		private IPublisher mockPublisher;
		private IAttributeController mockAttributeController;

		[SetUp]
		public void Setup()
		{
			mockPublisher = Substitute.For<IPublisher>();
			mockAttributeController = Substitute.For<IAttributeController>();

			// 設定 AddModifiers 回傳模擬的 modifierId
			var callCount = 0;
			mockAttributeController.AddModifiers(Arg.Any<string>(), Arg.Any<List<ModifyEffectInfo>>(), Arg.Any<string>(), Arg.Any<string>())
								   .Returns(callInfo =>
									   {
										   var effects = callInfo.ArgAt<List<ModifyEffectInfo>>(1);
										   return effects.Select(e => (e.AttributeName, $"mod-{++callCount}")).ToList();
									   }
								   );

			repository = new BuffRepository();
			controller = new BuffController(repository, mockPublisher, mockAttributeController);

			// 註冊配置
			var configs = new List<BuffConfig>
			{
				new()
				{
					BuffName = "Poison",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 10f,
					StackBehavior = StackBehavior.IncreaseStack,
					MaxStack = 5,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = -5 }
					},
					Tags = new List<string> { "Debuff", "DoT" }
				},
				new()
				{
					BuffName = "Burn",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 5f,
					StackBehavior = StackBehavior.RefreshDuration,
					MaxStack = -1,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Defense", ModifyType = ModifyType.Percent, Value = -20 }
					},
					Tags = new List<string> { "Debuff", "DoT" }
				},
				new()
				{
					BuffName = "SpeedUp",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 8f,
					StackBehavior = StackBehavior.RefreshDuration,
					MaxStack = -1,
					MutualExclusionGroup = "Movement",
					Priority = 1,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Speed", ModifyType = ModifyType.Percent, Value = 30 }
					},
					Tags = new List<string> { "Buff", "Movement" }
				},
				new()
				{
					BuffName = "SpeedDown",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 8f,
					StackBehavior = StackBehavior.RefreshDuration,
					MaxStack = -1,
					MutualExclusionGroup = "Movement",
					Priority = 1,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Speed", ModifyType = ModifyType.Percent, Value = -30 }
					},
					Tags = new List<string> { "Debuff", "Movement" }
				},
				new()
				{
					BuffName = "Invincible",
					LifetimeType = LifetimeType.TurnBased,
					Lifetime = 2f,
					StackBehavior = StackBehavior.Replace,
					MaxStack = -1,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "InvincibleCount", ModifyType = ModifyType.Flat, Value = 1 }
					},
					Tags = new List<string> { "Buff", "Immunity" }
				},
				new()
				{
					BuffName = "Independent",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 5f,
					StackBehavior = StackBehavior.Independent,
					MaxStack = -1,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>(),
					Tags = new List<string> { "Buff" }
				},
				new()
				{
					BuffName = "Permanent",
					LifetimeType = LifetimeType.Permanent,
					Lifetime = 0f,
					StackBehavior = StackBehavior.RefreshDuration,
					MaxStack = -1,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>(),
					Tags = new List<string> { "Buff", "Passive" },
					RemoveAllOnExpire = true
				},
				new()
				{
					BuffName = "StackPoison",
					LifetimeType = LifetimeType.TimeBased,
					Lifetime = 5f,
					StackBehavior = StackBehavior.IncreaseStack,
					MaxStack = 5,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = -5 }
					},
					Tags = new List<string> { "Debuff" },
					RemoveAllOnExpire = false
				},
				new()
				{
					BuffName = "StackShield",
					LifetimeType = LifetimeType.TurnBased,
					Lifetime = 2f,
					StackBehavior = StackBehavior.IncreaseStack,
					MaxStack = 5,
					MutualExclusionGroup = null,
					Priority = 0,
					Effects = new List<ModifyEffectInfo>
					{
						new() { AttributeName = "Defense", ModifyType = ModifyType.Flat, Value = 10 }
					},
					Tags = new List<string> { "Buff" },
					RemoveAllOnExpire = false
				}
			};
			controller.RegisterConfigs(configs);
		}

		[Test]
		public void AddBuff_WithValidParameters_CreatesBuffAndPublishesEvent()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

			Assert.IsNotNull(buffId);
			var buff = controller.GetBuff(buffId);
			Assert.AreEqual("Poison", buff.Config.BuffName);
			Assert.AreEqual("owner-1", buff.OwnerId);
			Assert.AreEqual("source-1", buff.SourceId);
			mockPublisher.Received(1)
						 .Publish(Arg.Is<BuffApplied>(e => e.BuffId == buffId && e.OwnerId == "owner-1" && e.BuffName == "Poison" && e.SourceId == "source-1"));
		}

		[Test]
		public void AddBuff_WithIncreaseStack_IncreasesStackCountAndPublishesEvent()
		{
			var buffId1 = controller.AddBuff("owner-1", "Poison", "source-1");
			var buffId2 = controller.AddBuff("owner-1", "Poison", "source-2");

			Assert.AreEqual(buffId1, buffId2);
			var buff = controller.GetBuff(buffId1);
			Assert.AreEqual(2, buff.StackCount);
			mockPublisher.Received(1).Publish(Arg.Is<BuffStackChanged>(e => e.OldStack == 1 && e.NewStack == 2));
		}

		[Test]
		public void AddBuff_WithRefreshDuration_RefreshesDuration()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");
			controller.TickTime(3f); // 模擬時間流逝，剩餘 2 秒

			controller.AddBuff("owner-1", "Burn", "source-2");

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(5f, buff.RemainingLifetime);
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

		[Test]
		public void AddBuff_WithMutualExclusion_RemovesConflictingBuff()
		{
			var buffId1 = controller.AddBuff("owner-1", "SpeedDown", "source-1");
			var buffId2 = controller.AddBuff("owner-1", "SpeedUp", "source-2");

			Assert.IsNull(controller.GetBuff(buffId1));
			Assert.IsNotNull(controller.GetBuff(buffId2));
		}

		[Test]
		public void RemoveBuff_WithExistingBuff_RemovesBuffAndPublishesEvent()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			mockPublisher.ClearReceivedCalls();

			controller.RemoveBuff(buffId);

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.OwnerId == "owner-1" && e.BuffName == "Poison"));
		}

		[Test]
		public void RemoveBuff_WithNonExistingBuff_DoesNothing()
		{
			controller.RemoveBuff("non-existing");

			mockPublisher.DidNotReceive().Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void RemoveBuffsBySource_RemovesAllBuffsFromSource()
		{
			controller.AddBuff("owner-1", "Poison", "source-1");
			controller.AddBuff("owner-1", "Burn", "source-1");
			controller.AddBuff("owner-1", "Independent", "source-2");

			controller.RemoveBuffsBySource("owner-1", "source-1");

			Assert.AreEqual(1, controller.GetBuffsByOwner("owner-1").Count);
			mockPublisher.Received(2).Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void RemoveBuffsByOwner_RemovesAllBuffsFromOwnerAndPublishesEvents()
		{
			controller.AddBuff("owner-1", "Poison", "source-1");
			controller.AddBuff("owner-1", "Burn", "source-2");
			controller.AddBuff("owner-2", "Independent", "source-3");
			mockPublisher.ClearReceivedCalls();

			controller.RemoveBuffsByOwner("owner-1");

			Assert.IsEmpty(controller.GetBuffsByOwner("owner-1"));
			Assert.AreEqual(1, controller.GetBuffsByOwner("owner-2").Count);
			mockPublisher.Received(2).Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void RemoveBuffsByTag_RemovesAllBuffsWithTagAndPublishesEvents()
		{
			controller.AddBuff("owner-1", "Poison", "source-1");  // Tags: Debuff, DoT
			controller.AddBuff("owner-1", "Burn", "source-2");    // Tags: Debuff, DoT
			controller.AddBuff("owner-1", "SpeedUp", "source-3"); // Tags: Buff, Movement
			mockPublisher.ClearReceivedCalls();

			controller.RemoveBuffsByTag("owner-1", "DoT");

			Assert.AreEqual(1, controller.GetBuffsByOwner("owner-1").Count);
			Assert.AreEqual("SpeedUp", controller.GetBuffsByOwner("owner-1")[0].Config.BuffName);
			mockPublisher.Received(2).Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void RemoveBuffsByTag_WithNonMatchingTag_DoesNothing()
		{
			controller.AddBuff("owner-1", "Poison", "source-1");

			controller.RemoveBuffsByTag("owner-1", "NonExistentTag");

			Assert.AreEqual(1, controller.GetBuffsByOwner("owner-1").Count);
		}

		[Test]
		public void TickTime_WithExpiredBuff_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.TickTime(6f);

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void TickTime_WithNonExpiredBuff_KeepsBuff()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.TickTime(3f);

			Assert.IsNotNull(controller.GetBuff(buffId));
			Assert.AreEqual(2f, controller.GetBuff(buffId).RemainingLifetime);
		}

		[Test]
		public void TickTurn_WithExpiredBuff_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "Invincible", "source-1");

			controller.TickTurn("owner-1");
			controller.TickTurn("owner-1");

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Any<BuffRemoved>());
		}

		[Test]
		public void ObserveBuffs_NotifiesOnBuffAdded()
		{
			List<BuffInfo> receivedBuffs = null;
			controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

			receivedBuffs.Should().BeEquivalentTo(new[] { new { BuffId = buffId, BuffName = "Poison", StackCount = 1 } });
		}

		[Test]
		public void ObserveBuffs_NotifiesOnBuffRemoved()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			List<BuffInfo> receivedBuffs = null;
			controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

			controller.RemoveBuff(buffId);

			receivedBuffs.Should().BeEmpty();
		}

		[Test]
		public void AdjustBuffLifetime_WithPositiveDelta_ExtendsLifetime()
		{
			var buffId = controller.AddBuff("owner-1", "Invincible", "source-1");

			controller.AdjustBuffLifetime(buffId, 3);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(5f, buff.RemainingLifetime);
		}

		[Test]
		public void AdjustBuffLifetime_WithNegativeDelta_ReducesLifetime()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.AdjustBuffLifetime(buffId, -2f);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(3f, buff.RemainingLifetime);
		}

		[Test]
		public void AdjustBuffLifetime_WithNonExistingBuff_DoesNothing()
		{
			controller.AdjustBuffLifetime("non-existing", 3);

			// 不應拋出例外
			Assert.Pass();
		}

		[Test]
		public void AdjustBuffLifetime_WithPermanentBuff_DoesNotAffect()
		{
			var buffId = controller.AddBuff("owner-1", "Permanent", "source-1");

			controller.AdjustBuffLifetime(buffId, 10f);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(0f, buff.RemainingLifetime);
		}

		[Test]
		public void AdjustBuffLifetime_ReducingBelowZero_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.AdjustBuffLifetime(buffId, -10f);

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.Reason == "Expired"));
		}

		[Test]
		public void AdjustBuffLifetime_NotifiesBuffsChanged()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");
			List<BuffInfo> receivedBuffs = null;
			controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

			controller.AdjustBuffLifetime(buffId, 5f);

			Assert.IsNotNull(receivedBuffs);
			Assert.AreEqual(10f, receivedBuffs[0].RemainingLifetime);
		}

		[Test]
		public void SetBuffLifetime_SetsLifetimeToSpecifiedValue()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.SetBuffLifetime(buffId, 3f);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(3f, buff.RemainingLifetime);
		}

		[Test]
		public void SetBuffLifetime_WithNonExistingBuff_DoesNothing()
		{
			controller.SetBuffLifetime("non-existing", 10f);

			// 不應拋出例外
			Assert.Pass();
		}

		[Test]
		public void SetBuffLifetime_WithPermanentBuff_DoesNotAffect()
		{
			var buffId = controller.AddBuff("owner-1", "Permanent", "source-1");

			controller.SetBuffLifetime(buffId, 10f);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(0f, buff.RemainingLifetime);
		}

		[Test]
		public void SetBuffLifetime_ToZero_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");

			controller.SetBuffLifetime(buffId, 0f);

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId && e.Reason == "Expired"));
		}

		[Test]
		public void SetBuffLifetime_NotifiesBuffsChanged()
		{
			var buffId = controller.AddBuff("owner-1", "Burn", "source-1");
			List<BuffInfo> receivedBuffs = null;
			controller.ObserveBuffs("owner-1").Subscribe(buffs => receivedBuffs = buffs);

			controller.SetBuffLifetime(buffId, 3f);

			Assert.IsNotNull(receivedBuffs);
			Assert.AreEqual(3f, receivedBuffs[0].RemainingLifetime);
		}

		[TestCase("Burn", TestName = "RefreshDuration does not increase stack")]
		[TestCase("SpeedUp", TestName = "RefreshDuration with MutualExclusion does not increase stack")]
		public void AddBuff_WithNonStackBehavior_DoesNotIncreaseStackCount(string buffName)
		{
			controller.AddBuff("owner-1", buffName, "source-1");
			controller.AddBuff("owner-1", buffName, "source-2");

			var buffs = controller.GetBuffsByOwner("owner-1");
			Assert.AreEqual(1, buffs.Count);
			Assert.AreEqual(1, buffs[0].StackCount);
		}

		[Test]
		public void AddBuff_AppliesEffects()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");

			mockAttributeController.Received(1)
								   .AddModifiers(
									   "owner-1",
									   Arg.Is<List<ModifyEffectInfo>>(list => list.Count == 1 && list[0].AttributeName == "Health" && list[0].Value == -5),
									   buffId, "Poison"
								   );
			controller.GetBuff(buffId).StackRecords.Should().HaveCount(1);
		}

		[Test]
		public void RemoveBuff_RemovesEffects()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			mockAttributeController.ClearReceivedCalls();

			controller.RemoveBuff(buffId);

			mockAttributeController.Received(1).RemoveAllModifiersBySource("owner-1", buffId);
		}

		[Test]
		public void AddBuff_WithIncreaseStack_AddsModifiersForNewStack()
		{
			controller.AddBuff("owner-1", "Poison", "source-1");
			mockAttributeController.ClearReceivedCalls();

			controller.AddBuff("owner-1", "Poison", "source-2");

			mockAttributeController.Received(1)
								   .AddModifiers(
									   "owner-1",
									   Arg.Is<List<ModifyEffectInfo>>(list => list.Count == 1 && list[0].AttributeName == "Health" && list[0].Value == -5),
									   Arg.Any<string>(), "Poison"
								   );
		}

		[Test]
		public void AdjustStack_WithNegativeDelta_DecreasesStackAndRemovesModifiers()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			controller.AddBuff("owner-1", "Poison", "source-2"); // stack = 2
			mockAttributeController.ClearReceivedCalls();

			controller.AdjustStack(buffId, -1);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(1, buff.StackCount);
			mockAttributeController.Received(1).RemoveModifier("owner-1", Arg.Is<ModifyEffectInfo>(e => e.AttributeName == "Health" && e.Value == -5), buffId);
		}

		[Test]
		public void AdjustStack_WithPositiveDelta_IncreasesStackAndAddsModifiers()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			mockAttributeController.ClearReceivedCalls();

			controller.AdjustStack(buffId, 2);

			var buff = controller.GetBuff(buffId);
			Assert.AreEqual(3, buff.StackCount);
			mockAttributeController.Received(2)
								   .AddModifiers(
									   "owner-1",
									   Arg.Is<List<ModifyEffectInfo>>(list => list.Count == 1 && list[0].AttributeName == "Health" && list[0].Value == -5),
									   buffId, "Poison"
								   );
		}

		[Test]
		public void AdjustStack_ReducingToZero_RemovesBuffAndPublishesEvent()
		{
			var buffId = controller.AddBuff("owner-1", "Poison", "source-1");
			mockPublisher.ClearReceivedCalls();

			controller.AdjustStack(buffId, -1);

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId));
		}

		[Test]
		public void AdjustStack_WithNonExistingBuff_DoesNothing()
		{
			controller.AdjustStack("non-existing", -1);

			// 不應拋出例外
			Assert.Pass();
		}

		[Test]
		public void TickTime_WithRemoveAllOnExpireFalse_RemovesOneStackAndRefreshesLifetime()
		{
			var buffId = controller.AddBuff("owner-1", "StackPoison", "source-1");
			controller.AddBuff("owner-1", "StackPoison", "source-2"); // stack = 2

			controller.TickTime(6f); // 超過 5 秒

			var buff = controller.GetBuff(buffId);
			Assert.IsNotNull(buff);
			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(5f, buff.RemainingLifetime);
		}

		[Test]
		public void TickTime_WithRemoveAllOnExpireFalse_WhenLastStack_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "StackPoison", "source-1"); // stack = 1

			controller.TickTime(6f); // 超過 5 秒

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId));
		}

		[Test]
		public void TickTurn_WithRemoveAllOnExpireFalse_RemovesOneStackAndRefreshesLifetime()
		{
			var buffId = controller.AddBuff("owner-1", "StackShield", "source-1");
			controller.AddBuff("owner-1", "StackShield", "source-2"); // stack = 2

			controller.TickTurn("owner-1");
			controller.TickTurn("owner-1"); // 2 回合後

			var buff = controller.GetBuff(buffId);
			Assert.IsNotNull(buff);
			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(2f, buff.RemainingLifetime);
		}

		[Test]
		public void TickTurn_WithRemoveAllOnExpireFalse_WhenLastStack_RemovesBuff()
		{
			var buffId = controller.AddBuff("owner-1", "StackShield", "source-1"); // stack = 1

			controller.TickTurn("owner-1");
			controller.TickTurn("owner-1"); // 2 回合後

			Assert.IsNull(controller.GetBuff(buffId));
			mockPublisher.Received(1).Publish(Arg.Is<BuffRemoved>(e => e.BuffId == buffId));
		}

		[Test]
		public void TickTurn_WithMultipleTurns_DecreasesLifetimeBySpecifiedAmount()
		{
			var buffId = controller.AddBuff("owner-1", "StackShield", "source-1"); // Lifetime = 2

			controller.TickTurn("owner-1", 1);

			var buff = controller.GetBuff(buffId);
			Assert.IsNotNull(buff);
			Assert.AreEqual(1f, buff.RemainingLifetime);
		}
	}
}