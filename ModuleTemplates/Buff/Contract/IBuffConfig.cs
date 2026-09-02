using System.Collections.Generic;
using Sumorin.Attribute;
using Sumorin.SumorinUtility;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 配置的數值面，只有 Controller 讀取
	/// </summary>
	public interface IBuffConfig: IConfig
	{
		/// <summary>
		///     生命週期類型
		/// </summary>
		LifetimeType LifetimeType { get; }

		/// <summary>
		///     時效長度（秒或回合數）
		/// </summary>
		float Lifetime { get; }

		/// <summary>
		///     重複獲得時的堆疊行為
		/// </summary>
		StackBehavior StackBehavior { get; }

		/// <summary>
		///     層數上限，負值表示無上限
		/// </summary>
		int MaxStack { get; }

		/// <summary>
		///     互斥群組名稱，空字串表示不互斥
		/// </summary>
		string MutualExclusionGroup { get; }

		/// <summary>
		///     互斥優先度，同群組內數值高者留下
		/// </summary>
		int Priority { get; }

		/// <summary>
		///     每層產生的屬性修改效果
		/// </summary>
		IReadOnlyList<ModifyEffectInfo> Effects { get; }

		/// <summary>
		///     分類標籤，供依標籤批次移除使用
		/// </summary>
		IReadOnlyList<string> Tags { get; }

		/// <summary>
		///     時效到期時是否移除全部層數，false 表示只移除一層並重置時效
		/// </summary>
		bool RemoveAllOnExpire { get; }
	}
}