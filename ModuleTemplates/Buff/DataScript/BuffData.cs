using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
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
		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("LifetimeType")]
		[LabelText("生命週期")]
		public LifetimeType LifetimeType { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("LifetimeType")]
		[HideLabel]
		[HideIf("LifetimeType", LifetimeType.Permanent)]
		[SuffixLabel("$LifetimeSuffix", Overlay = true)]
		public float Lifetime { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("Stack")]
		[LabelText("重複獲得時行為")]
		public StackBehavior StackBehavior { get; private set; }

		// MaxStack 是推導值、Effects 與 Tags 對外只給唯讀檢視，無法直接序列化介面屬性，留欄位轉發。
		// Odin 預設把欄位排在屬性前面，這三個欄位標順序才不會跑到最上面
		[SerializeField]
		[FormerlySerializedAs("MaxStack")]
		[HorizontalGroup("Stack")]
		[ShowIf("StackBehavior", StackBehavior.IncreaseStack)]
		[MinValue(1)]
		[LabelText("疊層上限")]
		[PropertyOrder(1)]
		private int maxStack = 1;

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("MutualExclusion")]
		[LabelText("互斥群組"), PropertyTooltip("同群組內的 Buff 會互斥（同時存在會替換掉優先級低的 Buff），空字串表示無互斥群組")]
		public string MutualExclusionGroup { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("MutualExclusion")]
		[LabelText("優先級")]
		[ShowIf("@!string.IsNullOrEmpty(MutualExclusionGroup)")]
		public int Priority { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[LabelText("時效到期時移除全部層數"), PropertyTooltip("關閉時，時效到期只移除一層並重置時效")]
		public bool RemoveAllOnExpire { get; private set; }

		[SerializeField]
		[FormerlySerializedAs("Effects")]
		[LabelText("效果")]
		[PropertyOrder(1)]
		private List<ModifyEffectInfo> effects = new();

		[SerializeField]
		[LabelText("標籤"), Tooltip("供依標籤批次移除使用，如 Debuff、DoT、Movement")]
		[PropertyOrder(1)]
		private List<string> tags = new();

		/// <inheritdoc />
		public int MaxStack => StackBehavior == StackBehavior.IncreaseStack ? maxStack : -1;

		/// <inheritdoc />
		public IReadOnlyList<ModifyEffectInfo> Effects => effects;

		/// <inheritdoc />
		public IReadOnlyList<string> Tags => tags;

		// 成員與型別同名為 LifetimeType，Odin 字串運算式可能解析錯邊，後綴改由屬性提供
		private string LifetimeSuffix => LifetimeType == LifetimeType.TimeBased ? "秒" : "回合";
	}
}