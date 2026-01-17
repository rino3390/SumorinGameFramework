namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 資訊結構，用於 Controller Observable 通知 Presenter 更新 UI
    /// </summary>
    public struct BuffInfo
    {
        /// <summary>
        /// Buff 識別碼
        /// </summary>
        public string BuffId;

        /// <summary>
        /// Buff 名稱
        /// </summary>
        public string BuffName;

        /// <summary>
        /// 當前堆疊數
        /// </summary>
        public int StackCount;

        /// <summary>
        /// 剩餘持續時間（秒）
        /// </summary>
        public float? RemainingDuration;

        /// <summary>
        /// 剩餘回合數
        /// </summary>
        public int? RemainingTurns;

        public BuffInfo(string buffId, string buffName, int stackCount, float? remainingDuration, int? remainingTurns)
        {
            BuffId = buffId;
            BuffName = buffName;
            StackCount = stackCount;
            RemainingDuration = remainingDuration;
            RemainingTurns = remainingTurns;
        }
    }
}
