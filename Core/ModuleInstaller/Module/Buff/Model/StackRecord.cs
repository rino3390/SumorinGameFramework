using System.Collections.Generic;
using Sumorin.GameFramework.AttributeSystem;

namespace Sumorin.GameFramework.BuffSystem
{
	/// <summary>
	/// 記錄單一 Stack 產生的所有效果
	/// </summary>
	public class StackRecord
	{
		/// <summary>
		/// 效果列表
		/// </summary>
		public List<ModifyEffectInfo> Effects { get; } = new();

		/// <summary>
		/// 建立 StackRecord
		/// </summary>
		/// <param name="effects">效果列表</param>
		public StackRecord(List<ModifyEffectInfo> effects)
		{
			Effects.AddRange(effects);
		}
	}
}
