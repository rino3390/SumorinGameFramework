using System;
using Rino.GameFramework.Core.AttributeSystem.Common;
using Rino.GameFramework.Core.AttributeSystem.Model;
using Attribute = Rino.GameFramework.Core.AttributeSystem.Model.Attribute;

namespace Rino.GameFramework.Core.AttributeSystem.Controller
{
    /// <summary>
    /// 屬性 Controller 介面（資源型）
    /// </summary>
    public interface IAttributeController
    {
        /// <summary>
        /// 訂閱特定屬性的變化
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <returns>屬性變化資訊的 Observable，訂閱時會立即收到當前值；若屬性不存在則回傳 Empty Observable</returns>
        IObservable<AttributeChangedInfo> ObserveAttribute(string ownerId, string attributeName);

        /// <summary>
        /// 訂閱所有同名屬性的變化
        /// </summary>
        /// <param name="attributeName">屬性名稱</param>
        /// <returns>屬性變化資訊的 Observable，僅在值變化時發送</returns>
        IObservable<AttributeChangedInfo> ObserveAttribute(string attributeName);

        /// <summary>
        /// 取得屬性值
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <returns>屬性值，若不存在回傳 0</returns>
        int GetValue(string ownerId, string attributeName);

        /// <summary>
        /// 設定屬性的基礎值
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="value">新的基礎值</param>
        void SetBaseValue(string ownerId, string attributeName, int value);

        /// <summary>
        /// 設定屬性的最小值
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="value">新的最小值</param>
        void SetMinValue(string ownerId, string attributeName, int value);

        /// <summary>
        /// 設定屬性的最大值
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="value">新的最大值</param>
        void SetMaxValue(string ownerId, string attributeName, int value);

        /// <summary>
        /// 新增修改器到屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="modifier">修改器</param>
        void AddModifier(string ownerId, string attributeName, Modifier modifier);

        /// <summary>
        /// 透過 Id 移除特定修改器
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="modifierId">修改器識別碼</param>
        void RemoveModifierById(string ownerId, string attributeName, string modifierId);

        /// <summary>
        /// 移除指定來源的所有修改器
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="sourceId">來源識別碼</param>
        void RemoveModifiersBySource(string ownerId, string attributeName, string sourceId);

        /// <summary>
        /// 建立屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        /// <param name="baseValue">基礎值</param>
        /// <returns>建立的屬性</returns>
        Attribute CreateAttribute(string ownerId, string attributeName, int baseValue);

        /// <summary>
        /// 移除特定屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        /// <param name="attributeName">屬性名稱</param>
        void RemoveAttribute(string ownerId, string attributeName);

        /// <summary>
        /// 移除指定擁有者的所有屬性
        /// </summary>
        /// <param name="ownerId">擁有者識別碼</param>
        void RemoveAttributesByOwner(string ownerId);
    }
}
