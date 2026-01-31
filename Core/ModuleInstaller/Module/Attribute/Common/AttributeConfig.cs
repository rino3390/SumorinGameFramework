using Sirenix.OdinInspector;

namespace Rino.GameFramework.AttributeSystem
{
	/// <summary>
	/// 屬性配置，從 ScriptableObject 轉換而來
	/// </summary>
	[InlineProperty]
	public struct AttributeConfig
	{
		/// <summary>
		/// 屬性名稱
		/// </summary>
		[HorizontalGroup("Name")]
		[LabelText("屬性名稱")]
		public string AttributeName;

		/// <summary>
		/// 最小值
		/// </summary>
		[HorizontalGroup("Min")]
		[LabelText("最小值")]
		[HideIf("@!string.IsNullOrEmpty(RelationMin)")]
		[SuffixLabel("@Min.ToString(\"N0\")", Overlay = true)]
		public int Min;

		/// <summary>
		/// 下限受哪個屬性影響（空字串 = 使用固定 Min）
		/// </summary>
		[HorizontalGroup("Min")]
		[LabelText("指定屬性為最小值"), PropertyTooltip("此屬性的最小值會受指定屬性的當前值影響，例：最小生命")]
		[ValueDropdown("@AttributeDropdownProvider.GetAttributeNames(AttributeName)")]
		public string RelationMin;

		/// <summary>
		/// 最大值
		/// </summary>
		[HorizontalGroup("Max")]
		[LabelText("最大值")]
		[SuffixLabel("@Max.ToString(\"N0\")", Overlay = true)]
		[HideIf("@!string.IsNullOrEmpty(RelationMax)")]
		public int Max;

		/// <summary>
		/// 上限受哪個屬性影響（空字串 = 使用固定 Max）
		/// </summary>
		[HorizontalGroup("Max")]
		[LabelText("指定屬性為最大值"), PropertyTooltip("此屬性的最大值會受指定屬性的當前值影響，例：最大生命")]
		[ValueDropdown("@AttributeDropdownProvider.GetAttributeNames(AttributeName)")]
		public string RelationMax;

		/// <summary>
		/// 外部取值時除以此值（用於顯示/計算轉換）
		/// </summary>
		[HorizontalGroup("Name")]
		[LabelText("相對倍率"), PropertyTooltip("用於計算屬性值時，會將屬性值除以此值")]
		[MinValue(1)]
		public int Ratio;
	}
}