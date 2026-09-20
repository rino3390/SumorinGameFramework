using System;
using System.Collections.Generic;
using System.Linq;
using Sumorin.SumorinUtility;

namespace Sumorin.Gm
{
	/// <summary>
	///     配置 Id 欄位的查找邏輯，判斷型別家族、取候選清單、把選到的 Id 包回原型別
	/// </summary>
	/// <remarks>
	///     刻意不放在欄位 View 內，候選清單挑得對不對才寫得了 EditMode 測試。
	/// </remarks>
	public static class GmConfigIdLookup
	{
		/// <summary>
		///     判斷型別是否屬於配置 Id 家族
		/// </summary>
		/// <param name="type">要判斷的型別</param>
		/// <returns>是 <see cref="GmConfigId{TConfig}" /> 的封閉泛型則為 true</returns>
		public static bool Matches(Type type) => type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(GmConfigId<>);

		/// <summary>
		///     取該配置型別的全部 Id
		/// </summary>
		/// <param name="configIdType">配置 Id 型別，即 <see cref="GmConfigId{TConfig}" /> 的封閉泛型</param>
		/// <param name="configs">配置查找入口</param>
		/// <returns>候選 Id，沒有符合的配置時為空集合</returns>
		public static IReadOnlyList<string> CandidatesOf(Type configIdType, ConfigManager configs)
		{
			var configType = configIdType.GetGenericArguments()[0];

			return configs.GetAll<IConfig>().Where(pair => configType.IsInstanceOfType(pair.Value)).Select(pair => pair.Key).ToList();
		}

		/// <summary>
		///     把選到的 Id 包成對應的 <see cref="GmConfigId{TConfig}" />
		/// </summary>
		/// <param name="configIdType">配置 Id 型別</param>
		/// <param name="id">選到的 Id，候選清單為空時為 null</param>
		/// <returns>包裝後的值</returns>
		public static object Wrap(Type configIdType, string id) => Activator.CreateInstance(configIdType, new object[] { id });
	}
}
