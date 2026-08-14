using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 已移除
	/// </summary>
	/// <remarks>
	///     移除時 Controller 已一併撤除該 Buff 掛上的所有 Modifier，酬載不再帶層數紀錄。
	/// </remarks>
	public class BuffRemoved: IEvent
	{
		/// <summary>
		///     Buff 識別碼
		/// </summary>
		public string BuffId { get; }

		/// <summary>
		///     擁有者識別碼
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		///     Buff 名稱
		/// </summary>
		public string BuffName { get; }

		/// <summary>
		///     移除原因
		/// </summary>
		public BuffRemoveReason Reason { get; }

		/// <summary>
		///     建立 Buff 移除事實
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="buffName">Buff 名稱</param>
		/// <param name="reason">移除原因</param>
		public BuffRemoved(string buffId, string ownerId, string buffName, BuffRemoveReason reason)
		{
			BuffId = buffId;
			OwnerId = ownerId;
			BuffName = buffName;
			Reason = reason;
		}
	}
}
