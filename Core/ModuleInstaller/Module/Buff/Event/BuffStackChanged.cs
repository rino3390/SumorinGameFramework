using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 堆疊變化事件
    /// </summary>
    public class BuffStackChanged : IEvent
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
        /// 變化前堆疊數
        /// </summary>
        public int OldStack { get; }

        /// <summary>
        /// 變化後堆疊數
        /// </summary>
        public int NewStack { get; }

        public BuffStackChanged(string buffId, string ownerId, string buffName, int oldStack, int newStack)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
            OldStack = oldStack;
            NewStack = newStack;
        }
    }
}
