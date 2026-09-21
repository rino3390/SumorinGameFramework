using System;
using System.Collections.Generic;
using System.Reflection;
using Sumorin.DDDCore;
using VContainer;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 操作的登記入口，遊戲側繼承後在 <see cref="Define" /> 內逐條登記操作
	/// </summary>
	/// <remarks>
	///     子類別由遊戲側進入點傳給 <see cref="GmBootstrap.Launch{TOperations}" />，由它組裝整個 GM。
	///     登記的委派一律呼叫 CommandService，驗證留在 Controller，GM 不繞過任何規則。
	/// </remarks>
	public abstract class GmOperations
	{
		/// <summary>
		///     已登記的全部操作
		/// </summary>
		public IReadOnlyCollection<GmOperation> Operations => operations.Values;

		[Inject]
		private IGmPresenter presenter;

		private readonly Dictionary<string, GmOperation> operations = new();

		/// <summary>
		///     登記全部操作並建置面板，由啟動點呼叫一次
		/// </summary>
		public void Build()
		{
			Define();
			presenter.BuildPanel(new List<GmOperation>(operations.Values));
		}

		/// <summary>
		///     登記這款遊戲的 GM 操作
		/// </summary>
		protected abstract void Define();

		/// <summary>
		///     登記一條無參數、不回傳值的操作
		/// </summary>
		/// <param name="name">操作名稱，格式為 <c>分類/名稱</c></param>
		/// <param name="body">執行內容</param>
		protected void Add(string name, Action body) =>
			Register(
				name,
				body.Method,
				_ =>
				{
					body();
					return CommandResult.Ok();
				}
			);

		/// <inheritdoc cref="Add(string,System.Action)" />
		protected void Add<T1>(string name, Action<T1> body) =>
			Register(
				name,
				body.Method,
				arguments =>
				{
					body((T1)arguments[0]);
					return CommandResult.Ok();
				}
			);

		/// <inheritdoc cref="Add(string,System.Action)" />
		protected void Add<T1, T2>(string name, Action<T1, T2> body) =>
			Register(
				name,
				body.Method,
				arguments =>
				{
					body((T1)arguments[0], (T2)arguments[1]);
					return CommandResult.Ok();
				}
			);

		/// <inheritdoc cref="Add(string,System.Action)" />
		protected void Add<T1, T2, T3>(string name, Action<T1, T2, T3> body) =>
			Register(
				name,
				body.Method,
				arguments =>
				{
					body((T1)arguments[0], (T2)arguments[1], (T3)arguments[2]);
					return CommandResult.Ok();
				}
			);

		/// <inheritdoc cref="Add(string,System.Action)" />
		protected void Add<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> body) =>
			Register(
				name,
				body.Method,
				arguments =>
				{
					body((T1)arguments[0], (T2)arguments[1], (T3)arguments[2], (T4)arguments[3]);
					return CommandResult.Ok();
				}
			);

		/// <summary>
		///     登記一條無參數、回傳命令結果的操作
		/// </summary>
		/// <param name="name">操作名稱，格式為 <c>分類/名稱</c></param>
		/// <param name="body">執行內容</param>
		protected void Add(string name, Func<CommandResult> body) => Register(name, body.Method, _ => body());

		/// <inheritdoc cref="Add(string,System.Func{Sumorin.DDDCore.CommandResult})" />
		protected void Add<T1>(string name, Func<T1, CommandResult> body) => Register(name, body.Method, arguments => body((T1)arguments[0]));

		/// <inheritdoc cref="Add(string,System.Func{Sumorin.DDDCore.CommandResult})" />
		protected void Add<T1, T2>(string name, Func<T1, T2, CommandResult> body) =>
			Register(name, body.Method, arguments => body((T1)arguments[0], (T2)arguments[1]));

		/// <inheritdoc cref="Add(string,System.Func{Sumorin.DDDCore.CommandResult})" />
		protected void Add<T1, T2, T3>(string name, Func<T1, T2, T3, CommandResult> body) =>
			Register(name, body.Method, arguments => body((T1)arguments[0], (T2)arguments[1], (T3)arguments[2]));

		/// <inheritdoc cref="Add(string,System.Func{Sumorin.DDDCore.CommandResult})" />
		protected void Add<T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, CommandResult> body) =>
			Register(name, body.Method, arguments => body((T1)arguments[0], (T2)arguments[1], (T3)arguments[2], (T4)arguments[3]));

		// 參數名稱取自委派的 MethodInfo。編譯器最佳化或混淆後可能拿不到名稱，退回型別名讓面板照樣建得起來
		private static IReadOnlyList<GmParameter> ParametersOf(MethodInfo method)
		{
			var source = method.GetParameters();
			var parameters = new GmParameter[source.Length];

			for(var index = 0; index < source.Length; index++)
			{
				var name = string.IsNullOrEmpty(source[index].Name) ? source[index].ParameterType.Name : source[index].Name;
				parameters[index] = new(name, source[index].ParameterType);
			}

			return parameters;
		}

		private void Register(string name, MethodInfo method, Func<object[], CommandResult> body)
		{
			var operation = new GmOperation(name, ParametersOf(method), body);

			if(!operations.TryAdd(operation.FullName, operation))
			{
				throw new InvalidOperationException($"GM 操作名稱重複：{operation.FullName}");
			}
		}
	}
}