using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Repository 實作
	/// </summary>
	public class BuffRepository: Repository<Buff>, IBuffRepository
	{
	#region IBuffRepository Members
		/// <inheritdoc />
		public Buff Get(string buffId)
		{
			TryGet(buffId, out var buff);
			return buff;
		}

		/// <inheritdoc />
		public IEnumerable<Buff> GetByOwner(string ownerId) => FindAll(buff => buff.OwnerId == ownerId);
	#endregion
	}
}