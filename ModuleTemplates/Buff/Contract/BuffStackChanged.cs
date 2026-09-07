using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 層數已變化
	/// </summary>
	public class BuffStackChanged: IEvent
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
		///     變化前層數
		/// </summary>
		public int OldStack { get; }

		/// <summary>
		///     變化後層數
		/// </summary>
		public int NewStack { get; }

		/// <summary>
		///     建立 Buff 層數變化事實
		/// </summary>
		/// <param name="buffId">Buff Id</param>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">Buff 配置 Id</param>
		/// <param name="oldStack">變化前層數</param>
		/// <param name="newStack">變化後層數</param>
		public BuffStackChanged(string buffId, string ownerId, string configId, int oldStack, int newStack)
		{
			BuffId = buffId;
			OwnerId = ownerId;
			ConfigId = configId;
			OldStack = oldStack;
			NewStack = newStack;
		}
	}
}