using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 施加事件
    /// </summary>
    public class BuffApplied : IEvent
    {
        /// <summary>
        /// Buff 識別碼
        /// </summary>
        public string BuffId { get; }

        /// <summary>
        /// 擁有者識別碼
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Buff 名稱
        /// </summary>
        public string BuffName { get; }

        /// <summary>
        /// 來源識別碼（施加者）
        /// </summary>
        public string SourceId { get; }

        public BuffApplied(string buffId, string ownerId, string buffName, string sourceId)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
            SourceId = sourceId;
        }
    }
}
