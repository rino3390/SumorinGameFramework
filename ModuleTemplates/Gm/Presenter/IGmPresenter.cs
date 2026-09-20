using System.Collections.Generic;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板的建置入口，由操作登記基底呼叫一次
	/// </summary>
	public interface IGmPresenter
	{
		/// <summary>
		///     依登記好的操作建置面板，過程中挑各參數的欄位建構器
		/// </summary>
		/// <remarks>
		///     參數型別找不到對應欄位時在此拋出例外。
		/// </remarks>
		/// <param name="operations">已登記的全部操作</param>
		void BuildPanel(IReadOnlyList<GmOperation> operations);
	}
}