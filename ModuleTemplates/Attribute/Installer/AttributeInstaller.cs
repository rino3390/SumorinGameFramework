using System;
using Sumorin.DDDCore;
using Zenject;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     安裝時傳入 <see cref="AttributeDataSet" />，本 Installer 把其中的配置綁成一個 <see cref="ConfigSource" />。
	///     <see cref="ConfigManager" /> 由 <c>DDDCoreInstaller</c> 綁定，解析時收集所有來源，遊戲側不需自行組裝。
	///     查找鍵取自 <see cref="AttributeData" /> 自己的 Id，Installer 不另外指定。
	/// </remarks>
	public class AttributeInstaller: Installer<AttributeDataSet, AttributeInstaller>
	{
		private readonly AttributeDataSet dataSet;

		public AttributeInstaller(AttributeDataSet dataSet)
		{
			this.dataSet = dataSet ? dataSet : throw new ArgumentNullException(nameof(dataSet), "安裝屬性系統需要指定 AttributeDataSet");
		}

		public override void InstallBindings()
		{
			Container.Bind<ConfigSource>().FromInstance(new(dataSet.Datas)).AsCached();

			Container.Bind<IAttributeRepository>().To<AttributeRepository>().AsSingle();
			Container.Bind<IAttributeController>().To<AttributeController>().AsSingle();

			Container.Bind<AttributeCommandService>().AsSingle();

			Container.Bind<AttributeQueryService>().AsSingle();
			Container.Bind<IAttributeValueService>().To<AttributeQueryService>().FromResolve();
			Container.Bind<IAttributeQueryService>().To<AttributeQueryService>().FromResolve();
		}
	}
}