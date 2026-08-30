using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sumorin.SumorinUtility.Editor;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 提供可選編輯器頁籤的下拉選單資料來源
	/// </summary>
	/// <remarks>
	/// 標註 <see cref="DataEditorConfigAttribute" /> 的 DataScript 有兩種頁籤，兩者不並列於同一個下拉。
	/// <see cref="DynamicDataEditor{T}" /> 是逐筆模式，供 GameManager 當獨立頁籤。
	/// <see cref="DataSetListEditor{T}" /> 是清單模式，供設定頁當內嵌項目。
	/// 另一種來源是自行繼承 <see cref="GameEditorMenuBase" /> 的具名類別，兩個下拉都收。
	/// </remarks>
	public static class EditorMenuTypeProvider
	{
		/// <summary>
		/// 取得可當獨立頁籤的編輯器，不含清單模式
		/// </summary>
		/// <remarks>
		/// 清單模式是給設定頁內嵌用的，單獨當頁籤時逐筆模式的左側選單樹好用得多。
		/// </remarks>
		/// <param name="excludedTypes">要排除的頁籤型別，用於排除呼叫端自己</param>
		/// <returns>下拉選單項目，顯示頁籤名稱、值為頁籤型別</returns>
		public static List<ValueDropdownItem> GetMenuTypes(params Type[] excludedTypes)
		{
			return ItemEditors().Concat(NamedEditors(excludedTypes)).ToList();
		}

		/// <summary>
		/// 取得可嵌入設定頁的編輯器頁籤，不含逐筆模式
		/// </summary>
		/// <remarks>
		/// 逐筆模式自帶左側選單樹，巢狀在設定頁的子選單裡會與外層選單打架。
		/// 清單模式只繪製單一頁面，才適合當設定頁的一個項目。
		/// </remarks>
		/// <param name="excludedTypes">要排除的頁籤型別，用於排除呼叫端自己</param>
		/// <returns>下拉選單項目，顯示頁籤名稱、值為頁籤型別</returns>
		public static List<ValueDropdownItem> GetSettingMenuTypes(params Type[] excludedTypes)
		{
			return ListEditors().Concat(NamedEditors(excludedTypes)).ToList();
		}

		private static IEnumerable<ValueDropdownItem> ItemEditors()
		{
			return DataTypes().Select(dataType => new ValueDropdownItem(TabNameOf(dataType), typeof(DynamicDataEditor<>).MakeGenericType(dataType)));
		}

		private static IEnumerable<ValueDropdownItem> ListEditors()
		{
			return DataTypes().Select(dataType => new ValueDropdownItem(TabNameOf(dataType), typeof(DataSetListEditor<>).MakeGenericType(dataType)));
		}

		private static IEnumerable<ValueDropdownItem> NamedEditors(Type[] excludedTypes)
		{
			return SumorinEditorUtility.GetDerivedClasses<GameEditorMenuBase>(excludeGenericBase: typeof(DynamicDataEditor<>))
									   .Where(type => !excludedTypes.Contains(type))
									   .Select(type => new ValueDropdownItem(CreateInstance(type)?.TabName ?? type.Name, type));
		}

		private static List<Type> DataTypes()
		{
			return SumorinEditorUtility.GetTypesWithAttribute<SODataBase, DataEditorConfigAttribute>();
		}

		private static string TabNameOf(Type dataType)
		{
			return dataType.GetCustomAttribute<DataEditorConfigAttribute>().TabName;
		}

		private static GameEditorMenuBase CreateInstance(Type type)
		{
			return Activator.CreateInstance(type) as GameEditorMenuBase;
		}
	}
}