using System.Collections.Generic;
using Rino.GameFramework.Core.DDDCore.Event;

namespace Rino.GameFramework.Core.DDDCore.Domain
{
    /// <summary>
    /// Domain Entity 基礎類別，提供 DomainEvent 累積機制
    /// </summary>
    public class Entity
    {
        private readonly List<IEvent> domainEvents = new();

        /// <summary>
        /// Entity 的唯一識別碼
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 累積的 Domain Events（唯讀）
        /// </summary>
        public IReadOnlyList<IEvent> DomainEvents => domainEvents;

        protected Entity(string id)
        {
            Id = id;
        }

        /// <summary>
        /// 新增 Domain Event 到累積清單
        /// </summary>
        /// <param name="evt">要新增的事件</param>
        public void AddDomainEvent(IEvent evt)
        {
            if (evt == null) return;

            domainEvents.Add(evt);
        }

        /// <summary>
        /// 清除所有累積的 Domain Events
        /// </summary>
        public void ClearDomainEvents()
        {
            domainEvents.Clear();
        }
    }
}
