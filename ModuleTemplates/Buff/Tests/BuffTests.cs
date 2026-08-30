using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.Attribute;
using UniRx;

namespace Sumorin.Buff.Tests
{
	[TestFixture]
	public class BuffTests
	{
		private static Buff CreateBuff(IBuffConfig config = null) => new("buff-1", "Poison", config ?? new FakeBuffConfig(), "owner-1", "source-1");

		[TestCase(LifetimeType.TimeBased, 10f, TestName = "時間制保留配置時效")]
		[TestCase(LifetimeType.TurnBased, 3f, TestName = "回合制保留配置時效")]
		[TestCase(LifetimeType.Permanent, 0f, TestName = "永久型時效為 0")]
		public void Constructor_WithValidParameters_SetsAllProperties(LifetimeType lifetimeType, float lifetime)
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetimeType, lifetime, maxStack: 5));

			buff.Should()
				.BeEquivalentTo(
					new
					{
						Id = "buff-1",
						ConfigId = "Poison",
						OwnerId = "owner-1",
						SourceId = "source-1",
						StackCount = 1,
						RemainingLifetime = lifetime,
						IsExpired = false
					}
				);
		}

		[Test]
		public void Constructor_WithConfiguredEffects_CreatesFirstStackWithThoseEffects()
		{
			var effects = new List<ModifyEffectInfo>
			{
				new() { AttributeConfigId = "Health", ModifyType = ModifyType.Flat, Value = 10 },
				new() { AttributeConfigId = "Attack", ModifyType = ModifyType.Percent, Value = 20 }
			};

			var buff = CreateBuff(new FakeBuffConfig(effects: effects));

			buff.StackRecords.Should().ContainSingle();
			buff.StackRecords[0].Effects.Should().BeEquivalentTo(effects);
		}

		private static IEnumerable<TestCaseData> InvalidParameterCases()
		{
			yield return new TestCaseData(null, "Poison", "owner-1", "source-1", typeof(ArgumentNullException), "id").SetName("id 為 null");
			yield return new TestCaseData("", "Poison", "owner-1", "source-1", typeof(ArgumentException), "id").SetName("id 為空字串");
			yield return new TestCaseData("buff-1", null, "owner-1", "source-1", typeof(ArgumentException), "configId").SetName("configId 為 null");
			yield return new TestCaseData("buff-1", "", "owner-1", "source-1", typeof(ArgumentException), "configId").SetName("configId 為空字串");
			yield return new TestCaseData("buff-1", "Poison", null, "source-1", typeof(ArgumentException), "ownerId").SetName("ownerId 為 null");
			yield return new TestCaseData("buff-1", "Poison", "", "source-1", typeof(ArgumentException), "ownerId").SetName("ownerId 為空字串");
			yield return new TestCaseData("buff-1", "Poison", "owner-1", null, typeof(ArgumentException), "sourceId").SetName("sourceId 為 null");
			yield return new TestCaseData("buff-1", "Poison", "owner-1", "", typeof(ArgumentException), "sourceId").SetName("sourceId 為空字串");
		}

		[TestCaseSource(nameof(InvalidParameterCases))]
		public void Constructor_WithMissingRequiredValue_Throws(string id, string configId, string ownerId, string sourceId, Type exceptionType,
																string paramName)
		{
			Action act = () => _ = new Buff(id, configId, new FakeBuffConfig(), ownerId, sourceId);

			var exception = act.Should().Throw<ArgumentException>().Which;
			exception.Should().BeOfType(exceptionType);
			exception.ParamName.Should().Be(paramName);
		}

		[Test]
		public void Constructor_WithNullConfig_Throws()
		{
			Action act = () => _ = new Buff("buff-1", "Poison", null, "owner-1", "source-1");

			act.Should().ThrowExactly<ArgumentNullException>().WithParameterName("config");
		}

		[TestCase(LifetimeType.TimeBased, 0f, TestName = "時間制配置 0 修正為 1")]
		[TestCase(LifetimeType.TurnBased, -5f, TestName = "回合制配置負值修正為 1")]
		public void Constructor_WithNonPositiveLifetime_CorrectsToOne(LifetimeType lifetimeType, float configuredLifetime)
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetimeType, configuredLifetime));

			buff.Should().BeEquivalentTo(new { RemainingLifetime = 1f, IsExpired = false });
		}

		[Test]
		public void IsExpired_WithPermanentLifetime_AlwaysFalse()
		{
			CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f)).IsExpired.Should().BeFalse();
		}

		[Test]
		public void RefreshLifetime_WhenActive_RestoresConfiguredLifetime()
		{
			var buff = CreateBuff();
			buff.AdjustLifetime(-5f);

			buff.RefreshLifetime();

			buff.RemainingLifetime.Should().Be(10f);
		}

		[Test]
		public void RefreshLifetime_WithPermanent_KeepsLifetime()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));

			buff.RefreshLifetime();

			buff.RemainingLifetime.Should().Be(0f);
		}

		[Test]
		public void RefreshLifetime_WhenExpired_StaysExpired()
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetime: 5f));
			buff.AdjustLifetime(-10f);

			buff.RefreshLifetime();

			buff.IsExpired.Should().BeTrue();
		}

		[Test]
		public void SetLifetime_WhenActive_UpdatesLifetime()
		{
			var buff = CreateBuff();

			buff.SetLifetime(5f);

			buff.RemainingLifetime.Should().Be(5f);
		}

		[Test]
		public void SetLifetime_WithTurnBased_TruncatesFraction()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.TurnBased, 5f));

			buff.SetLifetime(3.7f);

			buff.RemainingLifetime.Should().Be(3f);
		}

		[Test]
		public void SetLifetime_WithPermanent_DoesNothing()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));

			buff.SetLifetime(10f);

			buff.RemainingLifetime.Should().Be(0f);
		}

		[Test]
		public void SetLifetime_ToNonPositive_NotifiesExpired()
		{
			var buff = CreateBuff();
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.SetLifetime(-1f);

			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustLifetime_WithPositiveDelta_ExtendsLifetime()
		{
			var buff = CreateBuff();

			buff.AdjustLifetime(3f);

			buff.RemainingLifetime.Should().Be(13f);
		}

		[Test]
		public void AdjustLifetime_WithTurnBased_TruncatesFraction()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.TurnBased, 5f));

			buff.AdjustLifetime(-2.7f);

			buff.RemainingLifetime.Should().Be(3f);
		}

		[Test]
		public void AdjustLifetime_WithZeroDelta_KeepsLifetime()
		{
			var buff = CreateBuff();

			buff.AdjustLifetime(0f);

			buff.RemainingLifetime.Should().Be(10f);
		}

		[TestCase(LifetimeType.TimeBased, 10f, -15f, TestName = "時間制扣到 0 以下")]
		[TestCase(LifetimeType.TurnBased, 3f, -5f, TestName = "回合制扣到 0 以下")]
		[TestCase(LifetimeType.TimeBased, 10f, float.NegativeInfinity, TestName = "負無限大")]
		public void AdjustLifetime_WhenLifetimeDepleted_NotifiesExpired(LifetimeType lifetimeType, float lifetime, float delta)
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetimeType, lifetime));
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(delta);

			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[TestCase(LifetimeType.TimeBased, 10f, -5f, TestName = "時間制仍有剩餘")]
		[TestCase(LifetimeType.TurnBased, 5f, -2f, TestName = "回合制仍有剩餘")]
		[TestCase(LifetimeType.Permanent, 0f, -10f, TestName = "永久型不受影響")]
		public void AdjustLifetime_WhenStillActive_DoesNotNotifyExpired(LifetimeType lifetimeType, float lifetime, float delta)
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetimeType, lifetime));
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(delta);

			onExpired.DidNotReceive().Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustLifetime_WhenAlreadyExpired_DoesNotNotifyExpiredAgain()
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetime: 5f));
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(-10f);
			buff.AdjustLifetime(-5f);

			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustLifetime_WhenDepletedWithRemainingStacksAndPartialExpire_RemovesOneStackAndRefreshes()
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetime: 5f, removeAllOnExpire: false));
			buff.AdjustStack(1);
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(-10f);

			buff.Should().BeEquivalentTo(new { StackCount = 1, RemainingLifetime = 5f, IsExpired = false });
			onExpired.DidNotReceive().Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustLifetime_WhenDepletedWithRemainingStacksAndFullExpire_NotifiesExpired()
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetime: 5f));
			buff.AdjustStack(1);
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(-10f);

			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustLifetime_WhenDepletedWithSingleStackAndPartialExpire_NotifiesExpired()
		{
			var buff = CreateBuff(new FakeBuffConfig(lifetime: 5f, removeAllOnExpire: false));
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustLifetime(-10f);

			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void AdjustStack_WithPositiveDelta_AddsRecordWithConfiguredEffects()
		{
			var config = new FakeBuffConfig(LifetimeType.Permanent, 0f);
			var buff = CreateBuff(config);

			buff.AdjustStack(1);

			buff.StackCount.Should().Be(2);
			buff.StackRecords[1].Effects.Should().BeEquivalentTo(config.Effects);
		}

		[Test]
		public void AdjustStack_WhenAtMaxStack_DoesNotAdd()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f, maxStack: 2));
			buff.AdjustStack(1);

			buff.AdjustStack(1);

			buff.StackCount.Should().Be(2);
		}

		[Test]
		public void AdjustStack_WithNegativeDelta_RemovesFromTheEnd()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));
			buff.AdjustStack(2);
			var onRemove = Substitute.For<Action<CollectionRemoveEvent<StackRecord>>>();
			buff.StackRecords.ObserveRemove().Subscribe(onRemove);

			buff.AdjustStack(-2);

			buff.StackCount.Should().Be(1);
			onRemove.Received(2).Invoke(Arg.Any<CollectionRemoveEvent<StackRecord>>());
		}

		[Test]
		public void Stack_WhenStackChanges_EmitsNewCount()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));
			var received = new List<int>();
			buff.Stack.Subscribe(received.Add);

			buff.AdjustStack(1);

			received.Should().BeEquivalentTo(new[] { 1, 2 }, options => options.WithStrictOrdering());
		}

		[Test]
		public void Lifetime_WhenTicked_EmitsRemainingValue()
		{
			var buff = CreateBuff();
			var received = new List<float>();
			buff.Lifetime.Subscribe(received.Add);

			buff.AdjustLifetime(-3f);

			received.Should().BeEquivalentTo(new[] { 10f, 7f }, options => options.WithStrictOrdering());
		}

		[Test]
		public void AdjustStack_WhenLastStackRemoved_NotifiesExpired()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);

			buff.AdjustStack(-1);

			buff.Should().BeEquivalentTo(new { StackCount = 0, IsExpired = true });
			onExpired.Received(1).Invoke(Arg.Any<Unit>());
		}

		[Test]
		public void ClearStacks_WithMultipleStacks_ResetsCollectionWithoutNotifyingExpired()
		{
			var buff = CreateBuff(new FakeBuffConfig(LifetimeType.Permanent, 0f));
			buff.AdjustStack(2);
			var onExpired = Substitute.For<Action<Unit>>();
			buff.OnExpired.Subscribe(onExpired);
			var onReset = Substitute.For<Action<Unit>>();
			buff.StackRecords.ObserveReset().Subscribe(onReset);

			buff.ClearStacks();

			buff.StackCount.Should().Be(0);
			onReset.Received(1).Invoke(Arg.Any<Unit>());
			onExpired.DidNotReceive().Invoke(Arg.Any<Unit>());
		}
	}
}