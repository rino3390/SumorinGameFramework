using System;
using System.Collections.Generic;
using Sumorin.SumorinUtility;

namespace Sumorin.Gm
{
	/// <summary>
	///     型別到欄位建構器的註冊表
	/// </summary>
	/// <remarks>
	///     比對依序為遊戲側註冊的精確型別、框架內建的精確型別、框架的型別家族。
	///     家族指一種欄位涵蓋多個型別的情況，列舉與配置 Id 各算一族。
	/// </remarks>
	public class GmFieldCatalog: IGmFieldCatalog
	{
		private readonly Dictionary<Type, GmFieldBuilder> gameTypes = new();
		private readonly Dictionary<Type, GmFieldBuilder> builtInTypes = new();
		private readonly List<(Func<Type, bool> Matches, GmFieldBuilder Builder)> builtInFamilies = new();

		/// <summary>
		///     建立欄位目錄，填入框架內建欄位後套用遊戲側的登記
		/// </summary>
		/// <param name="configs">配置查找入口，配置 Id 欄位的候選清單來源</param>
		/// <param name="registrations">遊戲側的欄位登記，沒有自訂型別時可省略</param>
		public GmFieldCatalog(ConfigManager configs, IReadOnlyList<IGmFieldRegistration> registrations = null)
		{
			builtInTypes[typeof(int)] = GmBuiltInFields.Int;
			builtInTypes[typeof(float)] = GmBuiltInFields.Float;
			builtInTypes[typeof(bool)] = GmBuiltInFields.Bool;
			builtInTypes[typeof(string)] = GmBuiltInFields.String;

			builtInFamilies.Add((type => type.IsEnum, GmBuiltInFields.Enum));
			builtInFamilies.Add((GmConfigIdLookup.Matches, GmBuiltInFields.CreateConfigId(configs)));

			if(registrations == null) return;

			foreach(var registration in registrations)
			{
				registration.Register(this);
			}
		}

	#region IGmFieldCatalog Members
		/// <inheritdoc />
		public void Register(Type valueType, GmFieldBuilder builder)
		{
			gameTypes[valueType] = builder;
		}
	#endregion

		/// <summary>
		///     找出參數要用哪個欄位建構器
		/// </summary>
		/// <param name="operation">參數所屬的操作，只用於例外訊息</param>
		/// <param name="parameter">參數規格</param>
		/// <returns>欄位建構器</returns>
		/// <exception cref="InvalidOperationException">沒有型別對應的欄位</exception>
		public GmFieldBuilder Resolve(GmOperation operation, GmParameter parameter)
		{
			if(gameTypes.TryGetValue(parameter.Type, out var gameBuilder)) return gameBuilder;
			if(builtInTypes.TryGetValue(parameter.Type, out var builtInBuilder)) return builtInBuilder;

			foreach(var family in builtInFamilies)
			{
				if(family.Matches(parameter.Type)) return family.Builder;
			}

			throw new InvalidOperationException($"GM 操作「{operation.FullName}」的參數「{parameter.Name}」型別 {parameter.Type.Name} 沒有對應的欄位，請在 GM 欄位目錄註冊");
		}
	}
}