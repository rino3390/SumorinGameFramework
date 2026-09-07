using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sumorin.GameManagerBase;
using UnityEngine.Localization;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性配置資產，同時提供 Domain 讀取的數值面
	/// </summary>
	/// <remarks>
	///     Id、檔案名稱、顯示名稱與圖示由 <see cref="IconIncludedData" /> 提供。
	///     <see cref="SODataBase.Id" /> 同時是 <c>ConfigManager</c> 的查找鍵。
	/// </remarks>
	[DataEditorConfig("屬性資料", "Data/Attribute", "屬性")]
	public class AttributeData: IconIncludedData, IAttributeConfig
	{
		/// <summary>
		///     屬性說明
		/// </summary>
		[OdinSerialize]
		[LabelText("說明")]
		public LocalizedString Description { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[LabelText("種類"), PropertyTooltip("數值型：基礎值是底數，效果掛修改器，移除即還原。資源型：基礎值是當前量，用增減命令變動，不接受修改器")]
		public AttributeKind Kind { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("Min")]
		[LabelText("最小值")]
		[HideIf("@!string.IsNullOrEmpty(RelationMin)")]
		[SuffixLabel("@Min.ToString(\"N0\")", Overlay = true)]
		public int Min { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("Min")]
		[LabelText("指定屬性為最小值"), PropertyTooltip("此屬性的最小值會受指定屬性的當前值影響，例：最小生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes(Id)")]
		// 下拉的「不指定」對應空字串，Odin 序列化的字串預設是 null，不給初始值會對不上任何選項
		public string RelationMin { get; private set; } = "";

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("Max")]
		[LabelText("最大值")]
		[HideIf("@!string.IsNullOrEmpty(RelationMax)")]
		[SuffixLabel("@Max.ToString(\"N0\")", Overlay = true)]
		public int Max { get; private set; }

		/// <inheritdoc />
		[OdinSerialize]
		[HorizontalGroup("Max")]
		[LabelText("指定屬性為最大值"), PropertyTooltip("此屬性的最大值會受指定屬性的當前值影響，例：最大生命")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes(Id)")]
		public string RelationMax { get; private set; } = "";

		/// <inheritdoc />
		[OdinSerialize]
		[LabelText("相對倍率"), PropertyTooltip("用於計算屬性值時，會將屬性值除以此值")]
		[MinValue(1)]
		public int Ratio { get; private set; } = 1;
	}
}