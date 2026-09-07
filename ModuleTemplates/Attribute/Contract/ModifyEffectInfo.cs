using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性修改效果資訊，描述如何修改一個屬性
	/// </summary>
	[Serializable]
	public struct ModifyEffectInfo
	{
		/// <summary>
		///     目標屬性的配置 Id
		/// </summary>
		[FormerlySerializedAs("ConfigId")]
		// 不加 Required。值取自屬性資產的 Id，屬性沒填 Id 時這裡自然是空的
		// 那時 DataScriptIdRule 已經在報同一件事並提供修復，這裡再報一次只是重複
		[LabelText("目標屬性")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes()")]
		public string AttributeConfigId;

		/// <summary>
		///     修改類型
		/// </summary>
		[LabelText("修改類型")]
		public ModifyType ModifyType;

		/// <summary>
		///     修改數值
		/// </summary>
		[LabelText("數值")]
		[Required("修改數值不得為 0")]
		public int Value;
	}
}