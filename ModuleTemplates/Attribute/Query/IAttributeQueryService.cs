namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Domain 的查詢面，供 Presenter 取用
	/// </summary>
	public interface IAttributeQueryService
	{
		/// <inheritdoc cref="IAttributeController.GetValue" />
		int GetValue(string ownerId, string configId);

		/// <inheritdoc cref="IAttributeController.GetMaxValue" />
		int GetMaxValue(string ownerId, string configId);

		/// <inheritdoc cref="IAttributeController.GetMinValue" />
		int GetMinValue(string ownerId, string configId);
	}
}