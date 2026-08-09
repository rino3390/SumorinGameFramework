using System;
using UnityEngine;
using Zenject;

namespace Sumorin.Presentation
{
	/// <summary>
	///     池化型供應策略，從物件池借出，歸還即還池，適合生滅頻繁的 View
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public class PooledViewProvider<TView>: IViewProvider<TView> where TView: MonoBehaviour, IBindableView
	{
		private readonly MemoryPool<TView> pool;

		/// <summary>
		///     建立池化型供應策略
		/// </summary>
		/// <param name="pool">View 的物件池</param>
		public PooledViewProvider(MemoryPool<TView> pool)
		{
			if(pool == null)
			{
				throw new ArgumentNullException(nameof(pool));
			}

			this.pool = pool;
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