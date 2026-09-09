using System.Collections.Generic;
using System.Linq;
using Sumorin.SumorinUtility;
using VContainer;
using VContainer.Unity;

namespace Sumorin.DDDCore
{
	/// <summary>
	/// DDDCore 的 Installer，負責註冊 EventBus 與 ConfigManager
	/// </summary>
	/// <remarks>
	///     <see cref="ConfigManager" /> 於首次解析時收集容器內所有 <see cref="ConfigSource" />，合併成單一查找字典。
	///     註冊全部發生在容器建置前，本 Installer 與模組 Installer 的先後順序不影響結果。
	/// </remarks>
	public class DDDCoreInstaller: IInstaller
	{
	#region IInstaller Members
		/// <inheritdoc />
		public void Install(IContainerBuilder builder)
		{
			builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus, IPublisher, ISubscriber>();

			builder.Register(
				resolver => new ConfigManager(resolver.Resolve<IEnumerable<ConfigSource>>().SelectMany(source => source.Configs)),
				Lifetime.Singleton
			);
		}
	#endregion
	}
}