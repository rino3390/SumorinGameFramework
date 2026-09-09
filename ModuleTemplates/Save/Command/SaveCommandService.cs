using Sumorin.DDDCore;
using VContainer;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔 Domain 的命令入口，純轉發不含邏輯
	/// </summary>
	public class SaveCommandService
	{
		[Inject]
		private ISaveController controller;

		/// <inheritdoc cref="ISaveController.Save" />
		public CommandResult Save(string slotId, string description) => controller.Save(slotId, description);

		/// <inheritdoc cref="ISaveController.Load" />
		public CommandResult Load(string slotId) => controller.Load(slotId);

		/// <inheritdoc cref="ISaveController.Delete" />
		public CommandResult Delete(string slotId) => controller.Delete(slotId);

		/// <inheritdoc cref="ISaveController.NewGame" />
		public CommandResult NewGame() => controller.NewGame();

		/// <inheritdoc cref="ISaveController.SaveGlobal" />
		public CommandResult SaveGlobal() => controller.SaveGlobal();

		/// <inheritdoc cref="ISaveController.LoadGlobal" />
		public CommandResult LoadGlobal() => controller.LoadGlobal();
	}
}