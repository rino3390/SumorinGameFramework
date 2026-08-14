using System;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性的當前值快照，供 View 綁定
	/// </summary>
	/// <remarks>
	///     實作 <see cref="IEquatable{T}" /> 讓 ReactiveProperty 的相同值過濾走值比較而非反射。
	/// </remarks>
	public readonly struct AttributeValueInfo: IEquatable<AttributeValueInfo>
	{
		/// <summary>
		///     計算後的最終值
		/// </summary>
		public int Value { get; }

		/// <summary>
		///     當前下限
		/// </summary>
		public int MinValue { get; }

		/// <summary>
		///     當前上限
		/// </summary>
		public int MaxValue { get; }

		/// <summary>
		///     建立屬性值快照
		/// </summary>
		/// <param name="value">計算後的最終值</param>
		/// <param name="minValue">當前下限</param>
		/// <param name="maxValue">當前上限</param>
		public AttributeValueInfo(int value, int minValue, int maxValue)
		{
			Value = value;
			MinValue = minValue;
			MaxValue = maxValue;
		}

	#region IEquatable<AttributeValueInfo> Members
		/// <inheritdoc />
		public bool Equals(AttributeValueInfo other)
			=> Value == other.Value && MinValue == other.MinValue && MaxValue == other.MaxValue;
	#endregion

		/// <inheritdoc />
		public override bool Equals(object obj) => obj is AttributeValueInfo other && Equals(other);

		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(Value, MinValue, MaxValue);
	}
}
