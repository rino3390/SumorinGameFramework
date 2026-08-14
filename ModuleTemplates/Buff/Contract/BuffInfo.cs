using System;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 的當前狀態快照，供表現層讀取
	/// </summary>
	public readonly struct BuffInfo: IEquatable<BuffInfo>
	{
		/// <summary>
		///     Buff 識別碼
		/// </summary>
		public string BuffId { get; }

		/// <summary>
		///     Buff 名稱
		/// </summary>
		public string BuffName { get; }

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
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="buffName">Buff 名稱</param>
		/// <param name="stackCount">當前層數</param>
		/// <param name="lifetimeType">生命週期類型</param>
		/// <param name="remainingLifetime">剩餘時效</param>
		public BuffInfo(string buffId, string buffName, int stackCount, LifetimeType lifetimeType, float remainingLifetime)
		{
			BuffId = buffId;
			BuffName = buffName;
			StackCount = stackCount;
			LifetimeType = lifetimeType;
			RemainingLifetime = remainingLifetime;
		}

	#region IEquatable<BuffInfo> Members
		/// <inheritdoc />
		public bool Equals(BuffInfo other)
			=> BuffId == other.BuffId
			&& BuffName == other.BuffName
			&& StackCount == other.StackCount
			&& LifetimeType == other.LifetimeType
			&& RemainingLifetime.Equals(other.RemainingLifetime);
	#endregion

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is BuffInfo other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(BuffId, BuffName, StackCount, LifetimeType, RemainingLifetime);
	}
}
