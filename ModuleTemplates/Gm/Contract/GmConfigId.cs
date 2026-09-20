using Sumorin.SumorinUtility;

namespace Sumorin.Gm
{
	/// <summary>
	///     配置 Id 的包裝型別，讓 GM 操作的參數表達「這個字串是某種配置的 Id」
	/// </summary>
	/// <remarks>
	///     面板以此型別參數生成該配置型別全部 Id 的下拉。
	///     可隱式轉為 <see cref="string" />，登記的委派直接把它傳給 CommandService 即可。
	/// </remarks>
	/// <typeparam name="TConfig">配置介面型別，如 <c>IBuffConfig</c></typeparam>
	public readonly struct GmConfigId<TConfig> where TConfig: IConfig
	{
		/// <summary>
		///     配置 Id
		/// </summary>
		public string Value { get; }

		/// <summary>
		///     建立配置 Id 包裝
		/// </summary>
		/// <param name="value">配置 Id</param>
		public GmConfigId(string value)
		{
			Value = value;
		}

		public static implicit operator string(GmConfigId<TConfig> id) => id.Value;

		/// <inheritdoc />
		public override string ToString() => Value;
	}
}