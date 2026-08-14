using System.Collections.Generic;
using Sumorin.Attribute;

namespace Sumorin.Buff.Tests
{
	/// <summary>
	///     測試用的 Buff 配置替身
	/// </summary>
	public class FakeBuffConfig: IBuffConfig
	{
		public LifetimeType LifetimeType { get; }
		public float Lifetime { get; }
		public StackBehavior StackBehavior { get; }
		public int MaxStack { get; }
		public string MutualExclusionGroup { get; }
		public int Priority { get; }
		public IReadOnlyList<ModifyEffectInfo> Effects { get; }
		public IReadOnlyList<string> Tags { get; }
		public bool RemoveAllOnExpire { get; }

		public FakeBuffConfig(LifetimeType lifetimeType = LifetimeType.TimeBased,
							  float lifetime = 10f,
							  StackBehavior stackBehavior = StackBehavior.RefreshDuration,
							  int maxStack = -1,
							  string mutualExclusionGroup = "",
							  int priority = 0,
							  IReadOnlyList<ModifyEffectInfo> effects = null,
							  IReadOnlyList<string> tags = null,
							  bool removeAllOnExpire = true)
		{
			LifetimeType = lifetimeType;
			Lifetime = lifetime;
			StackBehavior = stackBehavior;
			MaxStack = maxStack;
			MutualExclusionGroup = mutualExclusionGroup;
			Priority = priority;
			Effects = effects ?? DefaultEffects;
			Tags = tags ?? new List<string>();
			RemoveAllOnExpire = removeAllOnExpire;
		}

		/// <summary>
		///     預設效果：Health +10
		/// </summary>
		public static readonly IReadOnlyList<ModifyEffectInfo> DefaultEffects = new List<ModifyEffectInfo>
		{
			new() { AttributeName = "Health", ModifyType = ModifyType.Flat, Value = 10 }
		};
	}
}
