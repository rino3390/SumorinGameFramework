using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sumorin.Attribute;
using Sumorin.GameManagerBase;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 配置資產，同時提供 Domain 讀取的數值面
	/// </summary>
	/// <remarks>
	///     欄位標註 <see cref="FormerlySerializedAsAttribute" /> 以保留舊版公開欄位的資產內容。
	/// </remarks>
	[DataEditorConfig("Buff 資料", "Data/Buff", "Buff")]
	public class BuffData: IconIncludedData, IBuffConfig
	{
		[SerializeField]
		[FormerlySerializedAs("LifetimeType")]
		[HorizontalGroup("LifetimeType")]
		[LabelText("生命週期")]
		private LifetimeType lifetimeType;

		[SerializeField]
		[FormerlySerializedAs("Lifetime")]
		[HorizontalGroup("LifetimeType")]
		[HideLabel]
		[HideIf("lifetimeType", LifetimeType.Permanent)]
		[SuffixLabel("@lifetimeType == Sumorin.Buff.LifetimeType.TimeBased ? \"秒\" : \"回合\"", Overlay = true)]
		private float lifetime;

		[SerializeField]
		[FormerlySerializedAs("StackBehavior")]
		[HorizontalGroup("Stack")]
		[LabelText("重複獲得時行為")]
		private StackBehavior stackBehavior;

		[SerializeField]
		[FormerlySerializedAs("MaxStack")]
		[HorizontalGroup("Stack")]
		[ShowIf("stackBehavior", StackBehavior.IncreaseStack)]
		[MinValue(1)]
		[LabelText("疊層上限")]
		private int maxStack = 1;

		[SerializeField]
		[FormerlySerializedAs("MutualExclusionGroup")]
		[HorizontalGroup("MutualExclusion")]
		[LabelText("互斥群組"), Tooltip("同群組內的 Buff 會互斥（同時存在會替換掉優先級低的 Buff），空字串表示無互斥群組")]
		private string mutualExclusionGroup;

		[SerializeField]
		[FormerlySerializedAs("Priority")]
		[HorizontalGroup("MutualExclusion")]
		[LabelText("優先級")]
		[ShowIf("@!string.IsNullOrEmpty(mutualExclusionGroup)")]
		private int priority;

		[SerializeField]
		[LabelText("時效到期時移除全部層數"), Tooltip("關閉時，時效到期只移除一層並重置時效")]
		private bool removeAllOnExpire;

		[SerializeField]
		[FormerlySerializedAs("Effects")]
		[LabelText("效果")]
		private List<ModifyEffectInfo> effects = new();

		[SerializeField]
		[LabelText("標籤"), Tooltip("供依標籤批次移除使用，如 Debuff、DoT、Movement")]
		private List<string> tags = new();

		/// <inheritdoc />
		public LifetimeType LifetimeType => lifetimeType;

		/// <inheritdoc />
		public float Lifetime => lifetime;

		/// <inheritdoc />
		public StackBehavior StackBehavior => stackBehavior;

		/// <inheritdoc />
		public int MaxStack => stackBehavior == StackBehavior.IncreaseStack ? maxStack : -1;

		/// <inheritdoc />
		public string MutualExclusionGroup => mutualExclusionGroup;

		/// <inheritdoc />
		public int Priority => priority;

		/// <inheritdoc />
		public IReadOnlyList<ModifyEffectInfo> Effects => effects;

		/// <inheritdoc />
		public IReadOnlyList<string> Tags => tags;

		/// <inheritdoc />
		public bool RemoveAllOnExpire => removeAllOnExpire;
	}
}