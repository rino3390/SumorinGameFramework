#if UNITY_EDITOR
	using Sirenix.OdinInspector.Editor.Validation;
	using Sumorin.GameManagerBase.Editor;
	using UnityEditor;

	[assembly: RegisterValidator(typeof(DataScriptIdValidator))]

	namespace Sumorin.GameManagerBase.Editor
	{
		/// <summary>
		/// 檢查 DataScript 的 Id，並提供產生 Id 的修復
		/// </summary>
		/// <remarks>
		/// 寫成 Validator 而不是欄位上的 <c>ValidateInput</c>，因為只有這條路徑能掛修復按鈕。
		/// 兩者並存會讓同一件事在 Validator 視窗報兩次，所以欄位上不再標 <c>ValidateInput</c>。
		/// 取 <c>RootObjectValidator</c> 而非 <c>ValueValidator</c>，後者連巢狀的參照一起驗。
		/// DataSet 的清單持有每一份資料，那會讓同一筆錯誤在資料本身與 DataSet 上各報一次。
		/// </remarks>
		public class DataScriptIdValidator: RootObjectValidator<SODataBase>
		{
			/// <inheritdoc />
			protected override void Validate(ValidationResult result)
			{
				var data = Value;

				if(data == null || data.IsIdLegal()) return;

				result.AddError("Id 不得為空，且只能是英數（含減號底線）").WithFix("產生 Id", () => AssignGuid(data));
			}

			// Id 只要求唯一，取不出語意時給 GUID 即可，使用者要可讀的名稱再自行改寫
			private static void AssignGuid(SODataBase data)
			{
				data.Id = SumorinUtility.GUID.NewGuid();
				EditorUtility.SetDirty(data);
				AssetDatabase.SaveAssets();
			}
		}
	}
#endif