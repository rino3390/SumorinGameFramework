using Rino.GameFramework.Core.AttributeSystem.Controller;
using Rino.GameFramework.Core.AttributeSystem.Repository;
using Zenject;

namespace Rino.GameFramework.Core.AttributeSystem.Installer
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
