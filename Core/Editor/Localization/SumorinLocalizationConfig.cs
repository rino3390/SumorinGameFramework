using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Sumorin.Localization.Editor
{
    /// <summary>
    /// Sumorin Localization 視窗的設定項，提供主要語言（Project Locale）選擇
    /// </summary>
    public class SumorinLocalizationConfig
    {
        /// <summary>
        /// 專案主要語言，對應 LocalizationSettings 的 Project Locale
        /// </summary>
        [ShowInInspector]
        [LabelText("主要語言")]
        [ValueDropdown(nameof(GetLocaleOptions))]
        public Locale PrimaryLocale
        {
            get => LocalizationSettings.HasSettings ? LocalizationSettings.ProjectLocale : null;
            set => SetPrimaryLocale(value);
        }

        private static IEnumerable<ValueDropdownItem<Locale>> GetLocaleOptions()
        {
            return LocalizationEditorSettings.GetLocales()
                                             .Select(locale => new ValueDropdownItem<Locale>(locale.ToString(), locale));
        }

        private static void SetPrimaryLocale(Locale locale)
        {
            if (locale == null || !LocalizationSettings.HasSettings) return;

            LocalizationSettings.ProjectLocale = locale;
            EditorUtility.SetDirty(LocalizationSettings.Instance);
            AssetDatabase.SaveAssetIfDirty(LocalizationSettings.Instance);
        }
    }
}
