using Rino.GameFramework.Core.GameManagerBase.EditorBase;

namespace Rino.GameFramework.Sample.GameManager.Editor
{
    /// <summary>
    /// 測試用資料 Editor 視窗
    /// </summary>
    public class TestDataEditor : CreateNewDataEditor<TestData>
    {
        protected override string DataRoot => "Data/Sample/TestData";

        protected override string DataTypeLabel => "測試資料";
    }
}
