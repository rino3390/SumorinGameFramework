#if UNITY_EDITOR
	using Sirenix.OdinInspector.Editor.Validation;
	using Sumorin.GameManagerBase.Editor;
	using UnityEditor;

	[assembly: RegisterValidator(typeof(DataScriptIdValidator))]

	namespace Sumorin.GameManagerBase.Editor
	{
		/// <summary>
		/// 檢查 DataScript 的識別碼，並提供產生識別碼的修復
		/// </summary>
		/// <remarks>
		/// 寫成 Validator 而不是欄位上的 <c>ValidateInput</c>，因為只有這條路徑能掛修復按鈕。
		/// 兩者並存會讓同一件事在 Validator 視窗報兩次，所以欄位上不再標 <c>ValidateInput</c>。
		/// </remarks>
		public class DataScriptIdValidator: ValueValidator<SODataBase>
		{
			/// <inheritdoc />
			protected override void Validate(ValidationResult result)
			{
				var data = Value;

				if(data == null || data.IsIdNameLegal()) return;

				result.AddError("識別碼不得為空，且只能是英數（含減號底線）").WithFix("產生識別碼", () => AssignGuid(data));
			}

			// 識別碼只要求唯一，取不出語意時給 GUID 即可，使用者要可讀的名稱再自行改寫
			private static void AssignGuid(SODataBase data)
			{
				data.IdName = SumorinUtility.GUID.NewGuid();
				EditorUtility.SetDirty(data);
				AssetDatabase.SaveAssets();
			}
		}
	}
#endif