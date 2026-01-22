using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 屬性 Repository 實作
    /// </summary>
    public class AttributeRepository : Repository<Attribute>, IAttributeRepository
    {
        /// <inheritdoc />
        public Attribute Get(string ownerId, string attributeName)
        {
            return Find(attr => attr.OwnerId == ownerId && attr.AttributeName == attributeName);
        }

        /// <inheritdoc />
        public List<Attribute> GetByOwnerId(string ownerId)
        {
            return entities.Values
                .Where(attr => attr.OwnerId == ownerId)
                .ToList();
        }

        /// <inheritdoc />
        public void DeleteByOwnerId(string ownerId)
        {
            var toRemove = entities.Values
                .Where(attr => attr.OwnerId == ownerId)
                .Select(attr => attr.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                entities.Remove(id);
            }
        }
    }
}
