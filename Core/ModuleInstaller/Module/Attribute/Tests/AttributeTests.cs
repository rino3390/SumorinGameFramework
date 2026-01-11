using NUnit.Framework;
using Rino.GameFramework.Core.AttributeSystem.Common;
using Rino.GameFramework.Core.AttributeSystem.Model;

namespace Rino.GameFramework.Core.AttributeSystem.Tests
{
    [TestFixture]
    public class AttributeTests
    {
        [Test]
        public void Constructor_WithValidParameters_SetsAllProperties()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);

            Assert.AreEqual("attr-1", attribute.Id);
            Assert.AreEqual("owner-1", attribute.OwnerId);
            Assert.AreEqual("Health", attribute.AttributeName);
            Assert.AreEqual(100, attribute.BaseValue);
            Assert.AreEqual(0, attribute.MinValue);
            Assert.AreEqual(999, attribute.MaxValue);
            Assert.IsEmpty(attribute.Modifiers);
        }

        [Test]
        public void Value_WithNoModifiers_ReturnsBaseValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);

            Assert.AreEqual(100, attribute.Value);
        }

        [Test]
        public void Value_WithBaseValueExceedsMax_ReturnsMaxValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 1000, 0, 500);

            Assert.AreEqual(500, attribute.Value);
        }

        [Test]
        public void Value_WithBaseValueBelowMin_ReturnsMinValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", -10, 0, 999);

            Assert.AreEqual(0, attribute.Value);
        }

        [Test]
        public void SetBaseValue_UpdatesBaseValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);

            attribute.SetBaseValue(150);

            Assert.AreEqual(150, attribute.BaseValue);
            Assert.AreEqual(150, attribute.Value);
        }

        [Test]
        public void SetMinValue_UpdatesMinValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 50, 0, 999);

            attribute.SetMinValue(100);

            Assert.AreEqual(100, attribute.MinValue);
            Assert.AreEqual(100, attribute.Value);
        }

        [Test]
        public void SetMaxValue_UpdatesMaxValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 500, 0, 999);

            attribute.SetMaxValue(300);

            Assert.AreEqual(300, attribute.MaxValue);
            Assert.AreEqual(300, attribute.Value);
        }

        [Test]
        public void AddModifier_Flat_AddsToBaseValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");

            attribute.AddModifier(modifier);

            Assert.AreEqual(150, attribute.Value);
            Assert.AreEqual(1, attribute.Modifiers.Count);
        }

        [Test]
        public void AddModifier_Percent_AddsPercentageOfFlat()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var modifier = new Modifier("mod-1", ModifyType.Percent, 50, "source-1");

            attribute.AddModifier(modifier);

            Assert.AreEqual(150, attribute.Value);
        }

        [Test]
        public void AddModifier_Multiple_MultipliesResult()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var modifier = new Modifier("mod-1", ModifyType.Multiple, 2, "source-1");

            attribute.AddModifier(modifier);

            Assert.AreEqual(200, attribute.Value);
        }

        [Test]
        public void AddModifier_Combined_CalculatesInCorrectOrder()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 9999);
            var flatMod = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            var percentMod = new Modifier("mod-2", ModifyType.Percent, 50, "source-1");
            var multipleMod = new Modifier("mod-3", ModifyType.Multiple, 2, "source-1");

            attribute.AddModifier(flatMod);
            attribute.AddModifier(percentMod);
            attribute.AddModifier(multipleMod);

            // Flat: 100 + 50 = 150
            // Percent: 150 + 150 * 50% = 150 + 75 = 225
            // Multiple: 225 * 2 = 450
            Assert.AreEqual(450, attribute.Value);
        }

        [Test]
        public void AddModifier_ResultClamped_ToMinMax()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 200);
            var modifier = new Modifier("mod-1", ModifyType.Multiple, 3, "source-1");

            attribute.AddModifier(modifier);

            Assert.AreEqual(200, attribute.Value);
        }

        [Test]
        public void RemoveModifierById_RemovesSpecificModifier()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var mod1 = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            var mod2 = new Modifier("mod-2", ModifyType.Flat, 30, "source-1");
            attribute.AddModifier(mod1);
            attribute.AddModifier(mod2);

            attribute.RemoveModifierById("mod-1");

            Assert.AreEqual(1, attribute.Modifiers.Count);
            Assert.AreEqual(130, attribute.Value);
        }

        [Test]
        public void RemoveModifierById_NonExistentId_DoesNothing()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            attribute.AddModifier(modifier);

            attribute.RemoveModifierById("non-existent");

            Assert.AreEqual(1, attribute.Modifiers.Count);
            Assert.AreEqual(150, attribute.Value);
        }

        [Test]
        public void RemoveModifiersBySource_RemovesAllFromSource()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var mod1 = new Modifier("mod-1", ModifyType.Flat, 50, "sword-1");
            var mod2 = new Modifier("mod-2", ModifyType.Flat, 30, "sword-1");
            var mod3 = new Modifier("mod-3", ModifyType.Flat, 20, "armor-1");
            attribute.AddModifier(mod1);
            attribute.AddModifier(mod2);
            attribute.AddModifier(mod3);

            attribute.RemoveModifiersBySource("sword-1");

            Assert.AreEqual(1, attribute.Modifiers.Count);
            Assert.AreEqual(120, attribute.Value);
        }

        [Test]
        public void RemoveModifiersBySource_NonExistentSource_DoesNothing()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var modifier = new Modifier("mod-1", ModifyType.Flat, 50, "source-1");
            attribute.AddModifier(modifier);

            attribute.RemoveModifiersBySource("non-existent");

            Assert.AreEqual(1, attribute.Modifiers.Count);
            Assert.AreEqual(150, attribute.Value);
        }

        [Test]
        public void AddModifier_MultipleFlatModifiers_AccumulatesCorrectly()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, 10, "source-1"));
            attribute.AddModifier(new Modifier("mod-2", ModifyType.Flat, 20, "source-1"));
            attribute.AddModifier(new Modifier("mod-3", ModifyType.Flat, 30, "source-1"));

            Assert.AreEqual(160, attribute.Value);
        }

        [Test]
        public void AddModifier_MultiplePercentModifiers_AccumulatesCorrectly()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Percent, 10, "source-1"));
            attribute.AddModifier(new Modifier("mod-2", ModifyType.Percent, 20, "source-1"));

            // Percent: 100 + 100 * 30% = 130
            Assert.AreEqual(130, attribute.Value);
        }

        [Test]
        public void AddModifier_MultipleMultipleModifiers_MultipliesInSequence()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 9999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Multiple, 2, "source-1"));
            attribute.AddModifier(new Modifier("mod-2", ModifyType.Multiple, 3, "source-1"));

            // Multiple: 100 * 2 * 3 = 600
            Assert.AreEqual(600, attribute.Value);
        }

        [Test]
        public void AddModifier_NegativeFlatModifier_DecreasesValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Flat, -30, "source-1"));

            Assert.AreEqual(70, attribute.Value);
        }

        [Test]
        public void AddModifier_NegativePercentModifier_DecreasesValue()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Percent, -20, "source-1"));

            // Percent: 100 + 100 * (-20%) = 80
            Assert.AreEqual(80, attribute.Value);
        }

        [TestCase(int.MaxValue, 0, int.MaxValue, int.MaxValue)]
        [TestCase(int.MinValue, int.MinValue, 0, int.MinValue)]
        [TestCase(0, -100, 100, 0)]
        public void Value_WithExtremeBaseValue_ClampsCorrectly(int baseValue, int minValue, int maxValue, int expected)
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", baseValue, minValue, maxValue);

            Assert.AreEqual(expected, attribute.Value);
        }

        [TestCase(ModifyType.Multiple, 2, int.MaxValue, 0, int.MaxValue, int.MaxValue)]
        [TestCase(ModifyType.Percent, 100, int.MaxValue, 0, int.MaxValue, int.MaxValue)]
        [TestCase(ModifyType.Multiple, -2, 100, -999, 999, -200)]
        [TestCase(ModifyType.Multiple, 0, 100, 0, 999, 0)]
        public void AddModifier_WithExtremeValue_HandlesCorrectly(ModifyType type, int modValue, int baseValue, int minValue, int maxValue, int expected)
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", baseValue, minValue, maxValue);
            attribute.AddModifier(new Modifier("mod-1", type, modValue, "source-1"));

            Assert.AreEqual(expected, attribute.Value);
        }

        [TestCase(100, 15, 115)]
        [TestCase(101, 5, 106)]
        [TestCase(99, 10, 109)]
        public void Value_WithRounding_RoundsAwayFromZero(int baseValue, int percentValue, int expected)
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", baseValue, 0, 999);
            attribute.AddModifier(new Modifier("mod-1", ModifyType.Percent, percentValue, "source-1"));

            Assert.AreEqual(expected, attribute.Value);
        }
    }
}
