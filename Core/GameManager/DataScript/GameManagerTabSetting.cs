using Rino.GameFramework.Core.GameManager.EditorStruct;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace Rino.GameFramework.Core.GameManager.DataScript
{
    /// <summary>
    /// GameManager 頁籤設定
    /// </summary>
    public class GameManagerTabSetting : SerializedScriptableObject
    {
        /// <summary>
        /// 頁籤清單
        /// </summary>
        [ListDrawerSettings(CustomAddFunction = nameof(CreateNewTab))]
        public List<EditorTabData> Tabs = new();

        private EditorTabData CreateNewTab()
        {
            return new EditorTabData();
        }
    }
}
