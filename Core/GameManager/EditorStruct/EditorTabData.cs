using Rino.GameFramework.GameManagerBase;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Linq;

namespace Rino.GameFramework.GameManager
{
    /// <summary>
    /// Editor 頁籤資料
    /// </summary>
    [HideReferenceObjectPicker]
    public class EditorTabData
    {
        /// <summary>
        /// 頁籤圖示
        /// </summary>
        [FoldoutGroup("標籤設定", true)]
        public SdfIconType TabIcon;

        /// <summary>
        /// 是否有左側選單
        /// </summary>
        [FoldoutGroup("Editor 設定", true)]
        [LabelText("是否有左側菜單")]
        public bool HasMenuTree;

        /// <summary>
        /// 是否繪製圖示
        /// </summary>
        [FoldoutGroup("Editor 設定")]
        [ShowIf("HasMenuTree"), LabelText("左列繪製 Icon")]
        public bool HasIcon;

        /// <summary>
        /// 圖示大小
        /// </summary>
        [FoldoutGroup("Editor 設定")]
        [ShowIf("@HasMenuTree && HasIcon"), LabelText("Icon 大小")]
        public float IconSize = 28;

        /// <summary>
        /// 對應的 Editor 視窗
        /// </summary>
        [FoldoutGroup("Editor 設定")]
        [ValueDropdown("GetWindowList")]
        [LabelText("繪製視窗")]
        [Required]
        public GameEditorMenuBase CorrespondingWindow;

        private static IEnumerable GetWindowList()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(x => !x.IsAbstract)
                .Where(x => !x.IsGenericTypeDefinition)
                .Where(x => typeof(GameEditorMenuBase).IsAssignableFrom(x))
                .Select(x =>
                {
                    var instance = Activator.CreateInstance(x) as GameEditorMenuBase;
                    return new ValueDropdownItem(instance?.TabName ?? x.Name, instance);
                });
        }
    }
}
