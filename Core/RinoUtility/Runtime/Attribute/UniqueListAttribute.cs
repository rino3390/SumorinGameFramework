namespace Rino.GameFramework.RinoUtility
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

        /// <summary>
        /// 要檢查唯一性的欄位或屬性名稱，若為 null 則比對整個物件
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// 建立唯一性驗證屬性
        /// </summary>
        /// <param name="propertyName">要檢查的欄位名稱，null 表示比對整個物件</param>
        /// <param name="errorMessage">重複時的錯誤訊息</param>
        public UniqueListAttribute(string propertyName = null, string errorMessage = "清單值重複")
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}
