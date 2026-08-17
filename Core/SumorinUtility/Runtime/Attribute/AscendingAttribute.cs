using System;

namespace Sumorin.SumorinUtility
{
	/// <summary>
	///     標記清單必須嚴格遞增，用於打點時間這類先後有意義的序列。
	///     數值下限請直接用 Odin 內建的 [MinValue]，本屬性不重複處理
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class AscendingAttribute: Attribute
	{
		/// <summary>
		///     順序不符時顯示的錯誤訊息
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		///     建立遞增驗證屬性
		/// </summary>
		/// <param name="errorMessage">順序不符時的錯誤訊息</param>
		public AscendingAttribute(string errorMessage = "清單必須由小到大嚴格遞增")
		{
			ErrorMessage = errorMessage;
		}
	}
}