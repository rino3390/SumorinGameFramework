using System.Collections.Generic;
using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 移除事件
    /// </summary>
    public class BuffRemoved : IEvent
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
        /// Modifier 記錄列表
        /// </summary>
        public List<ModifierRecord> ModifierRecords { get; }

        /// <summary>
        /// 移除原因
        /// </summary>
        public string Reason { get; }

        public BuffRemoved(string buffId, string ownerId, string buffName, List<ModifierRecord> modifierRecords, string reason)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
            ModifierRecords = modifierRecords;
            Reason = reason;
        }
    }
}
