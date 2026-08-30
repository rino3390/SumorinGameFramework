using System;
using System.Collections;
using Sumorin.GameManagerBase;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace Sumorin.GameSetting
{
	/// <summary>
	/// 遊戲設定配置，管理要顯示的 Setting 項目
	/// </summary>
	public class GameSettingConfig: SerializedScriptableObject
	{
		[OdinSerialize]
		[ListDrawerSettings(CustomAddFunction = nameof(CreateNewTab), DraggableItems = true)]
		[LabelText("Setting 列表")]
		public List<SettingItemData> Settings = new();

		private SettingItemData CreateNewTab()
		{
			return new SettingItemData();
		}
	}

	/// <summary>
	/// Setting 項目資料
	/// </summary>
	[HideReferenceObjectPicker]
	public class SettingItemData
	{
		public SdfIconType Icon = SdfIconType.GearFill;

		[LabelText("繪製視窗")]
		[ValueDropdown("GetSettingEditorTypes")]
		[Required("必須指定要繪製的編輯器視窗")]
		public Type SettingEditorType;

		private static IEnumerable GetSettingEditorTypes()
		{
			return EditorMenuTypeProvider.GetSettingMenuTypes(typeof(GameSettingEditorMenu));
		}
	}
}