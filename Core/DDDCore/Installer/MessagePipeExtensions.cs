#if ZENJECT
using MessagePipe;
using Zenject;

namespace Rino.GameFramework.DDDCore
{
    /// <summary>
    /// MessagePipe 擴展方法，簡化 Event 註冊
    /// </summary>
    public static class MessagePipeExtensions
    {
        /// <summary>
        /// 註冊 Message Broker，自動從 Container resolve MessagePipeOptions
        /// </summary>
        public static DiContainer BindMessageBroker<TMessage>(this DiContainer container)
        {
            var options = container.Resolve<MessagePipeOptions>();
            return container.BindMessageBroker<TMessage>(options);
        }
    }
}
#endif
