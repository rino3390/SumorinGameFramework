using System.Collections.Generic;
using Sumorin.DDDCore;

namespace Sumorin.Attribute
{
	/// <summary>
	/// 屬性 Repository 介面，擴展基本 Repository 功能
	/// </summary>
	public interface IAttributeRepository: IRepository<Attribute>
	{
		/// <summary>
		/// 透過擁有者 Id 和屬性配置 Id 取得屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <returns>找到的屬性，若不存在則回傳 null</returns>
		Attribute Get(string ownerId, string configId);

		/// <summary>
		/// 取得指定擁有者的所有屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <returns>該擁有者的所有屬性</returns>
		List<Attribute> GetByOwnerId(string ownerId);

		/// <summary>
		/// 刪除指定擁有者的所有屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		void DeleteByOwnerId(string ownerId);
	}
}