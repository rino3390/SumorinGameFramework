using Rino.GameFramework.Core.AttributeSystem.Common;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rino.GameFramework.Core.AttributeSystem.View
{
    /// <summary>
    /// 屬性配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "RinoGameFramework/Data/AttributeData")]
    public class AttributeData : ScriptableObject
    {
        [LabelText("屬性名稱")]
        public string AttributeName;

        [LabelText("啟用最小值限制")]
        public bool HasMin;

        [ShowIf(nameof(HasMin))]
        [LabelText("使用關聯屬性")]
        public bool UseRelationMin;

        [ShowIf("@" + nameof(HasMin) + " && !" + nameof(UseRelationMin))]
        [LabelText("最小值")]
        public int Min;

        [ShowIf("@" + nameof(HasMin) + " && " + nameof(UseRelationMin))]
        [LabelText("關聯屬性")]
        public string RelationMin;

        [LabelText("啟用最大值限制")]
        public bool HasMax;

        [ShowIf(nameof(HasMax))]
        [LabelText("使用關聯屬性")]
        public bool UseRelationMax;

        [ShowIf("@" + nameof(HasMax) + " && !" + nameof(UseRelationMax))]
        [LabelText("最大值")]
        public int Max;

        [ShowIf("@" + nameof(HasMax) + " && " + nameof(UseRelationMax))]
        [LabelText("關聯屬性")]
        public string RelationMax;

        [LabelText("比例轉換")]
        public int Ratio = 1;

        /// <summary>
        /// 轉換為 AttributeConfig
        /// </summary>
        /// <returns>屬性配置</returns>
        public AttributeConfig ToConfig()
        {
            return new AttributeConfig
            {
                AttributeName = AttributeName,
                Min = HasMin && !UseRelationMin ? Min : int.MinValue,
                Max = HasMax && !UseRelationMax ? Max : int.MaxValue,
                RelationMin = HasMin && UseRelationMin ? RelationMin ?? "" : "",
                RelationMax = HasMax && UseRelationMax ? RelationMax ?? "" : "",
                Ratio = Ratio
            };
        }
    }
}
