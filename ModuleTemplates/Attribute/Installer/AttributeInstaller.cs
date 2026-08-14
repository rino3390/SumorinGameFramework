using Zenject;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     配置由組裝根建立 <see cref="DDDCore.ConfigManager" /> 時一併註冊，本 Installer 不碰它。
	///     配置內容來自 <see cref="AttributeSettingData" />，其中的 <see cref="AttributeConfig" /> 已實作 <see cref="IAttributeConfig" />。
	/// </remarks>
	public class AttributeInstaller: Installer<AttributeInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind<IAttributeRepository>().To<AttributeRepository>().AsSingle();
			Container.Bind<IAttributeController>().To<AttributeController>().AsSingle();

			Container.Bind<AttributeCommandService>().AsSingle();

			Container.Bind<AttributeQueryService>().AsSingle();
			Container.Bind<IAttributeValueService>().To<AttributeQueryService>().FromResolve();
			Container.Bind<IAttributeQueryService>().To<AttributeQueryService>().FromResolve();
		}
	}
}