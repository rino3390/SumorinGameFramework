using System.Collections.Generic;
using Sumorin.DDDCore;
using UniRx;

namespace Sumorin.Attribute
{
	/// <summary>
	///     屬性 Controller 介面（資源型）
	/// </summary>
	public interface IAttributeController
	{
		/// <summary>
		///     訂閱特定屬性的當前值
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <returns>屬性值快照，訂閱時立即收到現值；屬性不存在時回傳 null</returns>
		IReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string attributeName);

		/// <summary>
		///     取得屬性值
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <returns>屬性值，若不存在回傳 0</returns>
		int GetValue(string ownerId, string attributeName);

		/// <summary>
		///     設定屬性的基礎值
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="value">新的基礎值</param>
		CommandResult SetBaseValue(string ownerId, string attributeName, int value);

		/// <summary>
		///     設定屬性的最小值，配置有定義關聯下限時忽略本次設定
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="value">新的最小值</param>
		CommandResult SetMinValue(string ownerId, string attributeName, int value);

		/// <summary>
		///     設定屬性的最大值，配置有定義關聯上限時忽略本次設定
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="value">新的最大值</param>
		CommandResult SetMaxValue(string ownerId, string attributeName, int value);

		/// <summary>
		///     根據效果資訊新增修改器，目標屬性不存在時自動建立
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="effect">修改效果資訊</param>
		/// <param name="sourceId">來源識別碼</param>
		/// <param name="description">描述（選填）</param>
		/// <returns>成功時酬載為修改器識別碼</returns>
		CommandResult AddModifier(string ownerId, ModifyEffectInfo effect, string sourceId, string description = "");

		/// <summary>
		///     批次新增修改器
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="effects">修改效果資訊列表</param>
		/// <param name="sourceId">來源識別碼</param>
		/// <param name="description">描述（選填）</param>
		CommandResult AddModifiers(string ownerId, List<ModifyEffectInfo> effects, string sourceId, string description = "");

		/// <summary>
		///     透過 Id 移除特定修改器
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="modifierId">修改器識別碼</param>
		CommandResult RemoveModifierById(string ownerId, string attributeName, string modifierId);

		/// <summary>
		///     移除指定來源在該屬性上的所有修改器
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="sourceId">來源識別碼</param>
		CommandResult RemoveModifiersBySource(string ownerId, string attributeName, string sourceId);

		/// <summary>
		///     移除第一個符合效果資訊的修改器
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="effect">修改效果資訊</param>
		/// <param name="sourceId">來源識別碼</param>
		CommandResult RemoveModifier(string ownerId, ModifyEffectInfo effect, string sourceId);

		/// <summary>
		///     移除指定來源在所有屬性中的修改器
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="sourceId">來源識別碼</param>
		CommandResult RemoveAllModifiersBySource(string ownerId, string sourceId);

		/// <summary>
		///     建立屬性
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		/// <param name="baseValue">基礎值</param>
		/// <returns>成功時酬載為屬性識別碼</returns>
		CommandResult CreateAttribute(string ownerId, string attributeName, int baseValue);

		/// <summary>
		///     移除特定屬性
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="attributeName">屬性名稱</param>
		CommandResult RemoveAttribute(string ownerId, string attributeName);

		/// <summary>
		///     移除指定擁有者的所有屬性
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		CommandResult RemoveAttributesByOwner(string ownerId);
	}
}