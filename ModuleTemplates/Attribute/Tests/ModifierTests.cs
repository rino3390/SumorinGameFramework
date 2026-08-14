using System;
using FluentAssertions;
using NUnit.Framework;

namespace Sumorin.Attribute.Tests
{
	[TestFixture]
	public class ModifierTests
	{
		[Test]
		public void Constructor_WithValidParameters_SetsAllProperties()
		{
			var modifier = new Modifier("mod-1", ModifyType.Flat, 10, "source-1", "Test modifier");

			modifier.Should()
					.BeEquivalentTo(
						new
						{
							Id = "mod-1",
							ModifyType = ModifyType.Flat,
							Value = 10,
							SourceId = "source-1",
							Description = "Test modifier"
						}
					);
		}

		[TestCase(null, "source-1", "id", TestName = "id 為 null 時拋出")]
		[TestCase("", "source-1", "id", TestName = "id 為空字串時拋出")]
		[TestCase("mod-1", null, "sourceId", TestName = "sourceId 為 null 時拋出")]
		[TestCase("mod-1", "", "sourceId", TestName = "sourceId 為空字串時拋出")]
		public void Constructor_WithMissingRequiredId_ThrowsArgumentException(string id, string sourceId, string paramName)
		{
			Action act = () => _ = new Modifier(id, ModifyType.Flat, 10, sourceId);

			act.Should().ThrowExactly<ArgumentException>().WithParameterName(paramName);
		}

		[Test]
		public void Constructor_WithNullDescription_SetsEmptyString()
		{
			var modifier = new Modifier("mod-1", ModifyType.Flat, 10, "source-1", null);

			modifier.Description.Should().BeEmpty();
		}
	}
}