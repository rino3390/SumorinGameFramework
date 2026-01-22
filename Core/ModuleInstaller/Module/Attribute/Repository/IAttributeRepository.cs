using System.Collections.Generic;
using Rino.GameFramework.DDDCore;

namespace Rino.GameFramework.AttributeSystem
{
    /// <summary>
    /// 屬性 Repository 介面，擴展基本 Repository 功能
    /// </summary>
    public interface IAttributeRepository : IRepository<Attribute>
    {
        /// <summary>
        /// 透過擁有者 Id 和屬性名稱取得屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <returns>找到的屬性，若不存在則回傳 null</returns>
        Attribute Get(string ownerId, string attributeName);

        /// <summary>
        /// 取得指定擁有者的所有屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <returns>該擁有者的所有屬性</returns>
        List<Attribute> GetByOwnerId(string ownerId);

        /// <summary>
        /// 刪除指定擁有者的所有屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        void DeleteByOwnerId(string ownerId);
    }
}
