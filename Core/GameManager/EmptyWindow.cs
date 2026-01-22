using Sirenix.OdinInspector.Editor;

namespace Rino.GameFramework.GameManager
{
    /// <summary>
    /// 空白視窗佔位符
    /// </summary>
    public class EmptyWindow : OdinMenuEditorWindow
    {
        /// <summary>
        /// 建立選單樹
        /// </summary>
        /// <returns>空的選單樹</returns>
        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true);
            return tree;
        }
    }
}
