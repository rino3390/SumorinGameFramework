namespace Sumorin.GameFramework.DDDCore
{
    /// <summary>
    /// 事件匯流排介面，組合事件發布與訂閱功能
    /// </summary>
    public interface IEventBus : IPublisher, ISubscriber
    {
    }
}
