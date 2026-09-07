using Sirenix.OdinInspector;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性種類，決定基礎值的語意與允許的操作
	/// </summary>
	/// <remarks>
	///     最終值公式是 (基礎值 + Flat 總和) × (1 + Percent / 100) × Multiple。
	///     資源型屬性若掛上 Percent 修改器，代表傷害或治療的 Flat 會一起被放大，因此兩種用途不能疊在同一個屬性上。
	/// </remarks>
	public enum AttributeKind
	{
		/// <summary>
		///     數值型（ATK、DEF、MaxHP）。基礎值是修改器套用前的底數，效果一律掛修改器，移除即還原
		/// </summary>
		[LabelText("數值型")]
		Numeric,

		/// <summary>
		///     資源型（HP、ENG、Shield）。基礎值是當前量，以增減命令一次性變動，不接受修改器
		/// </summary>
		[LabelText("資源型")]
		Resource
	}
}