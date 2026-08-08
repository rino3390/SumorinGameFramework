using UnityEngine.Localization;

namespace Sumorin.SumorinUtility
{
    /// <summary>
    /// LocalizedString 擴充方法
    /// </summary>
    public static class LocalizedStringExtensions
    {
        /// <summary>
        /// 檢查 LocalizedString 是否為 null 或未設定有效參考
        /// </summary>
        /// <param name="localizedString">要檢查的 LocalizedString</param>
        /// <returns>為 null 或未設定有效參考則回傳 true</returns>
        public static bool IsNullOrEmpty(this LocalizedString localizedString)
        {
            return localizedString is not { IsEmpty: false };
        }
    }
}
