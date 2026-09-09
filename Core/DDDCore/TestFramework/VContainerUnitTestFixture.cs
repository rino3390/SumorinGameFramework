using NUnit.Framework;
using VContainer;

namespace Sumorin.TestFramework
{
	/// <summary>
	///     VContainer 單元測試基類，每個測試給一個新的 ContainerBuilder，首次取用 Container 時才建置
	/// </summary>
	public abstract class VContainerUnitTestFixture
	{
		private IObjectResolver container;

		/// <summary>
		///     測試用的註冊入口，建置前都可以註冊
		/// </summary>
		protected ContainerBuilder Builder { get; private set; }

		/// <summary>
		///     建置後的容器。首次取用時建置，之後對 <see cref="Builder" /> 的註冊不會生效
		/// </summary>
		protected IObjectResolver Container => container ??= Builder.Build();

		/// <summary>
		///     每個測試前執行，準備新的 ContainerBuilder
		/// </summary>
		[SetUp]
		public virtual void Setup()
		{
			Builder = new();
			container = null;
		}

		/// <summary>
		///     每個測試後執行，釋放容器
		/// </summary>
		[TearDown]
		public virtual void Teardown()
		{
			container?.Dispose();
			container = null;
			Builder = null;
		}
	}
}