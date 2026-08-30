using Sirenix.OdinInspector;
using Sumorin.GameManagerBase;
using UnityEngine;
using UnityEngine.Localization;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性配置資產，同時提供 Domain 讀取的數值面
	/// </summary>
	/// <remarks>
	///     識別碼、檔案名稱、顯示名稱與圖示由 <see cref="IconIncludedData" /> 提供。
	///     <see cref="SODataBase.Id" /> 同時是 <c>ConfigManager</c> 的查找鍵。
	/// </remarks>
	[DataEditorConfig("屬性資料", "Data/Attribute", "屬性")]
	public class AttributeData: IconIncludedData, IAttributeConfig
	{
		[SerializeField]
		[LabelText("說明")]
		private LocalizedString description;

		[SerializeField]
		[HorizontalGroup("Min")]
		[LabelText("最小值")]
		[HideIf("@!string.IsNullOrEmpty(relationMin)")]
		[SuffixLabel("@min.ToString(\"N0\")", Overlay = true)]
		private int min;

		[SerializeField]
		[HorizontalGroup("Min")]
		[LabelText("指定屬性為最小值"), PropertyTooltip("此屬性的最小值會受指定屬性的當前值影響，例：最小生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes(Id)")]
		private string relationMin;

		[SerializeField]
		[HorizontalGroup("Max")]
		[LabelText("最大值")]
		[HideIf("@!string.IsNullOrEmpty(relationMax)")]
		[SuffixLabel("@max.ToString(\"N0\")", Overlay = true)]
		private int max;

		[SerializeField]
		[HorizontalGroup("Max")]
		[LabelText("指定屬性為最大值"), PropertyTooltip("此屬性的最大值會受指定屬性的當前值影響，例：最大生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes(Id)")]
		private string relationMax;

		[SerializeField]
		[LabelText("相對倍率"), PropertyTooltip("用於計算屬性值時，會將屬性值除以此值")]
		[MinValue(1)]
		private int ratio = 1;

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
	}
}