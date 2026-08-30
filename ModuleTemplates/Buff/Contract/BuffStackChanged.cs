using Sumorin.DDDCore;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 層數已變化
	/// </summary>
	public class BuffStackChanged: IEvent
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
		///     Buff 配置識別碼
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
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="configId">Buff 配置識別碼</param>
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