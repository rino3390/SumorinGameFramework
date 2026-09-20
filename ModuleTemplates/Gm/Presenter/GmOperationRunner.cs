using System;

namespace Sumorin.Gm
{
	/// <summary>
	///     執行登記的操作，把命令結果與例外換成要顯示在結果訊息列的文字
	/// </summary>
	/// <remarks>
	///     刻意做成純 C#，成功、失敗與例外三種結果才寫得了 EditMode 測試。
	/// </remarks>
	public class GmOperationRunner
	{
		/// <summary>
		///     操作成功與無回傳值時顯示的結果訊息
		/// </summary>
		public const string ExecutedMessage = "已執行";

		/// <summary>
		///     執行一條操作
		/// </summary>
		/// <param name="operation">要執行的操作</param>
		/// <param name="arguments">各參數的當前值，順序同操作的參數順序</param>
		/// <returns>要顯示的結果訊息</returns>
		public string Run(GmOperation operation, object[] arguments)
		{
			try
			{
				var result = operation.Invoke(arguments);
				return result.IsSuccess ? ExecutedMessage : result.FailureReason;
			}
			catch(Exception exception)
			{
				// GM 操作炸掉不該把遊戲一起帶走，訊息丟到結果列讓開發者當場看到
				return exception.Message;
			}
		}
	}
}
