using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using UniRx;

namespace Sumorin.Attribute.Tests
{
	[TestFixture]
	public class AttributeTests
	{
		private static Attribute CreateAttribute(int baseValue = 100, int min = 0, int max = 999) => new("attr-1", "owner-1", "Health", baseValue, min, max);

		[Test]
		public void Constructor_WithValidParameters_SetsAllProperties()
		{
			var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);

			attribute.Should()
					 .BeEquivalentTo(
						 new
						 {
							 Id = "attr-1",
							 OwnerId = "owner-1",
							 AttributeName = "Health",
							 BaseValue = 100,
							 MinValue = 0,
							 MaxValue = 999,
							 Value = 100,
							 Modifiers = new List<Modifier>()
						 }
					 );
		}

		[TestCase(100, 0, 999, 100, TestName = "基礎值在範圍內取原值")]
		[TestCase(1000, 0, 500, 500, TestName = "基礎值超過上限取上限")]
		[TestCase(-10, 0, 999, 0, TestName = "基礎值低於下限取下限")]
		[TestCase(int.MaxValue, 0, int.MaxValue, int.MaxValue, TestName = "基礎值為 int 上限不溢位")]
		[TestCase(int.MinValue, int.MinValue, 0, int.MinValue, TestName = "基礎值為 int 下限不溢位")]
		public void Value_WithoutModifiers_ClampsToBoundary(int baseValue, int min, int max, int expected)
		{
			CreateAttribute(baseValue, min, max).Value.Should().Be(expected);
		}

		[Test]
		public void SetBaseValue_WithNewValue_UpdatesBaseValueAndValue()
		{
			var attribute = CreateAttribute();

			attribute.SetBaseValue(150);

			attribute.Should().BeEquivalentTo(new { BaseValue = 150, Value = 150 });
		}

		[Test]
		public void SetMinValue_AboveCurrentValue_RaisesValueToMin()
		{
			var attribute = CreateAttribute(50);

			attribute.SetMinValue(100);

			attribute.Should().BeEquivalentTo(new { MinValue = 100, Value = 100 });
		}

		[Test]
		public void SetMaxValue_BelowCurrentValue_LowersValueToMax()
		{
			var attribute = CreateAttribute(500, 0, 999);

			attribute.SetMaxValue(300);

			attribute.Should().BeEquivalentTo(new { MaxValue = 300, Value = 300 });
		}

		[TestCase(ModifyType.Flat, 50, 150, TestName = "Flat 固定值加減")]
		[TestCase(ModifyType.Percent, 50, 150, TestName = "Percent 百分比加減")]
		[TestCase(ModifyType.Multiple, 2, 200, TestName = "Multiple 倍率相乘")]
		[TestCase(ModifyType.Flat, -30, 70, TestName = "Flat 負值減少")]
		[TestCase(ModifyType.Percent, -20, 80, TestName = "Percent 負值減少")]
		public void AddModifier_WithSingleModifier_AppliesByModifyType(ModifyType modifyType, int value, int expected)
		{
			var attribute = CreateAttribute();

			attribute.AddModifier(new Modifier("mod-1", modifyType, value, "source-1"));

			attribute.Value.Should().Be(expected);
		}

		[Test]
		public void AddModifier_WithAllModifyTypes_AppliesFlatThenPercentThenMultiple()
		{
			var attribute = CreateAttribute(100, 0, 9999);

			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 50, "source-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Percent, 50, "source-1"));
			attribute.AddModifier(new Modifier("mod-3", ModifyType.Multiple, 2, "source-1"));

			// Flat 100+50=150 → Percent 150+150*50%=225 → Multiple 225*2=450
			attribute.Value.Should().Be(450);
		}

		[Test]
		public void AddModifier_WithMultipleFlatModifiers_AccumulatesAll()
		{
			var attribute = CreateAttribute();

			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 10, "source-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Flat, 20, "source-1"));
			attribute.AddModifier(new Modifier("mod-3", ModifyType.Flat, 30, "source-1"));

			attribute.Value.Should().Be(160);
		}

		[Test]
		public void AddModifier_WithMultiplePercentModifiers_SumsPercentBeforeApplying()
		{
			var attribute = CreateAttribute();

			attribute.AddModifier(new Modifier("mod-1", ModifyType.Percent, 10, "source-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Percent, 20, "source-1"));

			// 100 + 100*30% = 130，而非依序套用的 100*1.1*1.2 = 132
			attribute.Value.Should().Be(130);
		}

		[Test]
		public void AddModifier_WithMultipleMultipleModifiers_MultipliesInSequence()
		{
			var attribute = CreateAttribute(100, 0, 9999);

			attribute.AddModifier(new Modifier("mod-1", ModifyType.Multiple, 2, "source-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Multiple, 3, "source-1"));

			attribute.Value.Should().Be(600);
		}

		[TestCase(ModifyType.Multiple, 3, 100, 0, 200, 200, TestName = "相乘結果超過上限取上限")]
		[TestCase(ModifyType.Multiple, 2, int.MaxValue, 0, int.MaxValue, int.MaxValue, TestName = "相乘溢位取上限")]
		[TestCase(ModifyType.Percent, 100, int.MaxValue, 0, int.MaxValue, int.MaxValue, TestName = "百分比溢位取上限")]
		[TestCase(ModifyType.Multiple, -2, 100, -999, 999, -200, TestName = "負倍率得負值")]
		[TestCase(ModifyType.Multiple, 0, 100, 0, 999, 0, TestName = "零倍率歸零")]
		public void AddModifier_WithExtremeValue_ClampsWithoutOverflow(ModifyType modifyType, int modifierValue, int baseValue, int min, int max, int expected)
		{
			var attribute = CreateAttribute(baseValue, min, max);

			attribute.AddModifier(new Modifier("mod-1", modifyType, modifierValue, "source-1"));

			attribute.Value.Should().Be(expected);
		}

		[TestCase(10, 25, 0, 13, TestName = "正值中點 12.5 遠離零進位至 13")]
		[TestCase(-10, 25, -999, -13, TestName = "負值中點 -12.5 遠離零進位至 -13")]
		[TestCase(99, 10, 0, 109, TestName = "108.9 進位至 109")]
		public void Value_WithFractionalResult_RoundsAwayFromZero(int baseValue, int percentValue, int min, int expected)
		{
			var attribute = CreateAttribute(baseValue, min);

			attribute.AddModifier(new Modifier("mod-1", ModifyType.Percent, percentValue, "source-1"));

			attribute.Value.Should().Be(expected);
		}

		[Test]
		public void RemoveModifierById_WithExistingId_RemovesOnlyThatModifier()
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 50, "source-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Flat, 30, "source-1"));

			attribute.RemoveModifierById("mod-1");

			attribute.Modifiers.Should().BeEquivalentTo(new[] { new { Id = "mod-2" } });
			attribute.Value.Should().Be(130);
		}

		[Test]
		public void RemoveModifierById_WithUnknownId_KeepsAllModifiers()
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 50, "source-1"));

			attribute.RemoveModifierById("non-existent");

			attribute.Value.Should().Be(150);
		}

		[Test]
		public void RemoveModifiersBySource_WithMatchingSource_RemovesAllFromThatSource()
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 50, "sword-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Flat, 30, "sword-1"));
			attribute.AddModifier(new Modifier("mod-3", ModifyType.Flat, 20, "armor-1"));

			attribute.RemoveModifiersBySource("sword-1");

			attribute.Modifiers.Should().BeEquivalentTo(new[] { new { Id = "mod-3" } });
			attribute.Value.Should().Be(120);
		}

		[Test]
		public void RemoveModifiersBySource_WithUnknownSource_KeepsAllModifiers()
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 50, "source-1"));

			attribute.RemoveModifiersBySource("non-existent");

			attribute.Value.Should().Be(150);
		}

		[Test]
		public void RemoveFirstModifier_WithDuplicateModifiers_RemovesOnlyOne()
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 30, "buff-1"));
			attribute.AddModifier(new Modifier("mod-2", ModifyType.Flat, 30, "buff-1"));

			attribute.RemoveFirstModifier(ModifyType.Flat, 30, "buff-1");

			attribute.Modifiers.Should().BeEquivalentTo(new[] { new { Id = "mod-2" } });
			attribute.Value.Should().Be(130);
		}

		[TestCase(ModifyType.Percent, 30, "buff-1", TestName = "修改類型不符不移除")]
		[TestCase(ModifyType.Flat, 20, "buff-1", TestName = "數值不符不移除")]
		[TestCase(ModifyType.Flat, 30, "buff-2", TestName = "來源不符不移除")]
		public void RemoveFirstModifier_WithUnmatchedCondition_KeepsModifier(ModifyType modifyType, int value, string sourceId)
		{
			var attribute = CreateAttribute();
			attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 30, "buff-1"));

			attribute.RemoveFirstModifier(modifyType, value, sourceId);

			attribute.Value.Should().Be(130);
		}

		[Test]
		public void Current_OnSubscribe_EmitsCurrentSnapshot()
		{
			var attribute = CreateAttribute();
			AttributeValueInfo received = default;

			attribute.Current.Subscribe(info => received = info);

			received.Should().Be(new AttributeValueInfo(100, 0, 999));
		}

		[Test]
		public void Current_WhenValueChanges_EmitsNewSnapshot()
		{
			var attribute = CreateAttribute();
			var received = new List<AttributeValueInfo>();
			attribute.Current.Subscribe(info => received.Add(info));

			attribute.SetBaseValue(150);

			received.Should()
					.BeEquivalentTo(
						new[]
						{
							new AttributeValueInfo(100, 0, 999),
							new AttributeValueInfo(150, 0, 999)
						},
						options => options.WithStrictOrdering()
					);
		}

		[Test]
		public void Current_WhenBoundaryChangesWithoutValue_EmitsNewSnapshot()
		{
			var attribute = CreateAttribute();
			var emitCount = 0;
			attribute.Current.Subscribe(_ => emitCount++);

			attribute.SetMaxValue(500);

			emitCount.Should().Be(2);
		}

		[Test]
		public void Current_WhenNothingChanges_DoesNotEmitAgain()
		{
			var attribute = CreateAttribute();
			var emitCount = 0;
			attribute.Current.Subscribe(_ => emitCount++);

			attribute.SetBaseValue(100);

			emitCount.Should().Be(1);
		}

		[Test]
		public void Current_WhenClampedValueUnchanged_DoesNotEmitAgain()
		{
			var attribute = CreateAttribute(1000, 0, 500);
			var emitCount = 0;
			attribute.Current.Subscribe(_ => emitCount++);

			attribute.SetBaseValue(2000);

			emitCount.Should().Be(1);
		}
	}
}