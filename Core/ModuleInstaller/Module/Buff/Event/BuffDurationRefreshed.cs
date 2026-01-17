using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 時間刷新事件
    /// </summary>
    public class BuffDurationRefreshed : IEvent
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

        public BuffDurationRefreshed(string buffId, string ownerId, string buffName)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
        }
    }
}
