using Sumorin.GameFramework.SumorinUtility;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Sumorin.GameFramework.AttributeSystem
{
	/// <summary>
	/// 屬性系統配置，包含所有屬性的定義
	/// </summary>
	public class AttributeSettingData: SerializedScriptableObject
	{
		[ListDrawerSettings(DraggableItems = true, ShowFoldout = true, ListElementLabelName = nameof(AttributeConfig.Id), CustomAddFunction = nameof(CreateDefaultAttribute))]
		[LabelText("屬性列表", Icon = SdfIconType.DropletHalf)]
		[Searchable]
		[UniqueList(nameof(AttributeConfig.Id), "識別碼重複")]
		public List<AttributeConfig> Attributes = new();

		private AttributeConfig CreateDefaultAttribute() =>
			new()
			{
				Id = $"Attribute{Attributes.Count + 1}",
				Min = 0,
				Max = 999999999,
				RelationMax = "",
				RelationMin = "",
				Ratio = 1
			};
	}
}