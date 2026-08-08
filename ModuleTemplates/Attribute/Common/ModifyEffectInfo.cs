using System;
using Sirenix.OdinInspector;

namespace Sumorin.AttributeSystem
{
	/// <summary>
	/// 屬性修改效果資訊，描述如何修改一個屬性
	/// </summary>
	[Serializable]
	public struct ModifyEffectInfo
	{
		/// <summary>
		/// 目標屬性名稱
		/// </summary>
		[LabelText("目標屬性")]
		[Required]
		[ValueDropdown("@Sumorin.AttributeSystem.AttributeDropdownProvider.GetAttributeNames()")]
		public string AttributeName;

		/// <summary>
		/// 修改類型
		/// </summary>
		[LabelText("修改類型")]
		public ModifyType ModifyType;

		/// <summary>
		/// 修改數值
		/// </summary>
		[LabelText("數值")]
		[Required]
		public int Value;
	}
}
