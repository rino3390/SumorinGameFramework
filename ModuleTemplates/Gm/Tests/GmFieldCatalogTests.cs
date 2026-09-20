using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.DDDCore;
using Sumorin.SumorinUtility;
using UnityEngine;

namespace Sumorin.Gm.Tests
{
	[TestFixture]
	public class GmFieldCatalogTests
	{
		private GmFieldCatalog catalog;

		// 只驗「哪個型別挑到哪個建構器」。欄位取值與下拉內容屬於 View，靠測試 Scene 目視
		private static IEnumerable<TestCaseData> BuiltInTypes()
		{
			yield return new TestCaseData(typeof(int), GmBuiltInFields.Int).SetName("整數");
			yield return new TestCaseData(typeof(float), GmBuiltInFields.Float).SetName("浮點數");
			yield return new TestCaseData(typeof(bool), GmBuiltInFields.Bool).SetName("布林");
			yield return new TestCaseData(typeof(string), GmBuiltInFields.String).SetName("字串");
			yield return new TestCaseData(typeof(TestTarget), GmBuiltInFields.Enum).SetName("列舉");
		}

		[SetUp]
		public void Setup()
		{
			catalog = new(new ConfigManager(Array.Empty<IConfig>()));
		}

		private static GmOperation OperationWith(params GmParameter[] parameters) => new("戰鬥/生成", parameters, _ => CommandResult.Ok());

		[TestCaseSource(nameof(BuiltInTypes))]
		public void Resolve_ForBuiltInType_ReturnsThatTypesBuilder(Type valueType, GmFieldBuilder expected)
		{
			var parameter = new GmParameter("參數", valueType);

			var resolved = catalog.Resolve(OperationWith(parameter), parameter);

			resolved.Should().BeSameAs(expected);
		}

		[Test]
		public void Resolve_ForConfigIdType_ReturnsTheConfigIdFamilyBuilder()
		{
			var parameter = new GmParameter("configId", typeof(GmConfigId<ITestConfig>));

			var resolved = catalog.Resolve(OperationWith(parameter), parameter);

			// 配置 Id 的建構器綁著 ConfigManager，每個目錄一份，比不了身分。
			// 比得出「有挑到東西」就夠，家族比對本身由 GmConfigIdLookupTests 驗
			resolved.Should().NotBeNull();
		}

		[Test]
		public void Resolve_WithGameRegisteredType_PrefersGameBuilderOverBuiltIn()
		{
			GmFieldBuilder custom = (_, _, _) => Substitute.For<IGmField>();
			catalog.Register(typeof(int), custom);
			var parameter = new GmParameter("amount", typeof(int));

			var resolved = catalog.Resolve(OperationWith(parameter), parameter);

			resolved.Should().BeSameAs(custom);
		}

		[Test]
		public void Resolve_WithFieldRegistration_UsesTheBuilderThatRegistrationAdded()
		{
			var withRegistration = new GmFieldCatalog(new ConfigManager(Array.Empty<IConfig>()), new[] { new FakeFieldRegistration() });
			var parameter = new GmParameter("target", typeof(FakeTargetId));

			var resolved = withRegistration.Resolve(OperationWith(parameter), parameter);

			resolved.Should().BeSameAs(FakeFieldRegistration.FieldBuilder);
		}

		[Test]
		public void Resolve_WithUnsupportedType_ThrowsWithOperationParameterAndTypeName()
		{
			var parameter = new GmParameter("position", typeof(Vector3));
			var operation = OperationWith(parameter);

			Action act = () => catalog.Resolve(operation, parameter);

			act.Should().ThrowExactly<InvalidOperationException>().WithMessage("*戰鬥/生成*position*Vector3*");
		}

	#region Nested Types
		private enum TestTarget
		{
			Player,
			Enemy
		}

		private interface ITestConfig: IConfig { }

		private class FakeTargetId { }

		private class FakeFieldRegistration: IGmFieldRegistration
		{
			public static readonly GmFieldBuilder FieldBuilder = (_, _, _) => Substitute.For<IGmField>();

		#region IGmFieldRegistration Members
			public void Register(IGmFieldCatalog catalog) => catalog.Register(typeof(FakeTargetId), FieldBuilder);
		#endregion
		}
	#endregion
	}
}