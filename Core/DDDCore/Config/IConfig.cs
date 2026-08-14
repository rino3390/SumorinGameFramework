namespace Sumorin.DDDCore
{
	/// <summary>
	///     配置數值的標記介面
	/// </summary>
	/// <remarks>
	///     各 Domain 於 Contract 定義自己的 <c>I{X}Config</c> 繼承本介面，只含數值不含 Unity 資源型別。
	///     不宣告 Id，配置的 id 是 <see cref="ConfigManager" /> 建字典時的鍵而非業務數值。
	/// </remarks>
	public interface IConfig
	{
	}
}
