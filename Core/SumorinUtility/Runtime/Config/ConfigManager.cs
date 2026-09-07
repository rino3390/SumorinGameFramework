using System.Collections.Generic;
using System.Linq;

namespace Sumorin.SumorinUtility
{
	/// <summary>
	///     配置的查找入口，Controller 取數值介面，Presenter 與 View 取具體 SO
	/// </summary>
	/// <remarks>
	///     不抽介面，全專案只有這一個實作。測試直接 new 一份帶假配置的實例。
	/// </remarks>
	public class ConfigManager
	{
		private readonly Dictionary<string, IConfig> configs;

		/// <summary>
		///     建立 ConfigManager
		/// </summary>
		/// <remarks>
		///     查找鍵取自配置自己的 <see cref="IConfig.Id" />，不另外指定，鍵與配置 Id 因此不會分歧。
		/// </remarks>
		/// <param name="configs">配置清單，由 Installer 從 DataScript 讀出</param>
		public ConfigManager(IEnumerable<IConfig> configs)
		{
			this.configs = configs?.ToDictionary(config => config.Id) ?? new Dictionary<string, IConfig>();
		}

		/// <summary>
		///     取得指定 id 的配置
		/// </summary>
		/// <typeparam name="TConfig">配置介面型別</typeparam>
		/// <param name="id">配置 Id</param>
		/// <returns>配置內容，id 不存在或型別不符時回傳 null</returns>
		public TConfig Get<TConfig>(string id) where TConfig: class, IConfig
		{
			if(id == null) return null;

			return configs.TryGetValue(id, out var config) ? config as TConfig : null;
		}

		/// <summary>
		///     取得指定型別的所有配置及其 Id
		/// </summary>
		/// <typeparam name="TConfig">配置介面型別</typeparam>
		/// <returns>符合型別的所有配置，沒有時回傳空集合</returns>
		public IEnumerable<KeyValuePair<string, TConfig>> GetAll<TConfig>() where TConfig: class, IConfig
		{
			return configs.Where(pair => pair.Value is TConfig).Select(pair => new KeyValuePair<string, TConfig>(pair.Key, (TConfig)pair.Value));
		}
	}
}