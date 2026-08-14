using UniRx;
using Zenject;

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
		public int GetValue(string ownerId, string attributeName) => controller.GetValue(ownerId, attributeName);
	#endregion

	#region IAttributeValueService Members
		/// <inheritdoc />
		public IReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string attributeName)
			=> controller.ObserveAttribute(ownerId, attributeName);
	#endregion
	}
}
