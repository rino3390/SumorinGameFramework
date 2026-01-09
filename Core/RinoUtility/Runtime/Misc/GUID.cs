using System;

namespace Rino.GameFramework.Core.RinoUtility.Misc
{
    /// <summary>
    /// GUID 產生器工具類別
    /// </summary>
    public class GUID
    {
        /// <summary>
        /// 產生新的 GUID 字串
        /// </summary>
        /// <returns>新的 GUID 字串</returns>
        public static string NewGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
