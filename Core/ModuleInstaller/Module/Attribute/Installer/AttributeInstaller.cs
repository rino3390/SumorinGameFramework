using Zenject;

namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 屬性系統的 Zenject Installer
    /// </summary>
    public class AttributeInstaller : Installer<AttributeInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<IAttributeRepository>().To<AttributeRepository>().AsSingle();
            Container.Bind<IAttributeController>().To<AttributeController>().AsSingle();
        }
    }
}
