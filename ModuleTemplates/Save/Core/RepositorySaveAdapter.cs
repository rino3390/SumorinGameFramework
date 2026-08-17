using System;
using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.Save
{
	/// <summary>
	///     Repository 的存檔轉接層，把一個 Repository 包裝成存檔參與者
	/// </summary>
	/// <typeparam name="TEntity">Entity 類型</typeparam>
	/// <remarks>
	///     由 Installer 以具體實體型別閉合泛型後綁定，Repository 本身不知道自己正在參與存檔。
	///     清空的順序是先釋放再刪除，含值面欄位的實體若不先釋放就刪除，舊實例上的值面不會被釋放。
	/// </remarks>
	public class RepositorySaveAdapter<TEntity>: ISaveParticipant where TEntity: Entity
	{
		/// <inheritdoc />
		public string SaveKey { get; }

		/// <inheritdoc />
		public int LoadOrder { get; }

		/// <inheritdoc />
		public bool IsGlobal { get; }

		private readonly IRepository<TEntity> repository;
		private readonly ISerializer serializer;

		/// <summary>
		///     建立 Repository 轉接層
		/// </summary>
		/// <param name="repository">要參與存檔的 Repository</param>
		/// <param name="serializer">序列化器</param>
		/// <param name="saveKey">存檔鍵</param>
		/// <param name="loadOrder">載入順序，數字越大越先載入</param>
		/// <param name="isGlobal">是否為跨存檔資料</param>
		public RepositorySaveAdapter(IRepository<TEntity> repository, ISerializer serializer, string saveKey, int loadOrder, bool isGlobal)
		{
			this.repository = repository;
			this.serializer = serializer;
			SaveKey = saveKey;
			LoadOrder = loadOrder;
			IsGlobal = isGlobal;
		}

	#region ISaveParticipant Members
		/// <inheritdoc />
		public string Export() => serializer.Serialize(new List<TEntity>(repository.Values));

		/// <inheritdoc />
		public void Import(string data)
		{
			if(string.IsNullOrEmpty(data)) return;

			var entities = serializer.Deserialize<List<TEntity>>(data);
			if(entities == null) return;

			foreach(var entity in entities)
			{
				repository.Save(entity);
			}
		}

		/// <inheritdoc />
		public void Clear()
		{
			foreach(var entity in repository.Values)
			{
				(entity as IDisposable)?.Dispose();
			}

			repository.DeleteAll();
		}
	#endregion
	}
}