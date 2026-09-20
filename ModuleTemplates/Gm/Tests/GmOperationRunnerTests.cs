using System;
using FluentAssertions;
using NUnit.Framework;
using Sumorin.DDDCore;

namespace Sumorin.Gm.Tests
{
	[TestFixture]
	public class GmOperationRunnerTests
	{
		private GmOperationRunner runner;

		[SetUp]
		public void Setup()
		{
			runner = new();
		}

		private static GmOperation OperationReturning(Func<object[], CommandResult> body) =>
			new("戰鬥/殺死目標", Array.Empty<GmParameter>(), body);

		[Test]
		public void Run_WithSuccessResult_ShowsExecuted()
		{
			var message = runner.Run(OperationReturning(_ => CommandResult.Ok()), Array.Empty<object>());

			message.Should().Be(GmOperationRunner.ExecutedMessage);
		}

		[Test]
		public void Run_WithFailedResult_ShowsFailureReason()
		{
			var message = runner.Run(OperationReturning(_ => CommandResult.Fail("場上沒有目標")), Array.Empty<object>());

			message.Should().Be("場上沒有目標");
		}

		[Test]
		public void Run_WhenDelegateThrows_ShowsExceptionMessageAndDoesNotThrow()
		{
			var operation = OperationReturning(_ => throw new InvalidOperationException("目標不存在"));
			string message = null;

			runner.Invoking(r => message = r.Run(operation, Array.Empty<object>())).Should().NotThrow();

			message.Should().Be("目標不存在");
		}
	}
}
