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
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <returns>屬性值快照，訂閱時立即收到現值；屬性不存在時回傳 null</returns>
		IReadOnlyReactiveProperty<AttributeValueInfo> ObserveAttribute(string ownerId, string configId);

		/// <summary>
		///     取得屬性值
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <returns>屬性值，若不存在回傳 0</returns>
		int GetValue(string ownerId, string configId);

		/// <summary>
		///     取得屬性的當前上限，供其他 Domain 計算溢出量
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <returns>當前上限，若不存在回傳 0</returns>
		int GetMaxValue(string ownerId, string configId);

		/// <summary>
		///     取得屬性的當前下限
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <returns>當前下限，若不存在回傳 0</returns>
		int GetMinValue(string ownerId, string configId);

		/// <summary>
		///     設定屬性的基礎值
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="value">新的基礎值</param>
		CommandResult SetBaseValue(string ownerId, string configId, int value);

		/// <summary>
		///     增減屬性的基礎值，直接對基礎值運算，不經過套用修改器後的最終值
		/// </summary>
		/// <remarks>
		///     資源型屬性的當前量一律用本命令變動。
		///     若先取最終值再算回寫，修改器的效果會烙進基礎值，下次計算再套一次。
		/// </remarks>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="delta">增減量</param>
		CommandResult AdjustBaseValue(string ownerId, string configId, int delta);

		/// <summary>
		///     設定屬性的最小值，配置有定義關聯下限時忽略本次設定
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="value">新的最小值</param>
		CommandResult SetMinValue(string ownerId, string configId, int value);

		/// <summary>
		///     設定屬性的最大值，配置有定義關聯上限時忽略本次設定
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="value">新的最大值</param>
		CommandResult SetMaxValue(string ownerId, string configId, int value);

		/// <summary>
		///     根據效果資訊新增修改器，目標屬性不存在時自動建立，目標為資源型屬性時失敗
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="effect">修改效果資訊</param>
		/// <param name="sourceId">來源 Id</param>
		/// <param name="description">描述（選填）</param>
		/// <returns>成功時酬載為修改器 Id</returns>
		CommandResult AddModifier(string ownerId, ModifyEffectInfo effect, string sourceId, string description = "");

		/// <summary>
		///     批次新增修改器，任一目標為資源型屬性時整批失敗，不套用任何效果
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="effects">修改效果資訊列表</param>
		/// <param name="sourceId">來源 Id</param>
		/// <param name="description">描述（選填）</param>
		CommandResult AddModifiers(string ownerId, List<ModifyEffectInfo> effects, string sourceId, string description = "");

		/// <summary>
		///     透過 Id 移除特定修改器
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="modifierId">修改器 Id</param>
		CommandResult RemoveModifierById(string ownerId, string configId, string modifierId);

		/// <summary>
		///     移除指定來源在該屬性上的所有修改器
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="sourceId">來源 Id</param>
		CommandResult RemoveModifiersBySource(string ownerId, string configId, string sourceId);

		/// <summary>
		///     移除第一個符合效果資訊的修改器
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="effect">修改效果資訊</param>
		/// <param name="sourceId">來源 Id</param>
		CommandResult RemoveModifier(string ownerId, ModifyEffectInfo effect, string sourceId);

		/// <summary>
		///     移除指定來源在所有屬性中的修改器
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="sourceId">來源 Id</param>
		CommandResult RemoveAllModifiersBySource(string ownerId, string sourceId);

		/// <summary>
		///     建立屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		/// <param name="baseValue">基礎值</param>
		/// <returns>成功時酬載為屬性 Id</returns>
		CommandResult CreateAttribute(string ownerId, string configId, int baseValue);

		/// <summary>
		///     移除特定屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		/// <param name="configId">屬性配置 Id</param>
		CommandResult RemoveAttribute(string ownerId, string configId);

		/// <summary>
		///     移除指定擁有者的所有屬性
		/// </summary>
		/// <param name="ownerId">擁有者 Id</param>
		CommandResult RemoveAttributesByOwner(string ownerId);
	}
}