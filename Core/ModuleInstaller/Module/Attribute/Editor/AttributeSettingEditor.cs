using Rino.GameFramework.GameManagerBase;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Rino.GameFramework.AttributeSystem
{
	/// <summary>
	/// 屬性設定編輯器
	/// </summary>
	public class AttributeSettingEditor : GameEditorMenuBase
	{
		public override string TabName => "屬性設定";

		[Required("尚未建立屬性配置")]
		[ShowInInspector]
		[InlineEditor(InlineEditorObjectFieldModes.Hidden)]
		[HideLabel]
		private AttributeSettingData data;

		protected override void OnInitialize()
		{
			data = GetOrCreateConfig();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = SetTree();
			tree.AddSelfMenu(this, "屬性設定");
			return tree;
		}

		private AttributeSettingData GetOrCreateConfig()
		{
			var configData = RinoEditorUtility.FindAsset<AttributeSettingData>();
			if (configData != null) return configData;

			configData = ScriptableObject.CreateInstance<AttributeSettingData>();
			RinoEditorUtility.CreateSOData(configData, "Data/Setting/AttributeSettingData");
			return configData;
		}
	}
}
