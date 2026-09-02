using Sumorin.SumorinUtility;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性配置的數值面，只有 Controller 讀取
	/// </summary>
	public interface IAttributeConfig: IConfig
	{
		/// <summary>
		///     固定下限
		/// </summary>
		int Min { get; }

		/// <summary>
		///     固定上限
		/// </summary>
		int Max { get; }

		/// <summary>
		///     下限受哪個屬性影響，空字串表示使用固定下限
		/// </summary>
		string RelationMin { get; }

		/// <summary>
		///     上限受哪個屬性影響，空字串表示使用固定上限
		/// </summary>
		string RelationMax { get; }

		/// <summary>
		///     顯示轉換倍率，不影響 Domain 計算
		/// </summary>
		int Ratio { get; }
	}
}