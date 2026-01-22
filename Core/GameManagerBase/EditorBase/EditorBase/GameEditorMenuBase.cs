using Sirenix.OdinInspector.Editor;

namespace Rino.GameFramework.GameManagerBase
{
    /// <summary>
    /// GameManager 選單模組的基底類別
    /// </summary>
    /// <remarks>
    /// 此類別不繼承 OdinMenuEditorWindow，而是作為獨立模組被 GameManager 嵌入使用。
    /// 這避免了多個 EditorWindow 繼承造成的 OnGUI 重複繪製問題。
    /// </remarks>
    public abstract class GameEditorMenuBase
    {
        /// <summary>
        /// 頁籤名稱
        /// </summary>
        public virtual string TabName => GetType().Name;

        /// <summary>
        /// 選單寬度
        /// </summary>
        public float MenuWidth { get; protected set; } = 220f;

        /// <summary>
        /// 選單樹
        /// </summary>
        public OdinMenuTree MenuTree { get; private set; }

        private bool initialized;

        /// <summary>
        /// 確保初始化完成
        /// </summary>
        public void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            OnInitialize();
            MenuTree = BuildMenuTree();
        }

        /// <summary>
        /// 強制重建選單樹
        /// </summary>
        public void ForceMenuTreeRebuild()
        {
            MenuTree = BuildMenuTree();
        }

        /// <summary>
        /// 初始化時呼叫
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 建立選單樹
        /// </summary>
        /// <returns>選單樹</returns>
        protected abstract OdinMenuTree BuildMenuTree();

        /// <summary>
        /// 設定選單樹的基本配置
        /// </summary>
        /// <param name="iconSize">圖示大小</param>
        /// <param name="drawSearchToolbar">是否顯示搜尋工具列</param>
        /// <param name="width">選單寬度</param>
        /// <returns>設定好的選單樹</returns>
        protected OdinMenuTree SetTree(float iconSize = 28, bool drawSearchToolbar = true, float width = 220f)
        {
            MenuWidth = width;
            var tree = new OdinMenuTree(true)
            {
                DefaultMenuStyle =
                {
                    IconSize = iconSize
                },
                Config =
                {
                    DrawSearchToolbar = drawSearchToolbar
                }
            };
            return tree;
        }

        public override string ToString() => TabName;
    }
}
