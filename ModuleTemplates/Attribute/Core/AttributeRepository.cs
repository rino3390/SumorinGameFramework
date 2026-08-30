using System.Collections.Generic;
using System.Linq;
using Sumorin.DDDCore;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Repository 實作
	/// </summary>
	public class AttributeRepository: Repository<Attribute>, IAttributeRepository
	{
	#region IAttributeRepository Members
		/// <inheritdoc />
		public Attribute Get(string ownerId, string configId)
		{
			return Find(attr => attr.OwnerId == ownerId && attr.ConfigId == configId);
		}

		/// <inheritdoc />
		public List<Attribute> GetByOwnerId(string ownerId)
		{
			return FindAll(attr => attr.OwnerId == ownerId).ToList();
		}

		/// <inheritdoc />
		public void DeleteByOwnerId(string ownerId)
		{
			foreach(var attribute in GetByOwnerId(ownerId))
			{
				DeleteById(attribute.Id);
			}
		}
	#endregion
	}
}