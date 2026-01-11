using Rino.GameFramework.Core.AttributeSystem.Common;

namespace Rino.GameFramework.Core.AttributeSystem.Model
{
    /// <summary>
    /// 修改器，用於改變屬性值
    /// </summary>
    /// <remarks>
    /// 有 Id 但不繼承 Entity，由 Attribute 管理。
    /// Id 用於移除特定 Modifier，SourceId 用於批次移除同來源的所有 Modifier。
    /// </remarks>
    public class Modifier
    {
        /// <summary>
        /// 修改器的唯一識別碼
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 修改類型
        /// </summary>
        public ModifyType ModifyType { get; }

        /// <summary>
        /// 修改數值
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// 來源識別碼（如裝備 Id、Buff Id），用於批次移除同來源的所有 Modifier
        /// </summary>
        public string SourceId { get; }

        /// <summary>
        /// 修改器描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 建立修改器
        /// </summary>
        /// <param name="id">唯一識別碼</param>
        /// <param name="modifyType">修改類型</param>
        /// <param name="value">修改數值</param>
        /// <param name="sourceId">來源識別碼</param>
        /// <param name="description">描述（選填）</param>
        /// <exception cref="System.ArgumentException">當 id 或 sourceId 為 null 或空字串時拋出</exception>
        public Modifier(string id, ModifyType modifyType, int value, string sourceId, string description = "")
        {
            if (string.IsNullOrEmpty(id))
                throw new System.ArgumentException("Id cannot be null or empty.", nameof(id));
            if (string.IsNullOrEmpty(sourceId))
                throw new System.ArgumentException("SourceId cannot be null or empty.", nameof(sourceId));

            Id = id;
            ModifyType = modifyType;
            Value = value;
            SourceId = sourceId;
            Description = description ?? "";
        }
    }
}
