using Zenject;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     <see cref="ISaveStorage" /> 與 <see cref="ISerializer" /> 由專案層實作並綁定，本 Installer 不碰它們。
	///     參與者的註冊見 <see cref="SaveBindingExtensions" />。
	/// </remarks>
	public class SaveInstaller: Installer<SaveInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind<ISaveController>().To<SaveController>().AsSingle();

			Container.Bind<SaveCommandService>().AsSingle();

			Container.Bind<SaveQueryService>().AsSingle();
			Container.Bind<ISaveQueryService>().To<SaveQueryService>().FromResolve();
		}
	}
}