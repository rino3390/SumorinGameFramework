namespace Rino.GameFramework.Core.RinoUtility.Attribute
{
    /// <summary>
    /// 標記清單項目必須唯一的屬性，用於驗證清單中不可有重複值
    /// </summary>
    public class UniqueListAttribute : System.Attribute
    {
        /// <summary>
        /// 當清單項目重複時顯示的錯誤訊息
        /// </summary>
        public string ErrorMessage { get; }

        public UniqueListAttribute(string errorMessage = "清單值重複")
        {
            ErrorMessage = errorMessage;
        }
    }
}
