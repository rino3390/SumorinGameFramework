using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.BuffSystem
{
    /// <summary>
    /// Buff Repository 實作
    /// </summary>
    public class BuffRepository : Repository<Buff>, IBuffRepository
    {
        /// <inheritdoc />
        public Buff Get(string buffId)
        {
            TryGet(buffId, out var buff);
            return buff;
        }

        /// <inheritdoc />
        public IEnumerable<Buff> GetByOwner(string ownerId) => FindAll(b => b.OwnerId == ownerId);
    }
}
