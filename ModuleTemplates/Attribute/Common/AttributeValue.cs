using System;
using Sirenix.OdinInspector;

namespace Sumorin.AttributeSystem
{
	/// <summary>
	/// 屬性初始值，供其他 Domain 設定初始屬性使用
	/// </summary>
	[Serializable]
	public struct AttributeValue
	{
		/// <summary>
		/// 目標屬性名稱
		/// </summary>
		[LabelText("屬性")]
		[Required]
		[ValueDropdown("@Sumorin.AttributeSystem.AttributeDropdownProvider.GetAttributeNames()")]
		public string AttributeName;

		/// <summary>
		/// 基礎值
		/// </summary>
		[LabelText("基礎值")]
		public int BaseValue;
	}
}
