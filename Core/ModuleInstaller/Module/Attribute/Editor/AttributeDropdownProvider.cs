using System.Collections.Generic;
using System.Linq;
using Rino.GameFramework.RinoUtility.Editor;

namespace Rino.GameFramework.AttributeSystem
{
	/// <summary>
	/// 提供屬性名稱下拉選單的資料來源
	/// </summary>
	public static class AttributeDropdownProvider
	{
		/// <summary>
		/// 取得所有已定義的屬性名稱（排除指定名稱）
		/// </summary>
		/// <param name="excludeName">要排除的屬性名稱</param>
		public static IEnumerable<string> GetAttributeNames(string excludeName)
		{
			var settingData = RinoEditorUtility.FindAsset<AttributeSettingData>();
			if (settingData == null) return new[] { "" };

			var names = settingData.Attributes
				.Select(x => x.Id)
				.Where(x => !string.IsNullOrEmpty(x) && x != excludeName);

			return new[] { "" }.Concat(names);
		}
	}
}
