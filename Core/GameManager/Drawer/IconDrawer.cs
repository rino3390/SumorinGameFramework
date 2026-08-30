using Sumorin.GameManagerBase;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Sumorin.GameManager
{
	/// <summary>
	/// IconIncludedData 自訂繪製器
	/// </summary>
	/// <typeparam name="T">繼承自 IconIncludedData 的類型</typeparam>
	internal class IconItemDrawer<T>: OdinValueDrawer<T> where T: IconIncludedData
	{
		/// <summary>
		/// 繪製屬性
		/// </summary>
		/// <param name="label">標籤</param>
		protected override void DrawPropertyLayout(GUIContent label)
		{
			// 標了 InlineEditor 的欄位要能就地編輯內容，讓給下一個繪製器
			if(Property.GetAttribute<InlineEditorAttribute>() != null)
			{
				CallNextDrawer(label);
				return;
			}

			var rect = EditorGUILayout.GetControlRect(label != null, 45);

			if(label != null)
			{
				rect.xMin = EditorGUI.PrefixLabel(rect.AlignCenterY(15), label).xMin;
			}
			else
			{
				rect = EditorGUI.IndentedRect(rect);
			}

			IconIncludedData item = this.ValueEntry.SmartValue;
			Texture texture = null;

			if(item)
			{
				if(item.Icon)
				{
					texture = item.Icon.texture;
				}

				GUI.Label(rect.AddXMin(50).AlignMiddle(16), EditorGUI.showMixedValue ? "-" : item.AssetName);
			}

			this.ValueEntry.WeakSmartValue = SirenixEditorFields.UnityPreviewObjectField(rect.AlignLeft(45), item, texture, this.ValueEntry.BaseValueType);
		}
	}
}