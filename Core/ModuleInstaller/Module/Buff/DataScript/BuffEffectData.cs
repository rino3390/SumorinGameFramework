using System;
using Sirenix.OdinInspector;
using Sumorin.GameFramework.AttributeSystem;

namespace Sumorin.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 效果配置
    /// </summary>
    [Serializable]
    public class BuffEffectData
    {
        [LabelText("目標屬性")]
		[Required]
        [ValueDropdown("@Sumorin.GameFramework.AttributeSystem.AttributeDropdownProvider.GetAttributeNames()")]
        public string AttributeName;

        [LabelText("修改類型")]
        public ModifyType ModifyType;

        [LabelText("數值")]
		[Required]
        public int Value;

        /// <summary>
        /// 轉換為 BuffEffectConfig
        /// </summary>
        /// <returns>Buff 效果配置</returns>
        public BuffEffectConfig ToConfig()
        {
            return new BuffEffectConfig
            {
                AttributeName = AttributeName,
                ModifyType = ModifyType,
                Value = Value
            };
        }
    }
}
