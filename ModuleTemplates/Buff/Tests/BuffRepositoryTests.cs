using FluentAssertions;
using NUnit.Framework;

namespace Sumorin.Buff.Tests
{
	[TestFixture]
	public class BuffRepositoryTests
	{
		private BuffRepository repository;

		[SetUp]
		public void Setup()
		{
			repository = new BuffRepository();
			repository.Save(new Buff("buff-1", new FakeBuffConfig(id: "Poison"), "owner-1", "source-1"));
			repository.Save(new Buff("buff-2", new FakeBuffConfig(id: "Burn"), "owner-1", "source-2"));
			repository.Save(new Buff("buff-3", new FakeBuffConfig(id: "Poison"), "owner-2", "source-1"));
		}

		[Test]
		public void Get_WithExistingId_ReturnsBuff()
		{
			repository.Get("buff-2").ConfigId.Should().Be("Burn");
		}

		[Test]
		public void Get_WithUnknownId_ReturnsNull()
		{
			repository.Get("buff-404").Should().BeNull();
		}

		[Test]
		public void GetByOwner_WithMultipleOwners_ReturnsOnlyThatOwner()
		{
			repository.GetByOwner("owner-1")
					  .Should()
					  .BeEquivalentTo(
						  new[]
						  {
							  new { Id = "buff-1" },
							  new { Id = "buff-2" }
						  }
					  );
		}

		[Test]
		public void GetByOwner_WithUnknownOwner_ReturnsEmpty()
		{
			repository.GetByOwner("owner-404").Should().BeEmpty();
		}
	}
}