using System.Linq;
using NUnit.Framework;

namespace Rino.GameFramework.BuffSystem.Tests
{
    [TestFixture]
    public class BuffRepositoryTests
    {
        private BuffRepository repository;

        [SetUp]
        public void SetUp()
        {
            repository = new BuffRepository();
        }

        #region Add Tests

        [Test]
        public void Add_WithValidBuff_StoresInRepository()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);

            repository.Add(buff);

            Assert.AreEqual(buff, repository.Get("buff-1"));
        }

        [Test]
        public void Add_WithSameId_OverwritesExisting()
        {
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            var buff2 = new Buff("buff-1", "Burn", "owner-2", "source-2", null, null, null);

            repository.Add(buff1);
            repository.Add(buff2);

            Assert.AreEqual("Burn", repository.Get("buff-1").BuffName);
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_WithExistingId_ReturnsBuff()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            repository.Add(buff);

            var result = repository.Get("buff-1");

            Assert.AreEqual(buff, result);
        }

        [Test]
        public void Get_WithNonExistingId_ReturnsNull()
        {
            var result = repository.Get("non-existing");

            Assert.IsNull(result);
        }

        #endregion

        #region GetByOwner Tests

        [Test]
        public void GetByOwner_WithExistingOwner_ReturnsOwnerBuffs()
        {
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            var buff2 = new Buff("buff-2", "Burn", "owner-1", "source-1", null, null, null);
            var buff3 = new Buff("buff-3", "Freeze", "owner-2", "source-1", null, null, null);
            repository.Add(buff1);
            repository.Add(buff2);
            repository.Add(buff3);

            var result = repository.GetByOwner("owner-1").ToList();

            Assert.AreEqual(2, result.Count);
            Assert.Contains(buff1, result);
            Assert.Contains(buff2, result);
        }

        [Test]
        public void GetByOwner_WithNonExistingOwner_ReturnsEmptyList()
        {
            var result = repository.GetByOwner("non-existing").ToList();

            Assert.IsEmpty(result);
        }

        #endregion

        #region GetAll Tests

        [Test]
        public void GetAll_WithMultipleBuffs_ReturnsAllBuffs()
        {
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            var buff2 = new Buff("buff-2", "Burn", "owner-2", "source-1", null, null, null);
            repository.Add(buff1);
            repository.Add(buff2);

            var result = repository.GetAll().ToList();

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetAll_WithEmptyRepository_ReturnsEmptyList()
        {
            var result = repository.GetAll().ToList();

            Assert.IsEmpty(result);
        }

        #endregion

        #region Remove Tests

        [Test]
        public void Remove_WithExistingId_RemovesBuff()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", null, null, null);
            repository.Add(buff);

            repository.Remove("buff-1");

            Assert.IsNull(repository.Get("buff-1"));
        }

        [Test]
        public void Remove_WithNonExistingId_DoesNothing()
        {
            repository.Remove("non-existing");

            Assert.IsEmpty(repository.GetAll());
        }

        #endregion
    }
}
