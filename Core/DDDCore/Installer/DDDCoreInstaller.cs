using System.Linq;
using Sumorin.SumorinUtility;
using Zenject;

namespace Sumorin.DDDCore
{
	/// <summary>
	/// DDDCore Zenject Installer，負責註冊 EventBus 與 ConfigManager
	/// </summary>
	/// <remarks>
	///     <see cref="ConfigManager" /> 於首次解析時收集容器內所有 <see cref="ConfigSource" />，合併成單一查找字典。
	///     因此本 Installer 可以先安裝，模組 Installer 之後再綁定各自的來源。
	///     反過來說它不可改為 <c>NonLazy</c>，那會在模組來源綁定前就把字典定型。
	/// </remarks>
	public class DDDCoreInstaller: Installer<DDDCoreInstaller>
	{
		public override void InstallBindings()
		{
			Container.Bind(typeof(IEventBus), typeof(IPublisher), typeof(ISubscriber)).To<EventBus>().AsSingle();

			Container.Bind<ConfigManager>()
					 .FromMethod(context => new(context.Container.ResolveAll<ConfigSource>().SelectMany(source => source.Configs)))
					 .AsSingle();
		}
	}
}