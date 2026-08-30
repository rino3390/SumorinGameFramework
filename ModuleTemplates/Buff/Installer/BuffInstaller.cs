using System;
using Sumorin.DDDCore;
using Zenject;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     安裝時傳入 <see cref="BuffDataSet" />，本 Installer 把其中的配置綁成一個 <see cref="ConfigSource" />。
	///     <see cref="ConfigManager" /> 由 <c>DDDCoreInstaller</c> 綁定，解析時收集所有來源，遊戲側不需自行組裝。
	///     <see cref="BuffData" /> 已實作 <see cref="IBuffConfig" />，直接作為配置內容使用。
	///     Buff 相依 Attribute，組裝根須先安裝 <c>AttributeInstaller</c>。
	/// </remarks>
	public class BuffInstaller: Installer<BuffDataSet, BuffInstaller>
	{
		private readonly BuffDataSet dataSet;

		public BuffInstaller(BuffDataSet dataSet)
		{
			this.dataSet = dataSet ? dataSet : throw new ArgumentNullException(nameof(dataSet), "安裝 Buff 系統需要指定 BuffDataSet");
		}

		public override void InstallBindings()
		{
			Container.Bind<ConfigSource>().FromInstance(new(dataSet.Datas)).AsCached();

			Container.Bind<IBuffRepository>().To<BuffRepository>().AsSingle();
			Container.Bind<IBuffController>().To<BuffController>().AsSingle();

			Container.Bind<BuffCommandService>().AsSingle();

			Container.Bind<BuffQueryService>().AsSingle();
			Container.Bind<IBuffValueService>().To<BuffQueryService>().FromResolve();
			Container.Bind<IBuffQueryService>().To<BuffQueryService>().FromResolve();
		}
	}
}