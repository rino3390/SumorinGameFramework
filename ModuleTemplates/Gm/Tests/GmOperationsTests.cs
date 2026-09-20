using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sumorin.DDDCore;
using Sumorin.TestFramework;
using VContainer;

namespace Sumorin.Gm.Tests
{
	[TestFixture]
	public class GmOperationsTests: VContainerUnitTestFixture
	{
		private IGmPresenter presenter;

		[SetUp]
		public override void Setup()
		{
			base.Setup();
			presenter = Substitute.For<IGmPresenter>();
		}

		private TestGmOperations CreateOperations(Action<TestGmOperations> define)
		{
			Builder.RegisterInstance(presenter);
			Builder.Register<TestGmOperations>(Lifetime.Singleton);

			var operations = Container.Resolve<TestGmOperations>();
			operations.OnDefine = define;
			return operations;
		}

		[TestCase("戰鬥/殺死目標", "戰鬥", "殺死目標", TestName = "帶分類的名稱拆成分類與名稱")]
		[TestCase("重置全部", GmOperation.UncategorizedCategory, "重置全部", TestName = "沒有分類的名稱歸未分類")]
		public void Build_ParsesOperationName(string fullName, string category, string name)
		{
			var operations = CreateOperations(o => o.Register(fullName, () => CommandResult.Ok()));

			operations.Build();

			operations.Operations.Should().BeEquivalentTo(new[] { new { FullName = fullName, Category = category, Name = name } });
		}

		[Test]
		public void Build_WithDuplicatedOperationName_ThrowsWithThatName()
		{
			var operations = CreateOperations(o =>
				{
					o.Register("戰鬥/殺死目標", () => CommandResult.Ok());
					o.Register("戰鬥/殺死目標", () => CommandResult.Ok());
				}
			);

			Action act = () => operations.Build();

			act.Should().ThrowExactly<InvalidOperationException>().WithMessage("*戰鬥/殺死目標*");
		}

		[Test]
		public void Build_WithParameters_TakesNameAndTypeFromDelegate()
		{
			var operations = CreateOperations(o => o.Register<string, int>("戰鬥/給錢", (targetId, amount) => CommandResult.Ok()));

			operations.Build();

			operations.Operations.Single()
					  .Parameters.Should()
					  .BeEquivalentTo(
						  new[]
						  {
							  new { Name = "targetId", Type = typeof(string) },
							  new { Name = "amount", Type = typeof(int) }
						  },
						  options => options.WithStrictOrdering()
					  );
		}

		[Test]
		public void Build_WithEveryArityAndResultKind_RegistersAllOperations()
		{
			var operations = CreateOperations(o =>
				{
					o.Register("無/命令0", () => CommandResult.Ok());
					o.Register<int>("無/命令1", a => CommandResult.Ok());
					o.Register<int, int>("無/命令2", (a, b) => CommandResult.Ok());
					o.Register<int, int, int>("無/命令3", (a, b, c) => CommandResult.Ok());
					o.Register<int, int, int, int>("無/命令4", (a, b, c, d) => CommandResult.Ok());
					o.Register("無/表現0", () => { });
					o.Register<int>("無/表現1", a => { });
					o.Register<int, int>("無/表現2", (a, b) => { });
					o.Register<int, int, int>("無/表現3", (a, b, c) => { });
					o.Register<int, int, int, int>("無/表現4", (a, b, c, d) => { });
				}
			);

			operations.Build();

			operations.Operations.Select(operation => new { operation.Name, Count = operation.Parameters.Count })
					  .Should()
					  .BeEquivalentTo(
						  new[]
						  {
							  new { Name = "命令0", Count = 0 }, new { Name = "命令1", Count = 1 }, new { Name = "命令2", Count = 2 },
							  new { Name = "命令3", Count = 3 }, new { Name = "命令4", Count = 4 },
							  new { Name = "表現0", Count = 0 }, new { Name = "表現1", Count = 1 }, new { Name = "表現2", Count = 2 },
							  new { Name = "表現3", Count = 3 }, new { Name = "表現4", Count = 4 }
						  }
					  );
		}

		[Test]
		public void Build_AfterDefine_HandsRegisteredOperationsToPresenter()
		{
			IReadOnlyList<GmOperation> built = null;
			presenter.When(p => p.BuildPanel(Arg.Any<IReadOnlyList<GmOperation>>())).Do(call => built = call.Arg<IReadOnlyList<GmOperation>>());
			var operations = CreateOperations(o =>
				{
					o.Register("戰鬥/殺死目標", () => CommandResult.Ok());
					o.Register("經濟/給錢", () => CommandResult.Ok());
				}
			);

			operations.Build();

			presenter.Received(1).BuildPanel(Arg.Any<IReadOnlyList<GmOperation>>());
			built.Select(operation => operation.FullName).Should().BeEquivalentTo("戰鬥/殺死目標", "經濟/給錢");
		}

		[Test]
		public void Build_WithVoidOperation_InvokesDelegateAndReportsSuccess()
		{
			var invoked = false;
			var operations = CreateOperations(o => o.Register<float>("表現/設定倍速", speed => invoked = true));
			operations.Build();

			var result = operations.Operations.Single().Invoke(new object[] { 2f });

			invoked.Should().BeTrue();
			result.IsSuccess.Should().BeTrue();
		}

		[Test]
		public void Build_WithArguments_PassesThemToTheRegisteredDelegateInOrder()
		{
			var received = Array.Empty<int>();
			var operations = CreateOperations(o => o.Register<int, int, int, int>(
												  "戰鬥/四參數",
												  (a, b, c, d) =>
												  {
													  received = new[] { a, b, c, d };
													  return CommandResult.Ok();
												  }
											  )
			);
			operations.Build();

			operations.Operations.Single().Invoke(new object[] { 1, 2, 3, 4 });

			received.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 }, options => options.WithStrictOrdering());
		}

	#region Nested Types
		private sealed class TestGmOperations: GmOperations
		{
			public Action<TestGmOperations> OnDefine;

			public void Register(string name, Action body)                                              => Add(name, body);
			public void Register<T1>(string name, Action<T1> body)                                      => Add(name, body);
			public void Register<T1, T2>(string name, Action<T1, T2> body)                              => Add(name, body);
			public void Register<T1, T2, T3>(string name, Action<T1, T2, T3> body)                      => Add(name, body);
			public void Register<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> body)              => Add(name, body);
			public void Register(string name, Func<CommandResult> body)                                 => Add(name, body);
			public void Register<T1>(string name, Func<T1, CommandResult> body)                         => Add(name, body);
			public void Register<T1, T2>(string name, Func<T1, T2, CommandResult> body)                 => Add(name, body);
			public void Register<T1, T2, T3>(string name, Func<T1, T2, T3, CommandResult> body)         => Add(name, body);
			public void Register<T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, CommandResult> body) => Add(name, body);

			protected override void Define() => OnDefine?.Invoke(this);
		}
	#endregion
	}
}