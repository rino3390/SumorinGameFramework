using Zenject;

namespace Sumorin.GameFramework.BuffSystem
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
        }
    }
}
