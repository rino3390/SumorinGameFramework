using Zenject;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     <see cref="ISaveStorage" /> 與 <see cref="ISerializer" /> 綁的是預設實作，安裝後不需另外綁定就能存讀檔。
	///     要換成雲端存檔或其他序列化方式時，在本 Installer 之後以 <c>Container.Rebind</c> 換掉該介面的綁定，另一個不受影響。
	///     用 <c>Bind</c> 會與預設綁定並存，解析時因多重綁定而失敗。
	///     參與者的註冊見 <see cref="SaveBindingExtensions" />。
	/// </remarks>
	public class SaveInstaller: Installer<SaveInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind<ISerializer>().To<NewtonsoftSaveSerializer>().AsSingle();
			Container.Bind<ISaveStorage>().To<LocalFileSaveStorage>().AsSingle();

			Container.Bind<ISaveController>().To<SaveController>().AsSingle();

			Container.Bind<SaveCommandService>().AsSingle();

			Container.Bind<SaveQueryService>().AsSingle();
			Container.Bind<ISaveQueryService>().To<SaveQueryService>().FromResolve();
		}
	}
}