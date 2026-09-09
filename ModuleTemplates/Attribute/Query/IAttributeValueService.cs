using R3;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Domain 的值面，供 View 訂閱
	/// </summary>
	public interface IAttributeValueService
	{
		/// <inheritdoc cref="IAttributeController.ObserveAttribute" />
		ReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string configId);
	}
}