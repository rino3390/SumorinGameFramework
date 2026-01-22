using System.Collections.Generic;
using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// Buff Repository 介面，擴展基本 Repository 功能
    /// </summary>
    public interface IBuffRepository : IRepository<Buff>
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
    }
}
