using Sirenix.OdinInspector;

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
		[LabelText("獨立為新的Buff")]
        Independent,

        /// <summary>
        /// 刷新時間，層數不變
        /// </summary>
        [LabelText("刷新舊有Buff時間")]
        RefreshDuration,

        /// <summary>
        /// 增加層數，時間刷新
        /// </summary>
        [LabelText("增加疊層")]
        IncreaseStack,

        /// <summary>
        /// 覆蓋舊的
        /// </summary>
        [LabelText("覆蓋舊有Buff")]
        Replace
    }
}
