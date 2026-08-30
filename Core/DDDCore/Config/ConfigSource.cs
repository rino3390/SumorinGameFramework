using System.Collections.Generic;
using System.Linq;

namespace Sumorin.DDDCore
{
	/// <summary>
	///     一個模組貢獻給 <see cref="ConfigManager" /> 的配置來源
	/// </summary>
	/// <remarks>
	///     由模組 Installer 從自己的 DataScript 建立並綁定，一個模組一個實例。
	///     <see cref="ConfigManager" /> 解析時收集容器內所有來源，合併成單一查找字典。
	///     不直接綁定 <c>IEnumerable&lt;IConfig&gt;</c>，
	///     因為 Zenject 對 <c>IEnumerable&lt;&gt;</c> 等集合型別的單一注入另有展開規則，行為與 <c>ResolveAll</c> 不一致。
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