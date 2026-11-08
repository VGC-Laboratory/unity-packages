using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Localization
{
    public enum Language
    {
        English,
        Japanese,
        Korean,
        ChineseSimplified,
        ChineseTraditional
    }

#if UNITY_EDITOR
    /// <summary>
    /// 翻訳に使うエディタ言語。VRChat の SDK に依存しない。
    ///
    /// 既定では EditorPrefs に保存する。アバター用プロジェクトでは
    /// VGC.Localization.Avatar.NdmfLanguageProvider が [InitializeOnLoad] で差し替え、
    /// NDMF / Modular Avatar と共通の言語設定(LanguagePrefs)に相乗りする
    /// (MA のインスペクタで言語を変えると VGC 側も切り替わる。逆も同じ)。
    /// </summary>
    public static class LanguageSetting
    {
        private const string FallbackPrefsKey = "VGC.Localization.Language";

        /// <summary>現在の言語を返す。差し替えられていなければ EditorPrefs を読む。</summary>
        public static Func<Language> Getter = GetFromEditorPrefs;

        /// <summary>言語を設定する。差し替えられていなければ EditorPrefs に書く。</summary>
        public static Action<Language> Setter = SetToEditorPrefs;

        /// <summary>言語が変わったときに呼ばれる。</summary>
        public static event Action<Language> Changed;

        public static Language Current
        {
            get => (Getter ?? GetFromEditorPrefs)();
            set
            {
                if (Current == value)
                    return;

                (Setter ?? SetToEditorPrefs)(value);
                NotifyChanged(value);
            }
        }

        /// <summary>
        /// 差し替えた側から、言語が変わったことを知らせる。
        /// </summary>
        public static void NotifyChanged(Language language)
        {
            Changed?.Invoke(language);
        }

        private static Language GetFromEditorPrefs()
        {
            var raw = EditorPrefs.GetString(FallbackPrefsKey, nameof(Language.English));
            return Enum.TryParse(raw, out Language parsed) ? parsed : Language.English;
        }

        private static void SetToEditorPrefs(Language language)
        {
            EditorPrefs.SetString(FallbackPrefsKey, language.ToString());
        }
    }
#endif
}
