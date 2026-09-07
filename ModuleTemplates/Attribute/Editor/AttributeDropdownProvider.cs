using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sumorin.SumorinUtility.Editor;

namespace Sumorin.Attribute
{
	/// <summary>
	/// 提供屬性下拉選單的資料來源
	/// </summary>
	/// <remarks>
	/// 選項顯示 <c>EditorLabel</c>，實際存入的值是 <c>Id</c>。
	/// 不使用 <c>DataSet&lt;T&gt;.DrawValueDropDown</c>，該方法以泛型型別名稱搜尋資產，找不到衍生的集合資產。
	/// </remarks>
	public static class AttributeDropdownProvider
	{
		/// <summary>
		/// 取得所有已定義的屬性
		/// </summary>
		public static IEnumerable<ValueDropdownItem> GetAttributes() => GetAttributes("");

		/// <summary>
		/// 取得所有已定義的屬性（排除指定 Id）
		/// </summary>
		/// <param name="excludeId">要排除的屬性 Id，為空字串時不過濾</param>
		public static IEnumerable<ValueDropdownItem> GetAttributes(string excludeId)
		{
			var items = SumorinEditorUtility.FindAssets<AttributeData>()
											.Where(data => !string.IsNullOrEmpty(data.Id) && (excludeId == "" || data.Id != excludeId))
											.Select(data => new ValueDropdownItem(data.AssetName, data.Id));

			return new[] { new ValueDropdownItem("（不指定）", "") }.Concat(items);
		}
	}
}