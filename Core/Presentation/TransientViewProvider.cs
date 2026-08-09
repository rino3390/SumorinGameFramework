using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sumorin.Presentation
{
	/// <summary>
	///     生滅型供應策略，每次借出都新建，歸還即銷毀，適合生滅不頻繁的 View
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public class TransientViewProvider<TView>: IViewProvider<TView> where TView: MonoBehaviour, IBindableView
	{
		private readonly TView prefab;

		/// <summary>
		///     建立生滅型供應策略
		/// </summary>
		/// <param name="prefab">生成用的 prefab</param>
		public TransientViewProvider(TView prefab)
		{
			if(prefab == null)
			{
				throw new ArgumentNullException(nameof(prefab));
			}

			this.prefab = prefab;
		}

		/// <inheritdoc />
		public TView Get()
		{
			return Object.Instantiate(prefab);
		}

		/// <inheritdoc />
		public void Release(TView view)
		{
			if(view == null) return;

			view.Unbind();

			// Editor 環境不接受 Destroy，需改用 DestroyImmediate
			if(Application.isPlaying)
			{
				Object.Destroy(view.gameObject);
			}
			else
			{
				Object.DestroyImmediate(view.gameObject);
			}
		}
	}
}
