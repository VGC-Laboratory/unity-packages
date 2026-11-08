using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VGC.Localization
{
    /// <summary>
    /// 言語が変わったら、開いている EditorWindow / Editor のうち ILocalizable を実装しているものに知らせ、
    /// すべてのビューを描き直す。
    /// </summary>
    [InitializeOnLoad]
    internal static class LanguageChangeNotifier
    {
        static LanguageChangeNotifier()
        {
            LanguageSetting.Changed -= Notify;
            LanguageSetting.Changed += Notify;
        }

        private static void Notify(SystemLanguage language)
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window is ILocalizable localizable)
                    localizable.OnLanguageChanged(language);
            }

            foreach (var editor in Resources.FindObjectsOfTypeAll<Editor>())
            {
                if (editor is ILocalizable localizable)
                    localizable.OnLanguageChanged(language);
            }

            InternalEditorUtility.RepaintAllViews();
        }
    }
}
