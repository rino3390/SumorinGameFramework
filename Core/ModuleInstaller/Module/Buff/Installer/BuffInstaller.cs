using Zenject;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff 系統的 Zenject Installer
    /// </summary>
    public class BuffInstaller : Installer<BuffInstaller>
    {
        public override void InstallBindings()
        {
            // Repository
            Container.Bind<IBuffRepository>().To<BuffRepository>().AsSingle();

            // Controller
            Container.BindInterfacesAndSelfTo<BuffController>().AsSingle();
            Container.Bind<BuffEffectController>().AsSingle();

            // Flow
            Container.BindInterfacesTo<BuffAppliedFlow>().AsSingle();
            Container.BindInterfacesTo<BuffRemovedFlow>().AsSingle();
            Container.BindInterfacesTo<BuffStackChangedFlow>().AsSingle();
        }
    }
}
