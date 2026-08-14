namespace Sumorin.DDDCore
{
	/// <summary>
	///     命令的統一回傳型別，供 Flow 判斷後續分支
	/// </summary>
	/// <remarks>
	///     預設值為失敗，未經 <see cref="Ok()" /> 建立的結果不會被誤判為成功。
	/// </remarks>
	public readonly struct CommandResult
	{
		/// <summary>
		///     命令是否成功
		/// </summary>
		public bool IsSuccess { get; }

		/// <summary>
		///     成功時的酬載（如新建立的 Entity Id），無酬載時為 null
		/// </summary>
		public string Value { get; }

		/// <summary>
		///     失敗原因，成功時為 null
		/// </summary>
		public string FailureReason { get; }

		private CommandResult(bool isSuccess, string value, string failureReason)
		{
			IsSuccess = isSuccess;
			Value = value;
			FailureReason = failureReason;
		}

		/// <summary>
		///     建立成功結果
		/// </summary>
		public static CommandResult Ok() => new(true, null, null);

		/// <summary>
		///     建立帶酬載的成功結果
		/// </summary>
		/// <param name="value">酬載內容，通常為新建立的 Entity Id</param>
		public static CommandResult Ok(string value) => new(true, value, null);

		/// <summary>
		///     建立失敗結果
		/// </summary>
		/// <param name="reason">失敗原因</param>
		public static CommandResult Fail(string reason) => new(false, null, reason);
	}
}
