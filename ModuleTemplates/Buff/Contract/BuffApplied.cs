using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 已施加
	/// </summary>
	public class BuffApplied: IEvent
	{
		/// <summary>
		///     Buff Id
		/// </summary>
		public string BuffId { get; }

		/// <summary>
		///     擁有者 Id
		/// </summary>
		public string OwnerId { get; }

		/// <summary>
		///     Buff 配置 Id
		/// </summary>
		public string ConfigId { get; }

		/// <summary>
		///     來源 Id（施加者）
		/// </summary>
		public string SourceId { get; }

		/// <summary>
		///     建立 Buff 施加事實
		/// </summary>
		/// <param name="buffId">Buff Id</param>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">Buff 配置 Id</param>
		/// <param name="sourceId">來源 Id</param>
		public BuffApplied(string buffId, string ownerId, string configId, string sourceId)
		{
			BuffId = buffId;
			OwnerId = ownerId;
			ConfigId = configId;
			SourceId = sourceId;
		}
	}
}