using System.Collections.Generic;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 配置結構
    /// </summary>
    public struct BuffConfig
    {
        /// <summary>
        /// Buff 名稱
        /// </summary>
        public string BuffName;

        /// <summary>
        /// 持續時間（秒），null 表示永久
        /// </summary>
        public float? Duration;

        /// <summary>
        /// 持續回合數，null 表示永久
        /// </summary>
        public int? Turns;

        /// <summary>
        /// 堆疊行為
        /// </summary>
        public StackBehavior StackBehavior;

        /// <summary>
        /// 最大堆疊數，null 表示無上限
        /// </summary>
        public int? MaxStack;

        /// <summary>
        /// 互斥群組名稱
        /// </summary>
        public string MutualExclusionGroup;

        /// <summary>
        /// 優先級（同互斥群組內比較）
        /// </summary>
        public int Priority;

        /// <summary>
        /// 效果列表
        /// </summary>
        public List<BuffEffectConfig> Effects;
    }
}
