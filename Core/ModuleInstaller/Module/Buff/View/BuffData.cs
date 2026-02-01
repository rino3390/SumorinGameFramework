using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.GameManagerBase;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;

namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff 配置 ScriptableObject
	/// </summary>
	[DataEditorConfig("Buff 資料", "Data/Buff", "Buff")]
	public class BuffData : SODataBase
	{
		[LabelText("Buff 名稱")]
		public LocalizedString BuffName;

		[HorizontalGroup("LifetimeType")]
		[LabelText("生命週期")]
		public LifetimeType LifetimeType;

		[HorizontalGroup("LifetimeType")]
		[HideLabel]
		[HideIf("LifetimeType", LifetimeType.Permanent)]
		[SuffixLabel("@LifetimeType == Rino.GameFramework.BuffSystem.LifetimeType.TimeBased ? \"秒\" : \"回合\"", Overlay = true)]
		public float Lifetime;

		[HorizontalGroup("Stack")]
		[LabelText("重複獲得時行為")]
		public StackBehavior StackBehavior;

		[HorizontalGroup("Stack")]
		[ShowIf("StackBehavior", StackBehavior.IncreaseStack)]
		[MinValue(1)]
		[LabelText("疊層上限")]
		public int MaxStack = 1;

		[HorizontalGroup("MutualExclusion")]
		[LabelText("互斥群組"), Tooltip("同群組內的Buff會互斥（同時存在會替換掉優先級低的Buff），空字串表示無互斥群組")]
		public string MutualExclusionGroup;

		[HorizontalGroup("MutualExclusion")]
		[LabelText("優先級")]
		[ShowIf("@!string.IsNullOrEmpty(MutualExclusionGroup)")]
		public int Priority;

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
				BuffName = BuffName.GetLocalizedString(),
				LifetimeType = LifetimeType,
				Lifetime = Lifetime,
				StackBehavior = StackBehavior,
				MaxStack = StackBehavior == StackBehavior.IncreaseStack ? MaxStack : -1,
				MutualExclusionGroup = MutualExclusionGroup,
				Priority = Priority,
				Effects = Effects?.Select(e => e.ToConfig()).ToList() ?? new List<BuffEffectConfig>()
			};
		}

#if UNITY_EDITOR
		/// <summary>
		/// 驗證資料是否合法
		/// </summary>
		/// <returns>資料是否合法</returns>
		public override bool IsDataLegal()
		{
			return base.IsDataLegal() && !BuffName.IsEmpty;
		}
#endif
	}
}
