using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性配置資產內容，同時提供 Domain 讀取的數值面
	/// </summary>
	/// <remarks>
	///     欄位標註 <see cref="FormerlySerializedAsAttribute" /> 以保留舊版公開欄位的資產內容。
	/// </remarks>
	[Serializable]
	public class AttributeConfig: IAttributeConfig
	{
		[SerializeField]
		[FormerlySerializedAs("Id")]
		[LabelText("識別碼")]
		[Required]
		private string id;

		[SerializeField]
		[FormerlySerializedAs("DisplayName")]
		[LabelText("顯示名稱")]
		private LocalizedString displayName;

		[SerializeField]
		[FormerlySerializedAs("Description")]
		[LabelText("說明")]
		private LocalizedString description;

		[SerializeField]
		[FormerlySerializedAs("Min")]
		[HorizontalGroup("Min")]
		[LabelText("最小值")]
		[HideIf("@!string.IsNullOrEmpty(relationMin)")]
		[SuffixLabel("@min.ToString(\"N0\")", Overlay = true)]
		private int min;

		[SerializeField]
		[FormerlySerializedAs("RelationMin")]
		[HorizontalGroup("Min")]
		[LabelText("指定屬性為最小值"), PropertyTooltip("此屬性的最小值會受指定屬性的當前值影響，例：最小生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributeNames(id)")]
		private string relationMin;

		[SerializeField]
		[FormerlySerializedAs("Max")]
		[HorizontalGroup("Max")]
		[LabelText("最大值")]
		[HideIf("@!string.IsNullOrEmpty(relationMax)")]
		[SuffixLabel("@max.ToString(\"N0\")", Overlay = true)]
		private int max;

		[SerializeField]
		[FormerlySerializedAs("RelationMax")]
		[HorizontalGroup("Max")]
		[LabelText("指定屬性為最大值"), PropertyTooltip("此屬性的最大值會受指定屬性的當前值影響，例：最大生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributeNames(id)")]
		private string relationMax;

		[SerializeField]
		[FormerlySerializedAs("Ratio")]
		[LabelText("相對倍率"), PropertyTooltip("用於計算屬性值時，會將屬性值除以此值")]
		[MinValue(1)]
		private int ratio = 1;

		/// <summary>
		///     屬性識別碼，同時作為配置的查找鍵
		/// </summary>
		public string Id => id;

		/// <summary>
		///     顯示名稱
		/// </summary>
		public LocalizedString DisplayName => displayName;

		/// <summary>
		///     屬性說明
		/// </summary>
		public LocalizedString Description => description;

		/// <inheritdoc />
		public int Min => min;

		/// <inheritdoc />
		public int Max => max;

		/// <inheritdoc />
		public string RelationMin => relationMin;

		/// <inheritdoc />
		public string RelationMax => relationMax;

		/// <inheritdoc />
		public int Ratio => ratio;

		/// <summary>
		///     建立屬性配置
		/// </summary>
		/// <param name="id">屬性識別碼</param>
		/// <param name="min">固定下限</param>
		/// <param name="max">固定上限</param>
		/// <param name="relationMin">下限關聯屬性，空字串表示使用固定下限</param>
		/// <param name="relationMax">上限關聯屬性，空字串表示使用固定上限</param>
		/// <param name="ratio">顯示轉換倍率</param>
		public AttributeConfig(string id, int min, int max, string relationMin = "", string relationMax = "", int ratio = 1)
		{
			this.id = id;
			this.min = min;
			this.max = max;
			this.relationMin = relationMin;
			this.relationMax = relationMax;
			this.ratio = ratio;
		}
	}
}
