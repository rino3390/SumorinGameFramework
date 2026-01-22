using NUnit.Framework;

namespace Zenject
{
    /// <summary>
    /// Zenject 單元測試基類，提供 DiContainer 給子類別使用
    /// </summary>
    public abstract class ZenjectUnitTestFixture
    {
        private DiContainer container;

        /// <summary>
        /// 測試用的 DI 容器
        /// </summary>
        protected DiContainer Container => container;

        /// <summary>
        /// 每個測試前執行，初始化 DiContainer
        /// </summary>
        [SetUp]
        public virtual void Setup()
        {
            container = new DiContainer();
        }

        /// <summary>
        /// 每個測試後執行，清理 DiContainer
        /// </summary>
        [TearDown]
        public virtual void Teardown()
        {
            container = null;
        }
    }
}
