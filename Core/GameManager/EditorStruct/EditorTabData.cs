using Sumorin.GameManagerBase;
using Sirenix.OdinInspector;
using System;
using System.Collections;

namespace Sumorin.GameManager
{
	/// <summary>
	/// Editor 頁籤資料
	/// </summary>
	[HideReferenceObjectPicker]
	public class EditorTabData
	{
		/// <summary>
		/// 頁籤圖示
		/// </summary>
		[FoldoutGroup("標籤設定", true)]
		public SdfIconType TabIcon;

		/// <summary>
		/// 對應的 Editor 視窗類型
		/// </summary>
		[FoldoutGroup("Editor 設定", true)]
		[ValueDropdown("GetWindowTypeList")]
		[LabelText("繪製視窗")]
		[Required("必須指定要繪製的編輯器視窗")]
		public Type CorrespondingWindowType;

		/// <summary>
		/// 是否繪製圖示
		/// </summary>
		[FoldoutGroup("Editor 設定")]
		[LabelText("左列繪製 Icon")]
		public bool HasIcon;

		/// <summary>
		/// 圖示大小
		/// </summary>
		[FoldoutGroup("Editor 設定")]
		[ShowIf("HasIcon"), LabelText("Icon 大小")]
		public float IconSize = 28;

		private static IEnumerable GetWindowTypeList()
		{
			return EditorMenuTypeProvider.GetMenuTypes();
		}
	}
}