using System;
using Sumorin.SumorinUtility;
using VContainer;
using VContainer.Unity;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff 系統的 Installer
	/// </summary>
	/// <remarks>
	///     建立時傳入 <see cref="BuffDataSet" />，本 Installer 把其中的配置註冊成一個 <see cref="ConfigSource" />。
	///     <see cref="ConfigManager" /> 由 <c>DDDCoreInstaller</c> 註冊，解析時收集所有來源，遊戲側不需自行組裝。
	///     <see cref="BuffData" /> 已實作 <see cref="IBuffConfig" />，直接作為配置內容使用。
	///     Buff 相依 Attribute，組裝根須一併安裝 <c>AttributeInstaller</c>。
	/// </remarks>
	public class BuffInstaller: IInstaller
	{
		private readonly BuffDataSet dataSet;

		public BuffInstaller(BuffDataSet dataSet)
		{
			if(dataSet == null)
			{
				throw new ArgumentNullException(nameof(dataSet), "安裝 Buff 系統需要指定 BuffDataSet");
			}

			this.dataSet = dataSet;
		}

	#region IInstaller Members
		/// <inheritdoc />
		public void Install(IContainerBuilder builder)
		{
			// 不用 RegisterInstance：它是 Singleton，VContainer 把同型別的 Singleton 重複註冊視為衝突，多個模組各註冊一個來源就會炸。
			// Scoped 的重複註冊會被收成集合，ConfigManager 才解析得到全部來源
			builder.Register(_ => new ConfigSource(dataSet.Datas), Lifetime.Scoped);

			builder.Register<IBuffRepository, BuffRepository>(Lifetime.Singleton);
			builder.Register<IBuffController, BuffController>(Lifetime.Singleton);

			builder.Register<BuffCommandService>(Lifetime.Singleton);

			builder.Register<BuffQueryService>(Lifetime.Singleton).AsSelf().As<IBuffValueService, IBuffQueryService>();
		}
	#endregion
	}
}