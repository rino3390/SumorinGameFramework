using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 的執行期啟動點，掃出遊戲側的操作登記子類別後自己組一個 child LifetimeScope
	/// </summary>
	/// <remarks>
	///     依賴方向只能 GM 指向遊戲，所以組裝由這裡發起，遊戲的組裝根不需要接線。
	///     遊戲側沒有 <see cref="GmOperations" /> 子類別時什麼都不做。
	/// </remarks>
	public static class GmBootstrap
	{
		private static LifetimeScope scope;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Launch()
		{
			if(FindOperationsType() == null) return;

			SceneManager.sceneLoaded += (_, _) => Mount();
			Mount();
		}

		// 掛上去的 scope 是場景物件時，換場景會連 GM 一起帶走，所以每次載入都檢查一次。
		// scope 活著就什麼都不做，面板與欄位輸入值因此不會在同場景內被重建
		private static void Mount()
		{
			if(scope != null) return;

			var operationsType = FindOperationsType();
			if(operationsType == null) return;

			var parent = LifetimeScope.Find<LifetimeScope>();

			if(parent == null)
			{
				Debug.LogWarning("GM 面板要掛在遊戲的 LifetimeScope 底下，場景中找不到，這次不啟動");
				return;
			}

			var installerType = typeof(GmInstaller<>).MakeGenericType(operationsType);
			var installer = (IInstaller)Activator.CreateInstance(installerType, new object[] { FindFieldRegistrationTypes() });

			scope = parent.CreateChild(installer, "GM");
			scope.Container.Resolve<GmOperations>().Build();
		}

		private static Type FindOperationsType()
		{
			var candidates = new List<Type>();

			foreach(var type in ConcreteTypes())
			{
				if(typeof(GmOperations).IsAssignableFrom(type)) candidates.Add(type);
			}

			if(candidates.Count == 0) return null;
			if(candidates.Count == 1) return candidates[0];

			Debug.LogError($"一款遊戲只能有一個 GmOperations 子類別，目前有 {candidates.Count} 個：{string.Join("、", candidates)}");
			return null;
		}

		// 只收型別，實例交給容器建，欄位登記才注入得到候選清單來源
		private static Type[] FindFieldRegistrationTypes()
		{
			var types = new List<Type>();

			foreach(var type in ConcreteTypes())
			{
				if(typeof(IGmFieldRegistration).IsAssignableFrom(type)) types.Add(type);
			}

			return types.ToArray();
		}

		// 掃全部 assembly 只發生在啟動當下，而且只有 Editor 與 Development Build 才編得出這段程式碼
		private static IEnumerable<Type> ConcreteTypes()
		{
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;

				try
				{
					types = assembly.GetTypes();
				}
				catch(ReflectionTypeLoadException exception)
				{
					types = exception.Types;
				}

				foreach(var type in types)
				{
					if(type is { IsAbstract: false, IsInterface: false }) yield return type;
				}
			}
		}
	}
}