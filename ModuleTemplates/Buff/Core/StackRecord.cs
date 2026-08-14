using System.Collections.Generic;
using Sumorin.Attribute;

namespace Sumorin.Buff
{
	/// <summary>
	///     記錄單一層數產生的所有效果
	/// </summary>
	public class StackRecord
	{
		/// <summary>
		///     效果列表
		/// </summary>
		public List<ModifyEffectInfo> Effects { get; } = new();

		/// <summary>
		///     建立層數紀錄
		/// </summary>
		/// <param name="effects">效果列表</param>
		public StackRecord(IEnumerable<ModifyEffectInfo> effects)
		{
			if(effects != null)
			{
				Effects.AddRange(effects);
			}
		}
	}
}