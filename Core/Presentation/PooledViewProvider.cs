using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Sumorin.Presentation
{
	/// <summary>
	///     池化型供應策略，從物件池借出，歸還即還池，適合生滅頻繁的 View
	///     借出時啟用 GameObject，歸還時先 Unbind 再停用，並收回池的父物件底下
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public class PooledViewProvider<TView>: IViewProvider<TView> where TView: MonoBehaviour, IBindableView
	{
		private readonly TView prefab;
		private readonly Transform parent;
		private readonly ObjectPool<TView> pool;

		/// <summary>
		///     建立池化型供應策略，並預先生成指定數量的實例
		/// </summary>
		/// <param name="prefab">生成用的 prefab</param>
		/// <param name="parent">閒置實例停放的父物件，借出後可自由改掛，歸還時收回</param>
		/// <param name="preloadCount">建立時預先生成的數量，生成後停用等待借出</param>
		public PooledViewProvider(TView prefab, Transform parent, int preloadCount = 0)
		{
			this.prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
			this.parent = parent;
			pool = new(Create, Activate, Deactivate);
			Preload(preloadCount);
		}

		/// <inheritdoc />
		public TView Get()
		{
			return pool.Get();
		}

		/// <inheritdoc />
		public void Release(TView view)
		{
			if(view == null) return;

			view.Unbind();
			pool.Release(view);
		}

		// ponytail: 內建池沒有預載，借出再歸還一輪就是預載。同一幀內啟用又停用，畫面看不到
		private void Preload(int count)
		{
			var views = new List<TView>();

			for(var i = 0; i < count; i++)
			{
				views.Add(pool.Get());
			}

			foreach(var view in views)
			{
				pool.Release(view);
			}
		}

		private TView Create()
		{
			return Object.Instantiate(prefab, parent);
		}

		private static void Activate(TView view)
		{
			view.gameObject.SetActive(true);
		}

		private void Deactivate(TView view)
		{
			view.gameObject.SetActive(false);
			view.transform.SetParent(parent, false);
		}
	}
}