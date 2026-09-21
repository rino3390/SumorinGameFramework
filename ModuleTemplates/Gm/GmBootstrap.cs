using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 的執行期啟動點，在遊戲的 LifetimeScope 底下組一個 GM 專用的 child LifetimeScope
	/// </summary>
	/// <remarks>
	///     依賴方向只能 GM 指向遊戲，遊戲的組裝根不接線。
	///     由遊戲側 GM 組件內掛 <c>[RuntimeInitializeOnLoadMethod]</c> 的進入點呼叫 <see cref="Launch{TOperations}" />。
	/// </remarks>
	public static class GmBootstrap
	{
		private static LifetimeScope scope;

		/// <summary>
		///     啟動 GM，之後每次載入場景都會檢查是否要重掛
		/// </summary>
		/// <param name="fieldRegistrationTypes">遊戲側 <see cref="IGmFieldRegistration" /> 實作的型別，沒有自訂欄位時省略</param>
		/// <typeparam name="TOperations">遊戲側的操作登記子類別</typeparam>
		public static void Launch<TOperations>(params Type[] fieldRegistrationTypes) where TOperations: GmOperations
		{
			var installer = new GmInstaller<TOperations>(fieldRegistrationTypes);

			SceneManager.sceneLoaded += (_, _) => Mount(installer);
			Mount(installer);
		}

		// 掛上去的 scope 是場景物件時，換場景會連 GM 一起帶走，所以每次載入都檢查一次。
		// scope 活著就什麼都不做，面板與欄位輸入值因此不會在同場景內被重建
		private static void Mount(IInstaller installer)
		{
			if(scope != null) return;

			var parent = LifetimeScope.Find<LifetimeScope>();

			if(parent == null)
			{
				Debug.LogWarning("GM 面板要掛在遊戲的 LifetimeScope 底下，場景中找不到，這次不啟動");
				return;
			}

			scope = parent.CreateChild(installer, "GM");
			scope.Container.Resolve<GmOperations>().Build();
		}
	}
}