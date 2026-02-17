using Zenject;

namespace Sumorin.GameFramework.DDDCore
{
    /// <summary>
    /// DDDCore Zenject Installer，負責註冊 EventBus 相關服務
    /// </summary>
    public class DDDCoreInstaller : Installer<DDDCoreInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IEventBus), typeof(IPublisher), typeof(ISubscriber))
                .To<EventBus>().AsSingle();
        }
    }
}
