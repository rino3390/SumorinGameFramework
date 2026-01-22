namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 屬性變化資訊，用於協調型 Controller 訂閱
    /// </summary>
    public struct AttributeChangedInfo
    {
        /// <summary>
        /// 擁有者識別碼
        /// </summary>
        public string OwnerId;

        /// <summary>
        /// 屬性名稱
        /// </summary>
        public string AttributeName;

        /// <summary>
        /// 變化前的數值
        /// </summary>
        public int OldValue;

        /// <summary>
        /// 變化後的數值
        /// </summary>
        public int NewValue;

        /// <summary>
        /// 當前最小值
        /// </summary>
        public int MinValue;

        /// <summary>
        /// 當前最大值
        /// </summary>
        public int MaxValue;
    }
}
