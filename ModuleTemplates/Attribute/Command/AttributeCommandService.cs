using System.Collections.Generic;
using Sumorin.DDDCore;
using VContainer;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Domain 的命令入口，純轉發不含邏輯
	/// </summary>
	public class AttributeCommandService
	{
		[Inject]
		private IAttributeController controller;

		/// <inheritdoc cref="IAttributeController.SetBaseValue" />
		public CommandResult SetBaseValue(string ownerId, string configId, int value) => controller.SetBaseValue(ownerId, configId, value);

		/// <inheritdoc cref="IAttributeController.AdjustBaseValue" />
		public CommandResult AdjustBaseValue(string ownerId, string configId, int delta) => controller.AdjustBaseValue(ownerId, configId, delta);

		/// <inheritdoc cref="IAttributeController.SetMinValue" />
		public CommandResult SetMinValue(string ownerId, string configId, int value) => controller.SetMinValue(ownerId, configId, value);

		/// <inheritdoc cref="IAttributeController.SetMaxValue" />
		public CommandResult SetMaxValue(string ownerId, string configId, int value) => controller.SetMaxValue(ownerId, configId, value);

		/// <inheritdoc cref="IAttributeController.AddModifier" />
		public CommandResult AddModifier(string ownerId, ModifyEffectInfo effect, string sourceId, string description = "") =>
			controller.AddModifier(ownerId, effect, sourceId, description);

		/// <inheritdoc cref="IAttributeController.AddModifiers" />
		public CommandResult AddModifiers(string ownerId, List<ModifyEffectInfo> effects, string sourceId, string description = "") =>
			controller.AddModifiers(ownerId, effects, sourceId, description);

		/// <inheritdoc cref="IAttributeController.RemoveModifierById" />
		public CommandResult RemoveModifierById(string ownerId, string configId, string modifierId) =>
			controller.RemoveModifierById(ownerId, configId, modifierId);

		/// <inheritdoc cref="IAttributeController.RemoveModifiersBySource" />
		public CommandResult RemoveModifiersBySource(string ownerId, string configId, string sourceId) =>
			controller.RemoveModifiersBySource(ownerId, configId, sourceId);

		/// <inheritdoc cref="IAttributeController.RemoveModifier" />
		public CommandResult RemoveModifier(string ownerId, ModifyEffectInfo effect, string sourceId) => controller.RemoveModifier(ownerId, effect, sourceId);

		/// <inheritdoc cref="IAttributeController.RemoveAllModifiersBySource" />
		public CommandResult RemoveAllModifiersBySource(string ownerId, string sourceId) => controller.RemoveAllModifiersBySource(ownerId, sourceId);

		/// <inheritdoc cref="IAttributeController.CreateAttribute" />
		public CommandResult CreateAttribute(string ownerId, string configId, int baseValue) => controller.CreateAttribute(ownerId, configId, baseValue);

		/// <inheritdoc cref="IAttributeController.RemoveAttribute" />
		public CommandResult RemoveAttribute(string ownerId, string configId) => controller.RemoveAttribute(ownerId, configId);

		/// <inheritdoc cref="IAttributeController.RemoveAttributesByOwner" />
		public CommandResult RemoveAttributesByOwner(string ownerId) => controller.RemoveAttributesByOwner(ownerId);
	}
}