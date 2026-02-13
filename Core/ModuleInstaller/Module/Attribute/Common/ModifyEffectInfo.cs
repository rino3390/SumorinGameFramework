namespace Sumorin.GameFramework.AttributeSystem
{
	/// <summary>
	/// 屬性修改效果資訊，描述如何修改一個屬性
	/// </summary>
	public struct ModifyEffectInfo
	{
		/// <summary>
		/// 目標屬性名稱
		/// </summary>
		public string AttributeName;

		/// <summary>
		/// 修改類型
		/// </summary>
		public ModifyType ModifyType;

		/// <summary>
		/// 修改數值
		/// </summary>
		public int Value;
	}
}
