namespace Sumorin.Presentation
{
	/// <summary>
	///     清理契約，約定 View 具備 Unbind，讓 IViewProvider 在回收前把 View 清乾淨
	/// </summary>
	public interface IBindableView
	{
		/// <summary>
		///     解除綁定，清除訂閱並重置視覺
		/// </summary>
		void Unbind();
	}
}
