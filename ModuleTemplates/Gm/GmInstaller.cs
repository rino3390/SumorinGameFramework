using System;
using System.Collections.Generic;
using Sumorin.SumorinUtility;
using VContainer;
using VContainer.Unity;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 的組裝，掛上遊戲側的登記子類別與自訂欄位
	/// </summary>
	/// <remarks>
	///     由 <see cref="GmBootstrap" /> 在執行期裝進 GM 自己的 child LifetimeScope，遊戲側的組裝根不引用 GM。
	/// </remarks>
	/// <typeparam name="TOperations">遊戲側的操作登記子類別</typeparam>
	public class GmInstaller<TOperations>: IInstaller where TOperations: GmOperations
	{
		private readonly IReadOnlyList<Type> fieldRegistrationTypes;

		/// <summary>
		///     建立 GM 組裝
		/// </summary>
		/// <remarks>
		///     收型別而非實例，欄位登記才能由容器建立。
		///     候選清單來源 <c>I{Domain}ValueService</c> 註冊在父 scope，登記類別注入它即可取得。
		/// </remarks>
		/// <param name="fieldRegistrationTypes">遊戲側 <see cref="IGmFieldRegistration" /> 實作的型別，沒有自訂型別時可省略</param>
		public GmInstaller(IReadOnlyList<Type> fieldRegistrationTypes = null)
		{
			this.fieldRegistrationTypes = fieldRegistrationTypes ?? Array.Empty<Type>();
		}

	#region IInstaller Members
		/// <inheritdoc />
		public void Install(IContainerBuilder builder)
		{
			// 不指定父物件的話面板會建在場景根層。遊戲的 scope 跨場景時 GM scope 跟著活下來、面板卻被換場景帶走，
			// 啟動點看到 scope 還在就不重建，面板從此消失。掛在 GM scope 底下才會同生同死
			builder.RegisterComponentOnNewGameObject<GmPanelView>(Lifetime.Singleton, "GM Panel")
				   .UnderTransform(resolver => (resolver.ApplicationOrigin as LifetimeScope)?.transform);

			foreach(var type in fieldRegistrationTypes)
			{
				builder.Register(typeof(IGmFieldRegistration), type, Lifetime.Singleton);
			}

			builder.Register(CreateCatalog, Lifetime.Singleton);
			builder.Register<GmOperationRunner>(Lifetime.Singleton);
			builder.Register<IGmPresenter, GmPresenter>(Lifetime.Singleton);

			// 註冊成基底型別，啟動點不必知道遊戲側叫它什麼就取得到
			builder.Register(typeof(GmOperations), typeof(TOperations), Lifetime.Singleton);
		}
	#endregion

		private GmFieldCatalog CreateCatalog(IObjectResolver resolver)
		{
			var configs = resolver.Resolve<ConfigManager>();

			// 一個都沒註冊時容器解不出集合，只給內建欄位
			if(fieldRegistrationTypes.Count == 0) return new(configs);

			return new(configs, resolver.Resolve<IReadOnlyList<IGmFieldRegistration>>());
		}
	}
}