using System;
using UnityEngine;

#if UNITY_EDITOR
using nadena.dev.ndmf.localization;
using UnityEditor;
#endif

namespace VGC.Gimmicks_MA.Lib.Localization
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
    /// VGCギミックのエディタ言語。実体はNDMF/Modular Avatarと共通の言語設定(LanguagePrefs)で、
    /// MAのインスペクタで言語を変えるとVGC側も切り替わる(逆も同じ)。
    /// </summary>
    [FilePath("ScriptableSingleton/VGC/LocalizeConfig.dat", FilePathAttribute.Location.ProjectFolder)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LocalizeConfig : ScriptableSingleton<LocalizeConfig>
    {
        // NDMFのLanguagePrefsが使うEditorPrefsのキー(未設定かどうかの判定にのみ使う)
        private const string NdmfLanguagePrefKey = "nadena.dev.ndmf.language-selection";

        private static readonly string[] SupportedLanguageCodes = { "en-us", "ja-jp", "ko-kr", "zh-hans", "zh-hant" };

        // 旧バージョンで保存していた言語。NDMF側が未設定のときに1回だけ引き継ぐ
        [SerializeField] private Language _language;

        public event Action<Language> OnLanguageChanged;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // NDMF/MAの言語選択に5言語を出す(登録済みでも問題ない)
            foreach (var code in SupportedLanguageCodes)
                LanguagePrefs.RegisterLanguage(code);

            // 旧設定の引き継ぎ
            if (!EditorPrefs.HasKey(NdmfLanguagePrefKey))
                LanguagePrefs.Language = ToCode(instance._language);

            // instanceはScriptableSingletonなのでGCされず、コールバックは常に有効
            LanguagePrefs.RegisterLanguageChangeCallback(instance,
                config => config.OnLanguageChanged?.Invoke(config.Language));
        }

        public Language Language
        {
            get => FromCode(LanguagePrefs.Language);
            set
            {
                if (Language == value)
                    return;

                _language = value;
                Save(true);
                // 変更通知はLanguagePrefsのコールバック経由でOnLanguageChangedに届く
                LanguagePrefs.Language = ToCode(value);
            }
        }

        public static string ToCode(Language language)
        {
            switch (language)
            {
                case Language.Japanese: return "ja-jp";
                case Language.Korean: return "ko-kr";
                case Language.ChineseSimplified: return "zh-hans";
                case Language.ChineseTraditional: return "zh-hant";
                default: return "en-us";
            }
        }

        public static Language FromCode(string code)
        {
            code = (code ?? "").ToLowerInvariant();

            if (code.StartsWith("ja")) return Language.Japanese;
            if (code.StartsWith("ko")) return Language.Korean;
            if (code == "zh-hant" || code == "zh-tw" || code == "zh-hk" || code == "zh-mo") return Language.ChineseTraditional;
            if (code.StartsWith("zh")) return Language.ChineseSimplified;
            return Language.English;
        }
    }
#endif
}
