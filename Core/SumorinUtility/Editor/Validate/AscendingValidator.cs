#if ODIN_INSPECTOR
	using Sirenix.OdinInspector.Editor;
	using Sirenix.OdinInspector.Editor.Validation;
	using System;
	using System.Collections;

	[assembly: RegisterValidator(typeof(Sumorin.SumorinUtility.Editor.AscendingValidator))]

	namespace Sumorin.SumorinUtility.Editor
	{
		/// <summary>
		///     AscendingAttribute 的 Odin Inspector 驗證器，檢查清單是否嚴格遞增
		/// </summary>
		public class AscendingValidator: AttributeValidator<AscendingAttribute>
		{
			/// <summary>
			///     判斷是否可以驗證指定的屬性
			/// </summary>
			/// <param name="property">要驗證的屬性</param>
			/// <returns>屬性值為清單時回傳 true</returns>
			public override bool CanValidateProperty(InspectorProperty property)
			{
				return typeof(IList).IsAssignableFrom(property.ValueEntry?.TypeOfValue);
			}

			/// <summary>
			///     執行驗證邏輯
			/// </summary>
			/// <param name="result">驗證結果</param>
			protected override void Validate(ValidationResult result)
			{
				if(Property.ValueEntry.WeakSmartValue is not IList list) return;
				if(list.Count < 2) return;

				for(var i = 1; i < list.Count; i++)
				{
					if(list[i - 1] is not IComparable previous || list[i] == null) continue;
					if(previous.CompareTo(list[i]) < 0) continue;

					result.AddError($"{Attribute.ErrorMessage}（第 {i} 項 {list[i - 1]} 未小於第 {i + 1} 項 {list[i]}）");
					return;
				}
			}
		}
	}
#endif