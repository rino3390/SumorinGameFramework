using Zenject;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     配置由組裝根建立 <see cref="DDDCore.ConfigManager" /> 時一併註冊，本 Installer 不碰它。
	///     配置內容來自 <see cref="BuffDataSet" />，其中的 <see cref="BuffData" /> 已實作 <see cref="IBuffConfig" />。
	///     Buff 相依 Attribute，組裝根須先安裝 <c>AttributeInstaller</c>。
	/// </remarks>
	public class BuffInstaller: Installer<BuffInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind<IBuffRepository>().To<BuffRepository>().AsSingle();
			Container.Bind<IBuffController>().To<BuffController>().AsSingle();

			Container.Bind<BuffCommandService>().AsSingle();

			Container.Bind<BuffQueryService>().AsSingle();
			Container.Bind<IBuffValueService>().To<BuffQueryService>().FromResolve();
			Container.Bind<IBuffQueryService>().To<BuffQueryService>().FromResolve();
		}
	}
}