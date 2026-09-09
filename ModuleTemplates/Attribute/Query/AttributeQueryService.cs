using R3;
using VContainer;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Domain 的查詢入口，純轉發不含邏輯
	/// </summary>
	public class AttributeQueryService: IAttributeValueService, IAttributeQueryService
	{
		[Inject]
		private IAttributeController controller;

	#region IAttributeQueryService Members
		/// <inheritdoc />
		public int GetValue(string ownerId, string configId) => controller.GetValue(ownerId, configId);

		/// <inheritdoc />
		public int GetMaxValue(string ownerId, string configId) => controller.GetMaxValue(ownerId, configId);

		/// <inheritdoc />
		public int GetMinValue(string ownerId, string configId) => controller.GetMinValue(ownerId, configId);
	#endregion

	#region IAttributeValueService Members
		/// <inheritdoc />
		public ReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string configId) => controller.ObserveAttribute(ownerId, configId);
	#endregion
	}
}