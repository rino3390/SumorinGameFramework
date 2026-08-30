using Sumorin.DDDCore;
using Zenject;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Domain 的命令入口，純轉發不含邏輯
	/// </summary>
	public class BuffCommandService
	{
		[Inject]
		private IBuffController controller;

		/// <inheritdoc cref="IBuffController.AddBuff" />
		public CommandResult AddBuff(string ownerId, string configId, string sourceId) => controller.AddBuff(ownerId, configId, sourceId);

		/// <inheritdoc cref="IBuffController.RemoveBuff" />
		public CommandResult RemoveBuff(string buffId) => controller.RemoveBuff(buffId);

		/// <inheritdoc cref="IBuffController.RemoveBuffsBySource" />
		public CommandResult RemoveBuffsBySource(string ownerId, string sourceId) => controller.RemoveBuffsBySource(ownerId, sourceId);

		/// <inheritdoc cref="IBuffController.RemoveBuffsByOwner" />
		public CommandResult RemoveBuffsByOwner(string ownerId) => controller.RemoveBuffsByOwner(ownerId);

		/// <inheritdoc cref="IBuffController.RemoveBuffsByTag" />
		public CommandResult RemoveBuffsByTag(string ownerId, string tag) => controller.RemoveBuffsByTag(ownerId, tag);

		/// <inheritdoc cref="IBuffController.TickTime" />
		public CommandResult TickTime(float deltaTime) => controller.TickTime(deltaTime);

		/// <inheritdoc cref="IBuffController.TickTurn" />
		public CommandResult TickTurn(string ownerId, int turns = 1) => controller.TickTurn(ownerId, turns);

		/// <inheritdoc cref="IBuffController.AdjustBuffLifetime" />
		public CommandResult AdjustBuffLifetime(string buffId, float delta) => controller.AdjustBuffLifetime(buffId, delta);

		/// <inheritdoc cref="IBuffController.SetBuffLifetime" />
		public CommandResult SetBuffLifetime(string buffId, float lifetime) => controller.SetBuffLifetime(buffId, lifetime);

		/// <inheritdoc cref="IBuffController.AddStack" />
		public CommandResult AddStack(string buffId) => controller.AddStack(buffId);

		/// <inheritdoc cref="IBuffController.RemoveStack" />
		public CommandResult RemoveStack(string buffId) => controller.RemoveStack(buffId);

		/// <inheritdoc cref="IBuffController.AdjustStack" />
		public CommandResult AdjustStack(string buffId, int delta) => controller.AdjustStack(buffId, delta);
	}
}