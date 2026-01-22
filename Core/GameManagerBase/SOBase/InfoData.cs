using Sirenix.OdinInspector;
using UnityEngine.Localization;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// 包含顯示名稱的資料基底類別
    /// </summary>
    public abstract class InfoData : SODataBase
    {
        /// <summary>
        /// 本地化顯示名稱
        /// </summary>
        [LabelText("顯示名稱")]
        [HorizontalGroup(LayoutConst.TopInfoLayout)]
        [VerticalGroup(LayoutConst.TopInfoLayout + "/1")]
        [PropertySpace(10, 10)]
        [Required("需要填寫名稱")]
        public LocalizedString DataName;
    }
}
