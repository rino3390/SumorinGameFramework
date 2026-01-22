namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff 資訊結構，用於 Controller Observable 通知 Presenter 更新 UI
	/// </summary>
	public struct BuffInfo
	{
		/// <summary>
		/// Buff 識別碼
		/// </summary>
		public string BuffId;

		/// <summary>
		/// Buff 名稱
		/// </summary>
		public string BuffName;

		/// <summary>
		/// 當前堆疊數
		/// </summary>
		public int StackCount;

		/// <summary>
		/// 生命週期類型
		/// </summary>
		public LifetimeType LifetimeType;

		/// <summary>
		/// 剩餘生命週期（秒或回合數）
		/// </summary>
		public float RemainingLifetime;

		public BuffInfo(string buffId, string buffName, int stackCount, LifetimeType lifetimeType, float remainingLifetime)
		{
			BuffId = buffId;
			BuffName = buffName;
			StackCount = stackCount;
			LifetimeType = lifetimeType;
			RemainingLifetime = remainingLifetime;
		}
	}
}
