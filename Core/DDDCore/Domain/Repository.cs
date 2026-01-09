using System;
using System.Collections.Generic;
using System.Linq;

namespace Rino.GameFramework.Core.DDDCore.Domain
{
	/// <summary>
	///     Repository 實作，提供 Entity 的記憶體儲存
	/// </summary>
	/// <typeparam name="TEntity">Entity 類型</typeparam>
	public class Repository<TEntity>: IRepository<TEntity> where TEntity: Entity
	{
		protected readonly Dictionary<string, TEntity> entities = new();

		/// <inheritdoc />
		public int Count => entities.Count;

		/// <inheritdoc />
		public IEnumerable<string> Keys => entities.Keys;

		/// <inheritdoc />
		public IEnumerable<TEntity> Values => entities.Values;

		/// <inheritdoc />
		public TEntity this[string id]
		{
			get
			{
				TryGet(id, out var entity);
				return entity;
			}
		}

		/// <inheritdoc />
		public void Save(TEntity entity)
		{
			if(entity == null) return;

			entities[entity.Id] = entity;
		}

		/// <inheritdoc />
		public void DeleteAll()
		{
			entities.Clear();
		}

		/// <inheritdoc />
		public void DeleteById(string id)
		{
			entities.Remove(id);
		}

		/// <inheritdoc />
		public bool TryGet(string id, out TEntity value)
		{
			return entities.TryGetValue(id, out value);
		}

		/// <inheritdoc />
		public TEntity Find(Func<TEntity, bool> predicate)
		{
			return predicate == null ? null : entities.Values.FirstOrDefault(predicate);
		}
	}
}