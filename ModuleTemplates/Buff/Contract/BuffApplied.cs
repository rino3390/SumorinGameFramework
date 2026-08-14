using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 已施加
	/// </summary>
	public class BuffApplied: IEvent
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
		///     來源識別碼（施加者）
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		///     建立 Buff 施加事實
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="buffName">Buff 名稱</param>
		/// <param name="sourceId">來源識別碼</param>
		public BuffApplied(string buffId, string ownerId, string buffName, string sourceId)
		{
			BuffId = buffId;
			OwnerId = ownerId;
			BuffName = buffName;
			SourceId = sourceId;
		}
	}
}
