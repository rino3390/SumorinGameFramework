using Zenject;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔系統的 Zenject Installer
	/// </summary>
	/// <remarks>
	///     <see cref="ISaveStorage" /> 與 <see cref="ISerializer" /> 綁的是預設實作，安裝後不需另外綁定就能存讀檔。
	///     <see cref="ISaveStorage" /> 另外裝飾了一層 <see cref="EncryptedSaveStorage" />，落地內容不是明文。
	///     要換成雲端存檔或其他序列化方式時，在本 Installer 之後以 <c>Container.Rebind</c> 換掉該介面的綁定，另一個不受影響。
	///     裝飾層不隨 <c>Rebind</c> 消失，換掉存取媒介後加密照樣套在新的實作上。
	///     只換通行碼是例外，不能在本 Installer 之後再 <c>Decorate</c> 一次，那會疊成兩層加密。
	///     要換通行碼請不要安裝本 Installer，改為自行組裝這幾行綁定。
	///     用 <c>Bind</c> 會與預設綁定並存，解析時因多重綁定而失敗。
	///     參與者的註冊見 <see cref="SaveBindingExtensions" />。
	/// </remarks>
	public class SaveInstaller: Installer<SaveInstaller>
	{
		// ponytail: 通行碼隨框架發佈，用同一套框架的專案共用它，在意的專案自行組裝綁定換掉
		private const string EncryptionPassphrase = "Sumorin.Save.Encryption";

		public override void InstallBindings()
		{
			Container.Bind<ISerializer>().To<NewtonsoftSaveSerializer>().AsSingle();

			Container.Bind<ISaveStorage>().To<LocalFileSaveStorage>().AsSingle();
			Container.Decorate<ISaveStorage>().With<EncryptedSaveStorage>().WithArguments(EncryptionPassphrase);

			Container.Bind<ISaveController>().To<SaveController>().AsSingle();

			Container.Bind<SaveCommandService>().AsSingle();

			Container.Bind<SaveQueryService>().AsSingle();
			Container.Bind<ISaveQueryService>().To<SaveQueryService>().FromResolve();
		}
	}
}