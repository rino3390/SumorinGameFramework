using System;
using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.Gm
{
	/// <summary>
	///     一條 GM 操作的規格，含分類、名稱、參數與執行內容
	/// </summary>
	public sealed class GmOperation
	{
		/// <summary>
		///     名稱未帶分類時歸屬的分頁
		/// </summary>
		public const string UncategorizedCategory = "未分類";

		private const char CategorySeparator = '/';

		/// <summary>
		///     由分類與名稱組成的完整名稱，格式為 <c>分類/名稱</c>，未分類時只有名稱，也是操作的唯一識別
		/// </summary>
		public string FullName => Category == UncategorizedCategory ? Name : $"{Category}{CategorySeparator}{Name}";

		/// <summary>
		///     分頁名稱
		/// </summary>
		public string Category { get; }

		/// <summary>
		///     操作在分頁內顯示的名稱
		/// </summary>
		public string Name { get; }

		/// <summary>
		///     參數規格，順序與委派的參數順序相同
		/// </summary>
		public IReadOnlyList<GmParameter> Parameters { get; }

		private readonly Func<object[], CommandResult> body;

		/// <summary>
		///     建立操作規格
		/// </summary>
		/// <param name="fullName">完整名稱，格式為 <c>分類/名稱</c>，不含分類時歸「未分類」</param>
		/// <param name="parameters">參數規格</param>
		/// <param name="body">執行內容，參數依 <paramref name="parameters" /> 順序傳入</param>
		public GmOperation(string fullName, IReadOnlyList<GmParameter> parameters, Func<object[], CommandResult> body)
		{
			if(string.IsNullOrWhiteSpace(fullName))
			{
				throw new ArgumentException("GM 操作名稱不可為空", nameof(fullName));
			}

			Parameters = parameters ?? Array.Empty<GmParameter>();
			this.body = body ?? throw new ArgumentNullException(nameof(body));

			var separatorIndex = fullName.IndexOf(CategorySeparator);
			Category = separatorIndex < 0 ? UncategorizedCategory : fullName.Substring(0, separatorIndex).Trim();
			Name = separatorIndex < 0 ? fullName.Trim() : fullName.Substring(separatorIndex + 1).Trim();
		}

		/// <summary>
		///     執行操作
		/// </summary>
		/// <param name="arguments">各參數的當前值，順序同 <see cref="Parameters" /></param>
		/// <returns>命令結果，無回傳值的操作一律為成功</returns>
		public CommandResult Invoke(object[] arguments) => body(arguments);
	}

	/// <summary>
	///     一個操作參數的規格，決定面板上要生成哪種輸入欄位與欄位標籤
	/// </summary>
	public readonly struct GmParameter
	{
		/// <summary>
		///     參數名稱，即欄位標籤
		/// </summary>
		public string Name { get; }

		/// <summary>
		///     參數型別，決定挑哪個欄位建構器
		/// </summary>
		public Type Type { get; }

		/// <summary>
		///     建立參數規格
		/// </summary>
		/// <param name="name">參數名稱</param>
		/// <param name="type">參數型別</param>
		public GmParameter(string name, Type type)
		{
			Name = name;
			Type = type;
		}
	}
}