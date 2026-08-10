using System;
using UnityEngine;
using Zenject;

namespace Sumorin.Presentation
{
	/// <summary>
	///     池化型供應策略，從物件池借出，歸還即還池，適合生滅頻繁的 View
	///     借出與歸還時由物件池啟用與停用 GameObject，歸還時一併復原 parent
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public class PooledViewProvider<TView>: IViewProvider<TView> where TView: MonoBehaviour, IBindableView
	{
		private readonly MonoMemoryPool<TView> pool;

		/// <summary>
		///     建立池化型供應策略
		/// </summary>
		/// <param name="pool">View 的物件池</param>
		public PooledViewProvider(MonoMemoryPool<TView> pool)
		{
			this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
		}

		/// <inheritdoc />
		public TView Get()
		{
			return pool.Spawn();
		}

		/// <inheritdoc />
		public void Release(TView view)
		{
			if(view == null) return;

			view.Unbind();
			pool.Despawn(view);
		}
	}
}