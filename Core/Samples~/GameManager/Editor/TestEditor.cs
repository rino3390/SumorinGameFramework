#if UNITY_EDITOR
using Rino.GameFramework.Core.GameManagerBase.EditorBase;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace Rino.GameFramework.Sample.GameManager.Editor
{
    /// <summary>
    /// 測試用一般 Editor 視窗
    /// </summary>
    public class TestEditor : GameEditorMenuBase
    {
        public override string TabName => "測試頁籤";

        [ShowInInspector]
        public string TestField { get; set; } = "Hello World";

        [Button("測試按鈕")]
        private void TestButton()
        {
            UnityEngine.Debug.Log("測試按鈕被點擊");
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = SetTree();
            tree.Add("首頁", this);
            tree.Add("設定/選項 A", new SettingItem("選項 A"));
            tree.Add("設定/選項 B", new SettingItem("選項 B"));
            return tree;
        }

        private class SettingItem
        {
            [ShowInInspector, ReadOnly]
            public string Name { get; }

            [ShowInInspector]
            public bool Enabled { get; set; }

            public SettingItem(string name)
            {
                Name = name;
            }
        }
    }
}
#endif
