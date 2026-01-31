using System.Collections.Generic;

namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff 配置結構
	/// </summary>
	public struct BuffConfig
	{
		/// <summary>
		/// Buff 名稱
		/// </summary>
		public string BuffName;

		/// <summary>
		/// 生命週期類型
		/// </summary>
		public LifetimeType LifetimeType;

		/// <summary>
		/// 生命週期值（秒或回合數）
		/// </summary>
		public float Lifetime;

		/// <summary>
		/// 堆疊行為
		/// </summary>
		public StackBehavior StackBehavior;

		/// <summary>
		/// 最大堆疊數，-1 表示無上限
		/// </summary>
		public int MaxStack;

		/// <summary>
		/// 互斥群組名稱
		/// </summary>
		public string MutualExclusionGroup;

		/// <summary>
		/// 優先級（同互斥群組內比較）
		/// </summary>
		public int Priority;

		/// <summary>
		/// 效果列表
		/// </summary>
		public List<BuffEffectConfig> Effects;

		/// <summary>
		/// 標籤列表（用於分類，如 "Poison", "Debuff", "DoT"）
		/// </summary>
		public List<string> Tags
		{
			get => tags ?? new List<string>();
			set => tags = value;
		}

		private List<string> tags;
	}
}
