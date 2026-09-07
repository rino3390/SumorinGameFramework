using Sirenix.OdinInspector;

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
		[LabelText("獨立存在")]
		Independent,

		/// <summary>
		///     重置時間，層數不變
		/// </summary>
		[LabelText("重置時間")]
		RefreshDuration,

		/// <summary>
		///     增加層數，同時重置時間
		/// </summary>
		[LabelText("增加層數")]
		IncreaseStack,

		/// <summary>
		///     取代舊的
		/// </summary>
		[LabelText("取代")]
		Replace
	}
}