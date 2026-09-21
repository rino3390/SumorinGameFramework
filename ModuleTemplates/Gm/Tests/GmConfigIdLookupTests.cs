using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Sumorin.SumorinUtility;
using UnityEngine;

namespace Sumorin.Gm.Tests
{
	[TestFixture]
	public class GmConfigIdLookupTests
	{
		[TestCase(typeof(GmConfigId<ITestConfig>), true, TestName = "配置 Id 的封閉泛型算同一族")]
		[TestCase(typeof(string), false, TestName = "非泛型型別不算")]
		[TestCase(typeof(Vector3), false, TestName = "其他結構不算")]
		[TestCase(typeof(List<int>), false, TestName = "其他泛型不算")]
		public void Matches_TellsConfigIdTypesApart(Type type, bool expected)
		{
			GmConfigIdLookup.Matches(type).Should().Be(expected);
		}

		[Test]
		public void CandidatesOf_ListsOnlyIdsOfThatConfigType()
		{
			var configs = new ConfigManager(
				new IConfig[]
				{
					new FakeOtherConfig { Id = "other-1" },
					new FakeTestConfig { Id = "target-1" },
					new FakeTestConfig { Id = "target-2" }
				}
			);

			var candidates = GmConfigIdLookup.CandidatesOf(typeof(GmConfigId<ITestConfig>), configs);

			candidates.Should().BeEquivalentTo("target-1", "target-2");
		}

		[Test]
		public void CandidatesOf_WithNoMatchingConfig_ReturnsEmpty()
		{
			var configs = new ConfigManager(new IConfig[] { new FakeOtherConfig { Id = "other-1" } });

			var candidates = GmConfigIdLookup.CandidatesOf(typeof(GmConfigId<ITestConfig>), configs);

			candidates.Should().BeEmpty();
		}

		[Test]
		public void Wrap_PutsTheIdBackIntoItsConfigIdType()
		{
			var wrapped = GmConfigIdLookup.Wrap(typeof(GmConfigId<ITestConfig>), "target-1");

			wrapped.Should().BeOfType<GmConfigId<ITestConfig>>();
			((GmConfigId<ITestConfig>)wrapped).Value.Should().Be("target-1");
		}

	#region Nested Types
		public interface ITestConfig: IConfig { }

		private interface IOtherConfig: IConfig { }

		private class FakeTestConfig: ITestConfig
		{
			public string Id { get; set; }
		}

		private class FakeOtherConfig: IOtherConfig
		{
			public string Id { get; set; }
		}
	#endregion
	}
}