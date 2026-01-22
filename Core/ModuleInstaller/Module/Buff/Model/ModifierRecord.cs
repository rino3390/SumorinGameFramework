namespace Rino.GameFramework.BuffSystem
{
    /// <summary>
    /// 記錄 Buff 產生的 Modifier，用於移除時清理
    /// </summary>
    public class ModifierRecord
    {
        /// <summary>
        /// 目標屬性名稱
        /// </summary>
        public string AttributeName { get; }

        /// <summary>
        /// Modifier 識別碼
        /// </summary>
        public string ModifierId { get; }

        public ModifierRecord(string attributeName, string modifierId)
        {
            AttributeName = attributeName;
            ModifierId = modifierId;
        }
    }
}
