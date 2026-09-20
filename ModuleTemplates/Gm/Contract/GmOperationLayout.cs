using System;
using System.Collections.Generic;

namespace Sumorin.Gm
{
	/// <summary>
	///     一條操作列的生成指示，操作規格加上各參數挑好的欄位建構器與執行方式
	/// </summary>
	public readonly struct GmOperationLayout
	{
		/// <summary>
		///     這一列對應的操作
		/// </summary>
		public GmOperation Operation { get; }

		/// <summary>
		///     各參數的欄位建構器，順序同操作的參數順序
		/// </summary>
		public IReadOnlyList<GmFieldBuilder> FieldBuilders { get; }

		/// <summary>
		///     按下執行鈕時要做的事，收各欄位的當前值，回傳要顯示的結果訊息
		/// </summary>
		public Func<object[], string> Execute { get; }

		/// <summary>
		///     建立操作列的生成指示
		/// </summary>
		/// <param name="operation">操作規格</param>
		/// <param name="fieldBuilders">各參數的欄位建構器</param>
		/// <param name="execute">執行方式，回傳要顯示的結果訊息</param>
		public GmOperationLayout(GmOperation operation, IReadOnlyList<GmFieldBuilder> fieldBuilders, Func<object[], string> execute)
		{
			Operation = operation;
			FieldBuilders = fieldBuilders;
			Execute = execute;
		}
	}
}