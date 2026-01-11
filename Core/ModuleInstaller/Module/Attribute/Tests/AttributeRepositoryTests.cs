using NUnit.Framework;
using Rino.GameFramework.Core.AttributeSystem.Model;
using Rino.GameFramework.Core.AttributeSystem.Repository;

namespace Rino.GameFramework.Core.AttributeSystem.Tests
{
    [TestFixture]
    public class AttributeRepositoryTests
    {
        private AttributeRepository repository;

        [SetUp]
        public void SetUp()
        {
            repository = new AttributeRepository();
        }

        [Test]
        public void Get_WithOwnerIdAndAttributeName_ReturnsAttribute()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            repository.Save(attribute);

            var result = repository.Get("owner-1", "Health");

            Assert.AreEqual(attribute, result);
        }

        [Test]
        public void Get_NonExistentAttribute_ReturnsNull()
        {
            var result = repository.Get("owner-1", "Health");

            Assert.IsNull(result);
        }

        [Test]
        public void Get_WrongOwnerId_ReturnsNull()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            repository.Save(attribute);

            var result = repository.Get("owner-2", "Health");

            Assert.IsNull(result);
        }

        [Test]
        public void Get_WrongAttributeName_ReturnsNull()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            repository.Save(attribute);

            var result = repository.Get("owner-1", "Attack");

            Assert.IsNull(result);
        }

        [Test]
        public void Save_MultipleAttributesForSameOwner_AllRetrievable()
        {
            var health = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var attack = new Attribute("attr-2", "owner-1", "Attack", 50, 0, 999);
            repository.Save(health);
            repository.Save(attack);

            Assert.AreEqual(health, repository.Get("owner-1", "Health"));
            Assert.AreEqual(attack, repository.Get("owner-1", "Attack"));
        }

        [Test]
        public void Save_SameAttributeNameDifferentOwners_AllRetrievable()
        {
            var health1 = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var health2 = new Attribute("attr-2", "owner-2", "Health", 200, 0, 999);
            repository.Save(health1);
            repository.Save(health2);

            Assert.AreEqual(health1, repository.Get("owner-1", "Health"));
            Assert.AreEqual(health2, repository.Get("owner-2", "Health"));
        }

        [Test]
        public void GetByOwnerId_ReturnsAllAttributesForOwner()
        {
            var health = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var attack = new Attribute("attr-2", "owner-1", "Attack", 50, 0, 999);
            var other = new Attribute("attr-3", "owner-2", "Health", 200, 0, 999);
            repository.Save(health);
            repository.Save(attack);
            repository.Save(other);

            var results = repository.GetByOwnerId("owner-1");

            Assert.AreEqual(2, results.Count);
            Assert.Contains(health, results);
            Assert.Contains(attack, results);
        }

        [Test]
        public void GetByOwnerId_NonExistentOwner_ReturnsEmptyList()
        {
            var attribute = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            repository.Save(attribute);

            var results = repository.GetByOwnerId("owner-2");

            Assert.IsEmpty(results);
        }

        [Test]
        public void DeleteByOwnerId_RemovesAllAttributesForOwner()
        {
            var health = new Attribute("attr-1", "owner-1", "Health", 100, 0, 999);
            var attack = new Attribute("attr-2", "owner-1", "Attack", 50, 0, 999);
            var other = new Attribute("attr-3", "owner-2", "Health", 200, 0, 999);
            repository.Save(health);
            repository.Save(attack);
            repository.Save(other);

            repository.DeleteByOwnerId("owner-1");

            Assert.IsNull(repository.Get("owner-1", "Health"));
            Assert.IsNull(repository.Get("owner-1", "Attack"));
            Assert.AreEqual(other, repository.Get("owner-2", "Health"));
        }
    }
}
