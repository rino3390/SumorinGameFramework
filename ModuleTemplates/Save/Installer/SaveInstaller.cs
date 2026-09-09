using VContainer;
using VContainer.Unity;

namespace Sumorin.Save
{
	/// <summary>
	///     存檔系統的 Installer
	/// </summary>
	/// <remarks>
	///     <see cref="ISaveStorage" /> 與 <see cref="ISerializer" /> 註冊的是預設實作，安裝後不需另外註冊就能存讀檔。
	///     <see cref="ISaveStorage" /> 解析到的是包著 <see cref="LocalFileSaveStorage" /> 的 <see cref="EncryptedSaveStorage" />，落地內容不是明文。
	///     要換成雲端存檔、其他序列化方式或另一組通行碼時，不安裝本 Installer，改為自行照這幾行組裝。
	///     參與者的註冊見 <see cref="SaveBindingExtensions" />。
	/// </remarks>
	public class SaveInstaller: IInstaller
	{
		// ponytail: 通行碼隨框架發佈，用同一套框架的專案共用它，在意的專案自行組裝註冊換掉
		private const string EncryptionPassphrase = "Sumorin.Save.Encryption";

	#region IInstaller Members
		/// <inheritdoc />
		public void Install(IContainerBuilder builder)
		{
			builder.Register<ISerializer, NewtonsoftSaveSerializer>(Lifetime.Singleton);

			builder.Register<LocalFileSaveStorage>(Lifetime.Singleton);
			builder.Register<ISaveStorage>(
				resolver => new EncryptedSaveStorage(resolver.Resolve<LocalFileSaveStorage>(), EncryptionPassphrase),
				Lifetime.Singleton
			);

			builder.Register<ISaveController, SaveController>(Lifetime.Singleton);

			builder.Register<SaveCommandService>(Lifetime.Singleton);

			builder.Register<SaveQueryService>(Lifetime.Singleton).AsSelf().As<ISaveQueryService>();
		}
	#endregion
	}
}