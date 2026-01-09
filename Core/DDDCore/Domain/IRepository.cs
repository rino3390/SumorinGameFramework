using System;
using System.Collections.Generic;

namespace Rino.GameFramework.Core.DDDCore.Domain
{
    /// <summary>
    /// Repository 介面，定義 Entity 儲存庫的基本操作
    /// </summary>
    /// <typeparam name="TEntity">Entity 類型</typeparam>
    public interface IRepository<TEntity> where TEntity : Entity
    {
        /// <summary>
        /// 透過 Id 取得 Entity
        /// </summary>
        TEntity this[string id] { get; }

        /// <summary>
        /// 儲存的 Entity 數量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 所有 Entity 的 Id
        /// </summary>
        IEnumerable<string> Keys { get; }

        /// <summary>
        /// 所有 Entity
        /// </summary>
        IEnumerable<TEntity> Values { get; }

        /// <summary>
        /// 儲存 Entity
        /// </summary>
        void Save(TEntity entity);

        /// <summary>
        /// 刪除所有 Entity
        /// </summary>
        void DeleteAll();

        /// <summary>
        /// 透過 Id 刪除 Entity
        /// </summary>
        void DeleteById(string id);

        /// <summary>
        /// 嘗試取得指定 Id 的 Entity
        /// </summary>
        bool TryGet(string id, out TEntity value);

        /// <summary>
        /// 根據條件尋找符合的 Entity
        /// </summary>
        /// <param name="predicate">篩選條件</param>
        /// <returns>符合條件的 Entity，若無則回傳 null</returns>
        TEntity Find(Func<TEntity, bool> predicate);
    }
}
