using NUnit.Framework;

namespace Sumorin.AttributeSystem.Tests
{
    [TestFixture]
    public class ModifierTests
    {
        [Test]
        public void Constructor_WithValidParameters_SetsAllProperties()
        {
            var modifier = new Modifier("mod-1", ModifyType.Flat, 10, "source-1", "Test modifier");

            Assert.AreEqual("mod-1", modifier.Id);
            Assert.AreEqual(ModifyType.Flat, modifier.ModifyType);
            Assert.AreEqual(10, modifier.Value);
            Assert.AreEqual("source-1", modifier.SourceId);
            Assert.AreEqual("Test modifier", modifier.Description);
        }

        #region 邊界值測試

        [TestCase(0)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void Constructor_WithBoundaryValues_AcceptsValue(int value)
        {
            var modifier = new Modifier("mod-1", ModifyType.Flat, value, "source-1");

            Assert.AreEqual(value, modifier.Value);
        }

        #endregion

        #region 參數驗證測試

        [TestCase(null)]
        [TestCase("")]
        public void Constructor_WithNullOrEmptyId_ThrowsArgumentException(string id)
        {
            Assert.That(() => new Modifier(id, ModifyType.Flat, 10, "source-1"),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("id"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void Constructor_WithNullOrEmptySourceId_ThrowsArgumentException(string sourceId)
        {
            Assert.That(() => new Modifier("mod-1", ModifyType.Flat, 10, sourceId),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("sourceId"));
        }

        [Test]
        public void Constructor_WithNullDescription_SetsEmptyString()
        {
            var modifier = new Modifier("mod-1", ModifyType.Flat, 10, "source-1", null);

            Assert.AreEqual("", modifier.Description);
        }

        #endregion

        #region 特殊字元測試

        [TestCase("😀emoji")]
        [TestCase("Line1\nLine2")]
        [TestCase("Tab\there")]
        [TestCase("特殊!@#$%^&*()字元")]
        public void Constructor_WithSpecialCharactersInDescription_AcceptsValue(string description)
        {
            var modifier = new Modifier("mod-1", ModifyType.Flat, 10, "source-1", description);

            Assert.AreEqual(description, modifier.Description);
        }

        #endregion

        #region ModifyType 完整性測試

        [TestCase(ModifyType.Flat)]
        [TestCase(ModifyType.Percent)]
        [TestCase(ModifyType.Multiple)]
        public void Constructor_WithAllModifyTypes_SetsCorrectType(ModifyType modifyType)
        {
            var modifier = new Modifier("mod-1", modifyType, 10, "source-1");

            Assert.AreEqual(modifyType, modifier.ModifyType);
        }

        #endregion
    }
}
