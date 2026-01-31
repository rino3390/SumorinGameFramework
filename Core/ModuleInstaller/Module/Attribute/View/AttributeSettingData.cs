using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Rino.GameFramework.AttributeSystem
{
	/// <summary>
	/// 屬性系統配置，包含所有屬性的定義
	/// </summary>
	public class AttributeSettingData: SerializedScriptableObject
	{
		[OdinSerialize]
		[ListDrawerSettings(DraggableItems = true, ShowFoldout = false, CustomAddFunction = nameof(CreateDefaultAttribute))]
		[LabelText("屬性列表", Icon = SdfIconType.DropletHalf)]
		public List<AttributeConfig> Attributes = new();

		private AttributeConfig CreateDefaultAttribute() =>
			new()
			{
				AttributeName = "",
				Min = 0,
				Max = 999999999,
				RelationMax = "",
				RelationMin = "",
				Ratio = 1
			};
	}
}