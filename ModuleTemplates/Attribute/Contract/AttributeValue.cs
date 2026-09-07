using System;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性初始值，供其他 Domain 設定初始屬性使用
	/// </summary>
	[Serializable]
	public struct AttributeValue
	{
		/// <summary>
		///     目標屬性的配置 Id
		/// </summary>
		[FormerlySerializedAs("ConfigId")]
		// 不加 Required，理由同 ModifyEffectInfo.AttributeConfigId
		[LabelText("屬性")]
		[ValueDropdown("@Sumorin.Attribute.AttributeDropdownProvider.GetAttributes()")]
		public string ConfigId;

		/// <summary>
		///     基礎值
		/// </summary>
		[LabelText("基礎值")]
		public int BaseValue;
	}
}