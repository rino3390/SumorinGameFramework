using System.Collections.Generic;
using System.Linq;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff Repository 實作
    /// </summary>
    public class BuffRepository : IBuffRepository
    {
        private readonly Dictionary<string, Buff> buffs = new();

        /// <inheritdoc />
        public Buff Get(string buffId)
        {
            return buffs.TryGetValue(buffId, out var buff) ? buff : null;
        }

        /// <inheritdoc />
        public IEnumerable<Buff> GetByOwner(string ownerId)
        {
            return buffs.Values.Where(b => b.OwnerId == ownerId);
        }

        /// <inheritdoc />
        public IEnumerable<Buff> GetAll()
        {
            return buffs.Values;
        }

        /// <inheritdoc />
        public void Add(Buff buff)
        {
            buffs[buff.Id] = buff;
        }

        /// <inheritdoc />
        public void Remove(string buffId)
        {
            buffs.Remove(buffId);
        }
    }
}
