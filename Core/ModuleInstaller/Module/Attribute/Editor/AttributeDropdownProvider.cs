using System.Collections.Generic;
using System.Linq;
using Sumorin.GameFramework.SumorinUtility.Editor;

namespace Sumorin.GameFramework.AttributeSystem
{
	/// <summary>
	/// 提供屬性名稱下拉選單的資料來源
	/// </summary>
	public static class AttributeDropdownProvider
	{
		/// <summary>
		/// 取得所有已定義的屬性名稱
		/// </summary>
		public static IEnumerable<string> GetAttributeNames() => GetAttributeNames("");

		/// <summary>
		/// 取得所有已定義的屬性名稱（排除指定名稱）
		/// </summary>
		/// <param name="excludeName">要排除的屬性名稱，為空字串時不過濾</param>
		public static IEnumerable<string> GetAttributeNames(string excludeName)
		{
			var settingData = SumorinEditorUtility.FindAsset<AttributeSettingData>();
			if (settingData == null) return new[] { "" };

			var names = settingData.Attributes
				.Select(x => x.Id)
				.Where(x => !string.IsNullOrEmpty(x) && (excludeName == "" || x != excludeName));

			return new[] { "" }.Concat(names);
		}
	}
}
