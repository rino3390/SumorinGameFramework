using Sirenix.OdinInspector;
using UnityEngine;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// 包含圖示的資料基底類別
    /// </summary>
    public abstract class IconIncludedData : InfoData
    {
        /// <summary>
        /// 資料圖示
        /// </summary>
        [HideLabel, PreviewField(70, ObjectFieldAlignment.Center)]
        [HorizontalGroup(LayoutConst.TopInfoLayout, 200)]
        [PropertyOrder(-1), PropertySpace(20)]
        public Sprite Icon;
    }
}
