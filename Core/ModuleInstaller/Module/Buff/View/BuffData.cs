using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 配置 ScriptableObject
    /// </summary>
    [CreateAssetMenu(menuName = "RinoGameFramework/Data/BuffData")]
    public class BuffData : ScriptableObject
    {
        [BoxGroup("基本資訊")]
        [LabelText("Buff 名稱")]
        public string BuffName;

        [BoxGroup("持續時間")]
        [LabelText("即時制秒數")]
        [Tooltip("0 或負數 = 永久")]
        public float Duration;

        [BoxGroup("持續時間")]
        [LabelText("回合數")]
        [Tooltip("0 或負數 = 永久")]
        public int Turns;

        [BoxGroup("堆疊設定")]
        [LabelText("堆疊行為")]
        public StackBehavior StackBehavior;

        [BoxGroup("堆疊設定")]
        [LabelText("最大堆疊數")]
        [Tooltip("0 = 無上限")]
        public int MaxStack;

        [BoxGroup("互斥設定")]
        [LabelText("互斥群組")]
        public string MutualExclusionGroup;

        [BoxGroup("互斥設定")]
        [LabelText("優先級")]
        public int Priority;

        [BoxGroup("效果列表")]
        [LabelText("效果")]
        public List<BuffEffectData> Effects;

        /// <summary>
        /// 轉換為 BuffConfig
        /// </summary>
        /// <returns>Buff 配置</returns>
        public BuffConfig ToConfig()
        {
            return new BuffConfig
            {
                BuffName = BuffName,
                Duration = Duration > 0 ? Duration : null,
                Turns = Turns > 0 ? Turns : null,
                StackBehavior = StackBehavior,
                MaxStack = MaxStack > 0 ? MaxStack : null,
                MutualExclusionGroup = MutualExclusionGroup,
                Priority = Priority,
                Effects = Effects?.Select(e => e.ToConfig()).ToList() ?? new List<BuffEffectConfig>()
            };
        }
    }
}
