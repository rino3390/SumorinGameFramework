namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 堆疊變化資訊，用於 Model Observable 通知 Controller 狀態變化
    /// </summary>
    public struct BuffStackChangedInfo
    {
        /// <summary>
        /// Buff 識別碼
        /// </summary>
        public string BuffId;

        /// <summary>
        /// 擁有者識別碼
        /// </summary>
        public string OwnerId;

        /// <summary>
        /// Buff 名稱
        /// </summary>
        public string BuffName;

        /// <summary>
        /// 變化前堆疊數
        /// </summary>
        public int OldStack;

        /// <summary>
        /// 變化後堆疊數
        /// </summary>
        public int NewStack;

        public BuffStackChangedInfo(string buffId, string ownerId, string buffName, int oldStack, int newStack)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
            OldStack = oldStack;
            NewStack = newStack;
        }
    }
}
