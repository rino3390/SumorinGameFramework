namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 時間刷新資訊，用於 Model Observable 通知 Controller 時間刷新
    /// </summary>
    public struct BuffDurationRefreshedInfo
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

        public BuffDurationRefreshedInfo(string buffId, string ownerId, string buffName)
        {
            BuffId = buffId;
            OwnerId = ownerId;
            BuffName = buffName;
        }
    }
}
