using System;
using Sumorin.SumorinUtility;
using VContainer;
using VContainer.Unity;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性系統的 Installer
	/// </summary>
	/// <remarks>
	///     建立時傳入 <see cref="AttributeDataSet" />，本 Installer 把其中的配置註冊成一個 <see cref="ConfigSource" />。
	///     <see cref="ConfigManager" /> 由 <c>DDDCoreInstaller</c> 註冊，解析時收集所有來源，遊戲側不需自行組裝。
	///     查找鍵取自 <see cref="AttributeData" /> 自己的 Id，Installer 不另外指定。
	/// </remarks>
	public class AttributeInstaller: IInstaller
	{
		private readonly AttributeDataSet dataSet;

		public AttributeInstaller(AttributeDataSet dataSet)
		{
			if(dataSet == null)
			{
				throw new ArgumentNullException(nameof(dataSet), "安裝屬性系統需要指定 AttributeDataSet");
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

			builder.Register<IAttributeRepository, AttributeRepository>(Lifetime.Singleton);
			builder.Register<IAttributeController, AttributeController>(Lifetime.Singleton);

			builder.Register<AttributeCommandService>(Lifetime.Singleton);

			builder.Register<AttributeQueryService>(Lifetime.Singleton).AsSelf().As<IAttributeValueService, IAttributeQueryService>();
		}
	#endregion
	}
}