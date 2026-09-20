using System;
using FluentAssertions;
using NUnit.Framework;
using Sumorin.SumorinUtility;
using Sumorin.TestFramework;
using UnityEngine;
using VContainer;

namespace Sumorin.Gm.Tests
{
	[TestFixture]
	public class GmInstallerTests: VContainerUnitTestFixture
	{
		[TearDown]
		public override void Teardown()
		{
			base.Teardown();

			foreach(var panel in UnityEngine.Object.FindObjectsByType<GmPanelView>(FindObjectsSortMode.None))
			{
				UnityEngine.Object.DestroyImmediate(panel.gameObject);
			}
		}

		private void InstallGm(params Type[] fieldRegistrationTypes)
		{
			Builder.RegisterInstance(new ConfigManager(Array.Empty<IConfig>()));
			new GmInstaller<TestGmOperations>(fieldRegistrationTypes).Install(Builder);
		}

		[Test]
		public void Install_RegistersEverythingTheOperationsEntryNeeds()
		{
			InstallGm();

			Container.Invoking(c => c.Resolve<GmOperations>()).Should().NotThrow();
			Container.Resolve<GmOperations>().Should().BeOfType<TestGmOperations>();
		}

		[Test]
		public void Install_WithFieldRegistration_FeedsItIntoTheCatalog()
		{
			InstallGm(typeof(InjectedFieldRegistration));
			var parameter = new GmParameter("target", typeof(FakeTargetId));
			var operation = new GmOperation("戰鬥/生成", new[] { parameter }, _ => default);

			var resolved = Container.Resolve<GmFieldCatalog>().Resolve(operation, parameter);

			resolved.Should().BeSameAs(InjectedFieldRegistration.FieldBuilder);
		}

	#region Nested Types
		private class FakeTargetId { }

		private sealed class TestGmOperations: GmOperations
		{
			protected override void Define() { }
		}

		// 登記類別由容器建立，建構子相依解析得到才證明這條路通
		private class InjectedFieldRegistration: IGmFieldRegistration
		{
			public static readonly GmFieldBuilder FieldBuilder = (_, _, _) => null;

			private readonly ConfigManager configs;

			public InjectedFieldRegistration(ConfigManager configs)
			{
				this.configs = configs;
			}

		#region IGmFieldRegistration Members
			public void Register(IGmFieldCatalog catalog)
			{
				configs.Should().NotBeNull();
				catalog.Register(typeof(FakeTargetId), FieldBuilder);
			}
		#endregion
		}
	#endregion
	}
}
