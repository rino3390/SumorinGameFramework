using System.Linq;
using NUnit.Framework;

namespace Rino.GameFramework.DDDCore.Tests
{
    [TestFixture]
    public class RepositoryTests
    {
        private Repository<TestEntity> repository;

        [SetUp]
        public void SetUp()
        {
            repository = new Repository<TestEntity>();
        }

        #region Find Tests

        [Test]
        public void Find_WithMatchingPredicate_ReturnsFirstMatch()
        {
            var entity1 = new TestEntity("1", "Alice");
            var entity2 = new TestEntity("2", "Bob");
            repository.Save(entity1);
            repository.Save(entity2);

            var result = repository.Find(e => e.Name == "Alice");

            Assert.AreEqual(entity1, result);
        }

        [Test]
        public void Find_WithNoMatch_ReturnsNull()
        {
            var entity = new TestEntity("1", "Alice");
            repository.Save(entity);

            var result = repository.Find(e => e.Name == "NonExistent");

            Assert.IsNull(result);
        }

        [Test]
        public void Find_WithNullPredicate_ReturnsNull()
        {
            var entity = new TestEntity("1", "Alice");
            repository.Save(entity);

            var result = repository.Find(null);

            Assert.IsNull(result);
        }

        #endregion

        #region FindAll Tests

        [Test]
        public void FindAll_WithMatchingPredicate_ReturnsAllMatches()
        {
            var entity1 = new TestEntity("1", "Alice");
            var entity2 = new TestEntity("2", "Alice");
            var entity3 = new TestEntity("3", "Bob");
            repository.Save(entity1);
            repository.Save(entity2);
            repository.Save(entity3);

            var result = repository.FindAll(e => e.Name == "Alice").ToList();

            Assert.AreEqual(2, result.Count);
            Assert.Contains(entity1, result);
            Assert.Contains(entity2, result);
        }

        [Test]
        public void FindAll_WithNoMatch_ReturnsEmpty()
        {
            var entity = new TestEntity("1", "Alice");
            repository.Save(entity);

            var result = repository.FindAll(e => e.Name == "NonExistent").ToList();

            Assert.IsEmpty(result);
        }

        [Test]
        public void FindAll_WithNullPredicate_ReturnsEmpty()
        {
            var entity = new TestEntity("1", "Alice");
            repository.Save(entity);

            var result = repository.FindAll(null).ToList();

            Assert.IsEmpty(result);
        }

        [Test]
        public void FindAll_WithEmptyRepository_ReturnsEmpty()
        {
            var result = repository.FindAll(e => e.Name == "Alice").ToList();

            Assert.IsEmpty(result);
        }

        #endregion

        private class TestEntity : Entity
        {
            public string Name { get; }

            public TestEntity(string id, string name) : base(id)
            {
                Name = name;
            }
        }
    }
}
