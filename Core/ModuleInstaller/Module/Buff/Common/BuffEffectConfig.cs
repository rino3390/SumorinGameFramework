using Rino.GameFramework.AttributeSystem;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 效果配置
    /// </summary>
    public struct BuffEffectConfig
    {
        /// <summary>
        /// 目標屬性名稱
        /// </summary>
        public string AttributeName;

        /// <summary>
        /// 修改類型
        /// </summary>
        public ModifyType ModifyType;

        /// <summary>
        /// 修改數值
        /// </summary>
        public int Value;
    }
}
