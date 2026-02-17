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
        /// 轉換為 ModifyEffectInfo
        /// </summary>
        /// <returns>修改效果資訊</returns>
        public ModifyEffectInfo ToConfig()
        {
            return new ModifyEffectInfo
            {
                AttributeName = AttributeName,
                ModifyType = ModifyType,
                Value = Value
            };
        }
    }
}
