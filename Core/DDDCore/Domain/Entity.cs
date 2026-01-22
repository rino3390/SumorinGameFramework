using System;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// Domain Entity 抽象基底類別，只提供 Id 屬性
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Entity 的唯一識別碼
        /// </summary>
        public string Id { get; }

        protected Entity(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (id.Length == 0)
                throw new ArgumentException("Id cannot be empty.", nameof(id));

            Id = id;
        }
    }
}
