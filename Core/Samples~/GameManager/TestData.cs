using Rino.GameFramework.GameManagerBase;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Rino.GameFramework.Sample.GameManager
{
    /// <summary>
    /// 測試用資料類別
    /// </summary>
    public class TestData : SODataBase
    {
        [BoxGroup("資料設定")]
        [LabelText("名稱")]
        public string Name;

        [BoxGroup("資料設定")]
        [LabelText("數值")]
        [Range(0, 100)]
        public int Value;

        [BoxGroup("資料設定")]
        [LabelText("描述")]
        [TextArea(3, 5)]
        public string Description;

#if UNITY_EDITOR
        public override bool IsDataLegal()
        {
            return base.IsDataLegal() && !string.IsNullOrEmpty(Name);
        }
#endif
    }
}
