using System.Collections.Generic;
using System.Linq;

namespace Sumorin.SumorinUtility
{
	/// <summary>
	///     一個模組貢獻給 <see cref="ConfigManager" /> 的配置來源
	/// </summary>
	/// <remarks>
	///     由模組 Installer 從自己的 DataScript 建立並註冊，一個模組一個實例。
	///     註冊要用 <c>Lifetime.Scoped</c> 的工廠，不能用 <c>RegisterInstance</c>。
	///     VContainer 把同型別的 Singleton 重複註冊視為衝突，Scoped 的重複註冊才會被收成集合。
	///     <see cref="ConfigManager" /> 解析時以 <c>IEnumerable&lt;ConfigSource&gt;</c> 收集容器內所有來源，合併成單一查找字典。
	///     不直接註冊 <c>IEnumerable&lt;IConfig&gt;</c>，
	///     容器把 <c>IEnumerable&lt;T&gt;</c> 解析成「所有 T 的註冊」，各模組的整批配置會與逐筆註冊的 <c>IConfig</c> 混在一起。
	/// </remarks>
	public class ConfigSource
	{
		/// <summary>
		///     本來源提供的配置
		/// </summary>
		public IEnumerable<IConfig> Configs { get; }

		/// <summary>
		///     建立配置來源
		/// </summary>
		/// <param name="configs">配置清單，傳入 null 時視為沒有配置</param>
		public ConfigSource(IEnumerable<IConfig> configs)
		{
			Configs = configs ?? Enumerable.Empty<IConfig>();
		}
	}
}