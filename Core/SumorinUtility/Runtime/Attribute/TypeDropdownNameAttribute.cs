using System;

namespace Sumorin.SumorinUtility
{
	/// <summary>
	///     宣告型別在 Odin 型別下拉選單中的顯示名稱，由 Editor 端的 SumorinEditorUtility.TypeDropdown 讀取
	/// </summary>
	// Inherited = false：子類別若沿用父類別的名稱，下拉會出現兩個同名項目分不出來
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class TypeDropdownNameAttribute: Attribute
	{
		/// <summary>
		///     下拉選單顯示的名稱
		/// </summary>
		public string Name { get; }

		/// <summary>
		///     建立型別下拉顯示名稱屬性
		/// </summary>
		/// <param name="name">下拉選單顯示的名稱</param>
		public TypeDropdownNameAttribute(string name)
		{
			Name = name;
		}
	}
}