using FluentAssertions;
using NUnit.Framework;

namespace Sumorin.Attribute.Tests
{
	[TestFixture]
	public class AttributeRepositoryTests
	{
		private AttributeRepository repository;

		[SetUp]
		public void SetUp()
		{
			repository = new AttributeRepository();
			repository.Save(new Attribute("attr-1", "owner-1", "Health", 100, 0, 999));
			repository.Save(new Attribute("attr-2", "owner-1", "Attack", 50, 0, 999));
			repository.Save(new Attribute("attr-3", "owner-2", "Health", 200, 0, 999));
		}

		[TestCase("owner-1", "Health", "attr-1", TestName = "命中第一位擁有者的屬性")]
		[TestCase("owner-1", "Attack", "attr-2", TestName = "同一擁有者的不同屬性各自可取")]
		[TestCase("owner-2", "Health", "attr-3", TestName = "同名屬性依擁有者區分")]
		public void Get_WithMatchingOwnerAndName_ReturnsAttribute(string ownerId, string attributeName, string expectedId)
		{
			repository.Get(ownerId, attributeName).Id.Should().Be(expectedId);
		}

		[TestCase("owner-3", "Health", TestName = "擁有者不存在回傳 null")]
		[TestCase("owner-1", "Defense", TestName = "屬性名稱不存在回傳 null")]
		[TestCase("owner-2", "Attack", TestName = "擁有者存在但無該屬性回傳 null")]
		public void Get_WithUnmatchedOwnerOrName_ReturnsNull(string ownerId, string attributeName)
		{
			repository.Get(ownerId, attributeName).Should().BeNull();
		}

		[Test]
		public void GetByOwnerId_WithMultipleOwners_ReturnsOnlyThatOwner()
		{
			var results = repository.GetByOwnerId("owner-1");

			results.Should()
				   .BeEquivalentTo(
					   new[]
					   {
						   new { Id = "attr-1" },
						   new { Id = "attr-2" }
					   }
				   );
		}

		[Test]
		public void GetByOwnerId_WithUnknownOwner_ReturnsEmpty()
		{
			repository.GetByOwnerId("owner-3").Should().BeEmpty();
		}

		[Test]
		public void DeleteByOwnerId_WithMultipleOwners_RemovesOnlyThatOwner()
		{
			repository.DeleteByOwnerId("owner-1");

			repository.Values.Should().BeEquivalentTo(new[] { new { Id = "attr-3" } });
		}
	}
}