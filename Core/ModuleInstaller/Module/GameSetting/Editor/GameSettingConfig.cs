using System;
using System.Collections;
using System.Linq;
using Rino.GameFramework.GameManagerBase;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;

namespace Rino.GameFramework.GameSetting
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
		[Required]
		public Type SettingEditorType;

		private static IEnumerable GetSettingEditorTypes()
		{
			return RinoEditorUtility.GetDerivedClasses<GameEditorMenuBase>()
				.Where(type => type != typeof(GameSettingEditorMenu))
				.Select(type =>
				{
					var instance = Activator.CreateInstance(type) as GameEditorMenuBase;
					return new ValueDropdownItem(instance?.TabName ?? type.Name, type);
				})
				.ToList();
		}
	}
}