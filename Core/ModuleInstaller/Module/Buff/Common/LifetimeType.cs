namespace Rino.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff 生命週期類型
	/// </summary>
	public enum LifetimeType
	{
		/// <summary>
		/// 永久（不會自動過期）
		/// </summary>
		Permanent,

		/// <summary>
		/// 時間制（以秒計算）
		/// </summary>
		TimeBased,

		/// <summary>
		/// 回合制（以回合數計算）
		/// </summary>
		TurnBased
	}
}
