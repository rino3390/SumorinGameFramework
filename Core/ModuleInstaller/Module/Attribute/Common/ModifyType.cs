namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 修改器類型，定義數值的計算方式
    /// </summary>
    public enum ModifyType
    {
        /// <summary>
        /// 固定值加減（當前值 + 調整值）
        /// </summary>
        Flat,

        /// <summary>
        /// 百分比加減（當前值 + 當前值 * 調整值%）
        /// </summary>
        Percent,

        /// <summary>
        /// 倍率相乘（當前值 * 調整值）
        /// </summary>
        Multiple
    }
}
