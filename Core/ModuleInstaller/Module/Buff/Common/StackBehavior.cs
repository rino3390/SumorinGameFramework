namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 堆疊行為
    /// </summary>
    public enum StackBehavior
    {
        /// <summary>
        /// 獨立存在，各自計時
        /// </summary>
        Independent,

        /// <summary>
        /// 刷新時間，層數不變
        /// </summary>
        RefreshDuration,

        /// <summary>
        /// 增加層數，時間刷新
        /// </summary>
        IncreaseStack,

        /// <summary>
        /// 覆蓋舊的
        /// </summary>
        Replace
    }
}
