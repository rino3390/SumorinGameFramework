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

        #region Save Tests

        [Test]
        public void Save_WithValidBuff_StoresInRepository()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);

            repository.Save(buff);

            Assert.AreEqual(buff, repository.Get("buff-1"));
        }

        [Test]
        public void Save_WithSameId_OverwritesExisting()
        {
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            var buff2 = new Buff("buff-1", "Burn", "owner-2", "source-2", -1, LifetimeType.Permanent, 0f);

            repository.Save(buff1);
            repository.Save(buff2);

            Assert.AreEqual("Burn", repository.Get("buff-1").BuffName);
        }

        #endregion

        #region Get Tests

        [Test]
        public void Get_WithExistingId_ReturnsBuff()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            repository.Save(buff);

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
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            var buff2 = new Buff("buff-2", "Burn", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            var buff3 = new Buff("buff-3", "Freeze", "owner-2", "source-1", -1, LifetimeType.Permanent, 0f);
            repository.Save(buff1);
            repository.Save(buff2);
            repository.Save(buff3);

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

        #region Values Tests

        [Test]
        public void Values_WithMultipleBuffs_ReturnsAllBuffs()
        {
            var buff1 = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            var buff2 = new Buff("buff-2", "Burn", "owner-2", "source-1", -1, LifetimeType.Permanent, 0f);
            repository.Save(buff1);
            repository.Save(buff2);

            var result = repository.Values.ToList();

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void Values_WithEmptyRepository_ReturnsEmptyList()
        {
            var result = repository.Values.ToList();

            Assert.IsEmpty(result);
        }

        #endregion

        #region DeleteById Tests

        [Test]
        public void DeleteById_WithExistingId_RemovesBuff()
        {
            var buff = new Buff("buff-1", "Poison", "owner-1", "source-1", -1, LifetimeType.Permanent, 0f);
            repository.Save(buff);

            repository.DeleteById("buff-1");

            Assert.IsNull(repository.Get("buff-1"));
        }

        [Test]
        public void DeleteById_WithNonExistingId_DoesNothing()
        {
            repository.DeleteById("non-existing");

            Assert.IsEmpty(repository.Values);
        }

        #endregion
    }
}
