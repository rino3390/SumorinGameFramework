using System;

namespace Sumorin.Gm
{
	/// <summary>
	///     型別到欄位建構器的註冊表，遊戲側在此補上框架沒有內建的參數型別
	/// </summary>
	public interface IGmFieldCatalog
	{
		/// <summary>
		///     登記某個參數型別要用哪種欄位
		/// </summary>
		/// <remarks>
		///     登記與框架內建相同的型別時以這裡登記的為準。
		/// </remarks>
		/// <param name="valueType">參數型別</param>
		/// <param name="builder">欄位建構器</param>
		void Register(Type valueType, GmFieldBuilder builder);
	}
}