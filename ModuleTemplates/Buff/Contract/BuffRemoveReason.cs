namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 移除原因
	/// </summary>
	public enum BuffRemoveReason
	{
		/// <summary>
		///     由呼叫端主動移除
		/// </summary>
		Manual,

		/// <summary>
		///     施加來源被移除而連帶移除
		/// </summary>
		SourceRemoved,

		/// <summary>
		///     時效或層數歸零而過期
		/// </summary>
		Expired,

		/// <summary>
		///     被同名或互斥群組的 Buff 取代
		/// </summary>
		Replaced,

		/// <summary>
		///     依標籤批次移除
		/// </summary>
		TagRemoved
	}
}
