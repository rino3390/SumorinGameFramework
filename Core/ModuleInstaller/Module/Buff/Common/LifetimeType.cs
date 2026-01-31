using Sirenix.OdinInspector;

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
		[LabelText("永久")]
		Permanent,

		/// <summary>
		/// 時間制（以秒計算）
		/// </summary>
		[LabelText("時間制")]
		TimeBased,

		/// <summary>
		/// 回合制（以回合數計算）
		/// </summary>
		[LabelText("回合制")]
		TurnBased
	}
}
