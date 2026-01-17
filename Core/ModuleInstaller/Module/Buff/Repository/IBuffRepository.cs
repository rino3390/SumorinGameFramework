using System.Collections.Generic;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff Repository 介面
    /// </summary>
    public interface IBuffRepository
    {
        /// <summary>
        /// 透過 Id 取得 Buff
        /// </summary>
        /// <param name="buffId">Buff 識別碼</param>
        /// <returns>找到的 Buff，若不存在則回傳 null</returns>
        Buff Get(string buffId);

        /// <summary>
        /// 取得指定擁有者的所有 Buff
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <returns>該擁有者的所有 Buff</returns>
        IEnumerable<Buff> GetByOwner(string ownerId);

        /// <summary>
        /// 取得所有 Buff
        /// </summary>
        /// <returns>所有 Buff</returns>
        IEnumerable<Buff> GetAll();

        /// <summary>
        /// 新增 Buff
        /// </summary>
        /// <param name="buff">要新增的 Buff</param>
        void Add(Buff buff);

        /// <summary>
        /// 移除 Buff
        /// </summary>
        /// <param name="buffId">Buff 識別碼</param>
        void Remove(string buffId);
    }
}
