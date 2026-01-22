using System.Text.RegularExpressions;

namespace Rino.GameFramework.RinoUtility
{
    /// <summary>
    /// 正規表達式驗證工具類別
    /// </summary>
    public class RegexChecking
    {
        /// <summary>
        /// 檢查字串是否僅包含英文字母、數字、橫線、底線和空格
        /// </summary>
        /// <param name="checkString">要檢查的字串</param>
        /// <returns>如果字串僅包含允許的字元則回傳 true，否則回傳 false</returns>
        public static bool OnlyEnglishAndNum(string checkString)
        {
            return Regex.IsMatch(checkString, @"^[a-zA-Z0-9-_ ]+$");
        }
    }
}
