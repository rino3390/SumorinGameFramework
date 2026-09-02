namespace Sumorin.SumorinUtility
{
	/// <summary>
	///     配置數值的介面
	/// </summary>
	/// <remarks>
	///     各 Domain 於 Contract 定義自己的 <c>I{X}Config</c> 繼承本介面，只含數值不含 Unity 資源型別。
	///     <see cref="Id" /> 即 <see cref="ConfigManager" /> 的查找鍵，數值面與具體 SO 用同一個鍵取得。
	///     DataScript 繼承 <c>SODataBase</c> 即已具備 <see cref="Id" />，不需重複宣告。
	/// </remarks>
	public interface IConfig
	{
		/// <summary>
		///     配置識別碼，全專案唯一
		/// </summary>
		string Id { get; }
	}
}