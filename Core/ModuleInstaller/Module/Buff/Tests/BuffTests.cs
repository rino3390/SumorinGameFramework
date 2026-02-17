using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.GameFramework.AttributeSystem;
using UniRx;

namespace Sumorin.GameFramework.BuffSystem.Tests
{
	[TestFixture]
	public class BuffTests
	{
		private static readonly List<ModifyEffectInfo> DefaultEffects = new()
		{
			new ModifyEffectInfo { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = 10 }
		};

		private static BuffConfig CreateConfig(string buffName = "Poison", LifetimeType lifetimeType = LifetimeType.TimeBased, float lifetime = 10f,
											   int maxStack = -1, bool removeAllOnExpire = true, List<ModifyEffectInfo> effects = null)
		{
			return new BuffConfig
			{
				BuffName = buffName,
				LifetimeType = lifetimeType,
				Lifetime = lifetime,
				MaxStack = maxStack,
				RemoveAllOnExpire = removeAllOnExpire,
				Effects = effects ?? DefaultEffects
			};
		}

	#region Constructor Tests
		[TestCase(LifetimeType.TimeBased, 10f, TestName = "TimeBased sets all properties")]
		[TestCase(LifetimeType.TurnBased, 3f, TestName = "TurnBased sets all properties")]
		[TestCase(LifetimeType.Permanent, 0f, TestName = "Permanent sets all properties")]
		public void Constructor_SetsAllProperties(LifetimeType lifetimeType, float lifetime)
		{
			var config = CreateConfig(lifetimeType: lifetimeType, lifetime: lifetime, maxStack: 5);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			Assert.AreEqual("buff-1", buff.Id);
			Assert.AreEqual("Poison", buff.Config.BuffName);
			Assert.AreEqual("owner-1", buff.OwnerId);
			Assert.AreEqual("source-1", buff.SourceId);
			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(lifetimeType, buff.Config.LifetimeType);
			Assert.AreEqual(lifetime, buff.RemainingLifetime);
			Assert.IsFalse(buff.IsExpired);
			buff.StackRecords.Should().HaveCount(1);
		}

		[Test]
		public void Constructor_InitializesStackRecordWithEffects()
		{
			var effects = new List<ModifyEffectInfo>
			{
				new() { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = 10 },
				new() { AttributeName = "Attack", ModifyType = ModifyType.Percent, Value = 20 }
			};
			var config = CreateConfig(effects: effects);

			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			buff.StackRecords.Should().HaveCount(1);
			buff.StackRecords[0].Effects.Should().HaveCount(2);
			buff.StackRecords[0].Effects[0].AttributeName.Should().Be("Health");
			buff.StackRecords[0].Effects[1].AttributeName.Should().Be("Attack");
		}

		private static IEnumerable<TestCaseData> InvalidParameterCases()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			yield return new TestCaseData(null, config, "owner-1", "source-1", typeof(ArgumentNullException), "id").SetName("Null id throws");
			yield return new TestCaseData("", config, "owner-1", "source-1", typeof(ArgumentException), "id").SetName("Empty id throws");
			yield return new TestCaseData("buff-1", config, null, "source-1", typeof(ArgumentException), "ownerId").SetName("Null ownerId throws");
			yield return new TestCaseData("buff-1", config, "", "source-1", typeof(ArgumentException), "ownerId").SetName("Empty ownerId throws");
			yield return new TestCaseData("buff-1", config, "owner-1", null, typeof(ArgumentException), "sourceId").SetName("Null sourceId throws");
			yield return new TestCaseData("buff-1", config, "owner-1", "", typeof(ArgumentException), "sourceId").SetName("Empty sourceId throws");
		}

		[TestCaseSource(nameof(InvalidParameterCases))]
		public void Constructor_WithInvalidParameter_ThrowsException(string id, BuffConfig config, string ownerId, string sourceId, Type exceptionType,
																	 string paramName)
		{
			Assert.That(() => new Buff(id, config, ownerId, sourceId), Throws.TypeOf(exceptionType).With.Property("ParamName").EqualTo(paramName));
		}

		[TestCase(LifetimeType.TimeBased, 0f, TestName = "TimeBased with zero corrects to 1")]
		[TestCase(LifetimeType.TimeBased, -1f, TestName = "TimeBased with negative corrects to 1")]
		[TestCase(LifetimeType.TurnBased, 0f, TestName = "TurnBased with zero corrects to 1")]
		[TestCase(LifetimeType.TurnBased, -5f, TestName = "TurnBased with negative corrects to 1")]
		public void Constructor_WithInvalidLifetime_CorrectsToMinimum(LifetimeType lifetimeType, float invalidLifetime)
		{
			var config = CreateConfig(lifetimeType: lifetimeType, lifetime: invalidLifetime);

			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			Assert.AreEqual(1f, buff.RemainingLifetime);
			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void Constructor_WithPermanentAndZeroLifetime_KeepsZero()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);

			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			Assert.AreEqual(0f, buff.RemainingLifetime);
			Assert.IsFalse(buff.IsExpired);
		}
	#endregion

	#region RefreshLifetime Tests
		[Test]
		public void RefreshLifetime_UpdatesRemainingLifetimeAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustLifetime(-5f); // 剩餘 5
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.RefreshLifetime();

			Assert.AreEqual(10f, buff.RemainingLifetime);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void RefreshLifetime_WithPermanent_DoesNothing()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.RefreshLifetime();

			Assert.AreEqual(0f, buff.RemainingLifetime);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void RefreshLifetime_WhenExpired_DoesNothing()
		{
			var config = CreateConfig(lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustLifetime(-10f); // 過期
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.RefreshLifetime();

			Assert.IsTrue(buff.IsExpired);
			onChanged.DidNotReceive().Invoke();
		}
	#endregion

	#region SetLifetime Tests
		[Test]
		public void SetLifetime_UpdatesRemainingLifetimeAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.SetLifetime(5f);

			Assert.AreEqual(5f, buff.RemainingLifetime);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void SetLifetime_WithPermanent_DoesNothing()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.SetLifetime(10f);

			Assert.AreEqual(0f, buff.RemainingLifetime);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void SetLifetime_WithTurnBased_TruncatesToIntegerAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.TurnBased, lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.SetLifetime(3.7f);

			Assert.AreEqual(3f, buff.RemainingLifetime);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void SetLifetime_WhenExpired_DoesNothing()
		{
			var config = CreateConfig(lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustLifetime(-10f); // 過期
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.SetLifetime(10f);

			Assert.IsTrue(buff.IsExpired);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void SetLifetime_ToZeroOrNegative_TriggersOnExpired()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.SetLifetime(-1f);

			onExpired.Received(1).Invoke();
		}
	#endregion

	#region AdjustLifetime Tests
		[Test]
		public void AdjustLifetime_UpdatesRemainingLifetimeAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustLifetime(3f);

			Assert.AreEqual(13f, buff.RemainingLifetime);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void AdjustLifetime_WithPermanent_DoesNothing()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustLifetime(3f);

			Assert.AreEqual(0f, buff.RemainingLifetime);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void AdjustLifetime_WithZeroDelta_DoesNothing()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustLifetime(0f);

			Assert.AreEqual(10f, buff.RemainingLifetime);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void AdjustLifetime_WhenExpired_DoesNothing()
		{
			var config = CreateConfig(lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustLifetime(-10f); // 過期
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustLifetime(5f);

			Assert.IsTrue(buff.IsExpired);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void AdjustLifetime_WithTurnBased_TruncatesToIntegerAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.TurnBased, lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustLifetime(-2.7f);

			Assert.AreEqual(3f, buff.RemainingLifetime);
			onChanged.Received(1).Invoke();
		}
	#endregion

	#region OnExpired Event Tests
		[TestCase(LifetimeType.TimeBased, 10f, -15f, TestName = "TimeBased triggers when lifetime depleted")]
		[TestCase(LifetimeType.TurnBased, 3f, -5f, TestName = "TurnBased triggers when lifetime depleted")]
		public void AdjustLifetime_WhenBecomesExpired_TriggersOnExpired(LifetimeType type, float initialLifetime, float delta)
		{
			var config = CreateConfig(lifetimeType: type, lifetime: initialLifetime);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(delta);

			onExpired.Received(1).Invoke();
		}

		[TestCase(LifetimeType.TimeBased, 10f, -5f, TestName = "TimeBased does not trigger when still valid")]
		[TestCase(LifetimeType.TurnBased, 5f, -2f, TestName = "TurnBased does not trigger when still valid")]
		[TestCase(LifetimeType.Permanent, 0f, -10f, TestName = "Permanent never triggers")]
		public void AdjustLifetime_WhenNotExpired_DoesNotTriggerOnExpired(LifetimeType type, float initialLifetime, float delta)
		{
			var config = CreateConfig(lifetimeType: type, lifetime: initialLifetime);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(delta);

			onExpired.DidNotReceive().Invoke();
		}

		[Test]
		public void AdjustLifetime_WhenAlreadyExpired_DoesNotTriggerAgain()
		{
			var config = CreateConfig(lifetime: 5f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(-10f);
			buff.AdjustLifetime(-5f);

			onExpired.Received(1).Invoke();
		}

		[Test]
		public void AdjustLifetime_WhenExpired_WithRemoveAllOnExpireFalse_AndMultipleStacks_RemovesOneStackAndRefreshes()
		{
			var config = CreateConfig(lifetime: 5f, removeAllOnExpire: false);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(1); // stack = 2
			StackRecord removedRecord = null;
			buff.StackRecords.ObserveRemove().Subscribe(e => removedRecord = e.Value);
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(-10f);

			Assert.AreEqual(1, buff.StackCount);
			Assert.AreEqual(5f, buff.RemainingLifetime);
			Assert.IsNotNull(removedRecord);
			Assert.IsFalse(buff.IsExpired);
			onExpired.DidNotReceive().Invoke();
		}

		[Test]
		public void AdjustLifetime_WhenExpired_WithRemoveAllOnExpireTrue_TriggersOnExpired()
		{
			var config = CreateConfig(lifetime: 5f, removeAllOnExpire: true);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(1); // stack = 2
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(-10f);

			onExpired.Received(1).Invoke();
		}

		[Test]
		public void AdjustLifetime_WhenExpired_WithRemoveAllOnExpireFalse_AndSingleStack_TriggersOnExpired()
		{
			var config = CreateConfig(lifetime: 5f, removeAllOnExpire: false);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(-10f);

			onExpired.Received(1).Invoke();
		}
	#endregion

	#region IsExpired Tests
		[TestCase(LifetimeType.TimeBased, -1f, TestName = "TimeBased negative corrected to 1")]
		[TestCase(LifetimeType.TurnBased, -1f, TestName = "TurnBased negative corrected to 1")]
		public void IsExpired_WithNegativeLifetime_CorrectedToOneAndNotExpired(LifetimeType type, float lifetime)
		{
			var config = CreateConfig(lifetimeType: type, lifetime: lifetime);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			Assert.AreEqual(1f, buff.RemainingLifetime);
			Assert.IsFalse(buff.IsExpired);
		}

		[TestCase(LifetimeType.Permanent, 0f, TestName = "Permanent with zero")]
		[TestCase(LifetimeType.Permanent, -1f, TestName = "Permanent with negative")]
		public void IsExpired_WithPermanentLifetime_NeverExpires(LifetimeType type, float lifetime)
		{
			var config = CreateConfig(lifetimeType: type, lifetime: lifetime);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			Assert.IsFalse(buff.IsExpired);
		}

		[Test]
		public void IsExpired_WhenStackDepleted_ReturnsTrueAndTriggersOnExpired()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustStack(-1);

			Assert.IsTrue(buff.IsExpired);
			onExpired.Received(1).Invoke();
		}
	#endregion

	#region StackRecord Tests
		[Test]
		public void AddStackRecord_AddsRecordAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onAdd = Substitute.For<Action>();
			buff.StackRecords.ObserveAdd().Subscribe(_ => onAdd());
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustStack(1);

			onAdd.Received(1).Invoke();
			buff.StackRecords.Should().HaveCount(2);
			buff.StackRecords[1].Effects.Should().BeEquivalentTo(config.Effects);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void AddStackRecord_WhenAtMaxStack_DoesNotAddAndNoOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f, maxStack: 2);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(1); // stack = 2
			var onAdd = Substitute.For<Action>();
			buff.StackRecords.ObserveAdd().Subscribe(_ => onAdd());
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustStack(1); // 嘗試第三層

			onAdd.DidNotReceive().Invoke();
			buff.StackRecords.Should().HaveCount(2);
			onChanged.DidNotReceive().Invoke();
		}

		[Test]
		public void RemoveLastStackRecord_RemovesRecordAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(1); // stack = 2
			var onRemove = Substitute.For<Action>();
			buff.StackRecords.ObserveRemove().Subscribe(_ => onRemove());
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustStack(-1);

			onRemove.Received(1).Invoke();
			buff.StackRecords.Should().HaveCount(1);
			buff.StackRecords[0].Effects.Should().BeEquivalentTo(config.Effects);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void RemoveLastStackRecord_WithCount_RemovesMultipleRecordsAndTriggersOnChanged()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(1);
			buff.AdjustStack(1);
			var onRemove = Substitute.For<Action>();
			buff.StackRecords.ObserveRemove().Subscribe(_ => onRemove());
			var onChanged = Substitute.For<Action>();
			buff.OnChanged += onChanged;

			buff.AdjustStack(-2);

			onRemove.Received(2).Invoke();
			Assert.AreEqual(1, buff.StackCount);
			onChanged.Received(1).Invoke();
		}

		[Test]
		public void RemoveLastStackRecord_WhenOnlyOneStack_BecomesEmptyAndTriggersOnExpired()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustStack(-1);

			Assert.AreEqual(0, buff.StackCount);
			onExpired.Received(1).Invoke();
		}

		[Test]
		public void ClearStacks_ClearsAllStacksAndTriggersObserveReset_WithoutTriggeringOnExpired()
		{
			var config = CreateConfig(lifetimeType: LifetimeType.Permanent, lifetime: 0f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			buff.AdjustStack(2);
			var onExpired = Substitute.For<Action>();
			var onReset = Substitute.For<Action>();
			buff.OnExpired += onExpired;
			buff.StackRecords.ObserveReset().Subscribe(_ => onReset());

			buff.ClearStacks();

			Assert.AreEqual(0, buff.StackCount);
			onReset.Received(1).Invoke();
			onExpired.DidNotReceive().Invoke();
		}
	#endregion

	#region Edge Cases
		[Test]
		public void AdjustLifetime_WithLargePositiveDelta_IncreasesLifetime()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");

			buff.AdjustLifetime(float.MaxValue);

			Assert.IsTrue(buff.RemainingLifetime > 0);
		}

		[Test]
		public void AdjustLifetime_WithNegativeInfinity_TriggersExpired()
		{
			var config = CreateConfig(lifetime: 10f);
			var buff = new Buff("buff-1", config, "owner-1", "source-1");
			var onExpired = Substitute.For<Action>();
			buff.OnExpired += onExpired;

			buff.AdjustLifetime(float.NegativeInfinity);

			onExpired.Received(1).Invoke();
		}
	#endregion
	}
}