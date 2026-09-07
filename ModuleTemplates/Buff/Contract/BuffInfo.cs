using System;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 的當前狀態快照，供表現層讀取
	/// </summary>
	public readonly struct BuffInfo: IEquatable<BuffInfo>
	{
		/// <summary>
		///     Buff Id
		/// </summary>
		public string BuffId { get; }

		/// <summary>
		///     Buff 配置 Id
		/// </summary>
		public string ConfigId { get; }

		/// <summary>
		///     當前層數
		/// </summary>
		public int StackCount { get; }

		/// <summary>
		///     生命週期類型
		/// </summary>
		public LifetimeType LifetimeType { get; }

		/// <summary>
		///     剩餘時效（秒或回合數）
		/// </summary>
		public float RemainingLifetime { get; }

		/// <summary>
		///     建立 Buff 狀態快照
		/// </summary>
		/// <param name="buffId">Buff Id</param>
		/// <param name="configId">Buff 配置 Id</param>
		/// <param name="stackCount">當前層數</param>
		/// <param name="lifetimeType">生命週期類型</param>
		/// <param name="remainingLifetime">剩餘時效</param>
		public BuffInfo(string buffId, string configId, int stackCount, LifetimeType lifetimeType, float remainingLifetime)
		{
			BuffId = buffId;
			ConfigId = configId;
			StackCount = stackCount;
			LifetimeType = lifetimeType;
			RemainingLifetime = remainingLifetime;
		}

	#region IEquatable<BuffInfo> Members
		/// <inheritdoc />
		public bool Equals(BuffInfo other) =>
			BuffId == other.BuffId
			&& ConfigId == other.ConfigId
			&& StackCount == other.StackCount
			&& LifetimeType == other.LifetimeType
			&& RemainingLifetime.Equals(other.RemainingLifetime);
	#endregion

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is BuffInfo other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(BuffId, ConfigId, StackCount, LifetimeType, RemainingLifetime);
	}
}