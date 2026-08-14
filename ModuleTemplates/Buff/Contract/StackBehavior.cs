namespace Sumorin.Buff
{
	/// <summary>
	///     重複獲得同名 Buff 時的堆疊行為
	/// </summary>
	public enum StackBehavior
	{
		/// <summary>
		///     獨立存在，各自計時
		/// </summary>
		Independent,

		/// <summary>
		///     刷新時效，層數不變
		/// </summary>
		RefreshDuration,

		/// <summary>
		///     增加層數，同時刷新時效
		/// </summary>
		IncreaseStack,

		/// <summary>
		///     覆蓋舊的
		/// </summary>
		Replace
	}
}
