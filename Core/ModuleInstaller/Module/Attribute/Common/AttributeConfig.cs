namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 屬性配置，從 ScriptableObject 轉換而來
    /// </summary>
    public struct AttributeConfig
    {
        /// <summary>
        /// 屬性名稱
        /// </summary>
        public string AttributeName;

        /// <summary>
        /// 最小值
        /// </summary>
        public int Min;

        /// <summary>
        /// 最大值
        /// </summary>
        public int Max;

        /// <summary>
        /// 上限受哪個屬性影響（空字串 = 使用固定 Max）
        /// </summary>
        public string RelationMax;

        /// <summary>
        /// 下限受哪個屬性影響（空字串 = 使用固定 Min）
        /// </summary>
        public string RelationMin;

        /// <summary>
        /// 外部取值時除以此值（用於顯示/計算轉換）
        /// </summary>
        public int Ratio;
    }
}
