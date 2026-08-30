namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Domain 的查詢面，供 Presenter 取用
	/// </summary>
	public interface IAttributeQueryService
	{
		/// <inheritdoc cref="IAttributeController.GetValue" />
		int GetValue(string ownerId, string configId);
	}
}