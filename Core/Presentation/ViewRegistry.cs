using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sumorin.Presentation
{
	/// <summary>
	///     View 的身分索引，用 id 生成、取用、銷毀 View，呼叫端只認 id 不直接持有 View
	/// </summary>
	/// <typeparam name="TView">View 類型</typeparam>
	public class ViewRegistry<TView> where TView: MonoBehaviour, IBindableView
	{
		/// <summary>
		///     索引內所有仍存活的 View，已被銷毀的條目不會出現
		/// </summary>
		public IEnumerable<TView> Values => views.Values.Where(view => view != null);

		private readonly IViewProvider<TView> provider;
		private readonly Dictionary<string, TView> views = new();

		/// <summary>
		///     建立身分索引
		/// </summary>
		/// <param name="provider">決定 View 從哪來、回哪去的供應策略</param>
		public ViewRegistry(IViewProvider<TView> provider)
		{
			this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		/// <summary>
		///     以指定 id 生成 View，id 已存在時先銷毀原本的 View 再覆蓋條目
		/// </summary>
		/// <param name="id">Domain Entity 的 id</param>
		/// <returns>生成的 View</returns>
		public TView Create(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("id 不可為 null 或空白", nameof(id));
			}

			Remove(id);

			var view = provider.Get();
			views[id] = view;
			return view;
		}

		/// <summary>
		///     取得指定 id 的 View，View 已被銷毀時自動移除該條目並回報找不到
		/// </summary>
		/// <param name="id">Domain Entity 的 id</param>
		/// <param name="view">找到的 View，找不到時為 null</param>
		/// <returns>是否找到存活的 View</returns>
		public bool TryGet(string id, out TView view)
		{
			view = null;

			if(id == null) return false;
			if(views.TryGetValue(id, out var found) == false) return false;

			if(found == null)
			{
				views.Remove(id);
				return false;
			}

			view = found;
			return true;
		}

		/// <summary>
		///     銷毀指定 id 的 View，先從索引移除再交給供應策略回收，id 不存在時靜默返回
		/// </summary>
		/// <param name="id">Domain Entity 的 id</param>
		public void Remove(string id)
		{
			if(id == null) return;

			if(!views.Remove(id, out var view)) return;

			if(view == null) return;

			provider.Release(view);
		}

		/// <summary>
		///     清空索引，逐一走銷毀流程，索引與物件池一併清乾淨
		/// </summary>
		public void Clear()
		{
			foreach(var id in views.Keys.ToArray())
			{
				Remove(id);
			}
		}
	}
}