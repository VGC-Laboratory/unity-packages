#if UNITY_EDITOR
using nadena.dev.ndmf.localization;
using UnityEditor;
using UnityEngine;
using VGC.Localization;

namespace VGC.Localization.Avatar
{
    /// <summary>
    /// VGC の翻訳のエディタ言語を、NDMF / Modular Avatar と共通の言語設定(LanguagePrefs)に相乗りさせる。
    /// MA のインスペクタで言語を変えると VGC 側も切り替わる(逆も同じ)。
    ///
    /// 共通側の <see cref="LanguageSetting"/> に自分を差し込むだけで、
    /// 共通側は NDMF を一切参照しない。NDMF が無いワールド用プロジェクトでは
    /// このアセンブリ自体がコンパイルされないので、LanguageSetting は EditorPrefs の既定動作になる。
    /// </summary>
    [FilePath("ScriptableSingleton/VGC/NdmfLanguageProvider.dat", FilePathAttribute.Location.ProjectFolder)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class NdmfLanguageProvider : ScriptableSingleton<NdmfLanguageProvider>
    {
        // NDMF の LanguagePrefs が使う EditorPrefs のキー(未設定かどうかの判定にのみ使う)
        private const string NdmfLanguagePrefKey = "nadena.dev.ndmf.language-selection";

        private static readonly string[] SupportedLanguageCodes = { "en-us", "ja-jp", "ko-kr", "zh-hans", "zh-hant" };

        // 旧バージョンで保存していた言語。NDMF 側が未設定のときに1回だけ引き継ぐ
        [SerializeField] private SystemLanguage _language = SystemLanguage.English;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // NDMF/MA の言語選択に5言語を出す(登録済みでも問題ない)
            foreach (var code in SupportedLanguageCodes)
                LanguagePrefs.RegisterLanguage(code);

            // 旧設定の引き継ぎ
            if (!EditorPrefs.HasKey(NdmfLanguagePrefKey))
                LanguagePrefs.Language = ToCode(instance._language);

            // 共通側の言語の出し入れを NDMF 側へつなぐ
            LanguageSetting.Getter = () => FromCode(LanguagePrefs.Language);
            LanguageSetting.Setter = language =>
            {
                instance._language = language;
                instance.Save(true);
                LanguagePrefs.Language = ToCode(language);
            };

            // instance は ScriptableSingleton なので GC されず、コールバックは常に有効
            LanguagePrefs.RegisterLanguageChangeCallback(instance,
                _ => LanguageSetting.NotifyChanged(FromCode(LanguagePrefs.Language)));
        }

        public static string ToCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Japanese: return "ja-jp";
                case SystemLanguage.Korean: return "ko-kr";
                case SystemLanguage.ChineseSimplified: return "zh-hans";
                case SystemLanguage.ChineseTraditional: return "zh-hant";
                default: return "en-us";
            }
        }

        public static SystemLanguage FromCode(string code)
        {
            code = (code ?? "").ToLowerInvariant();

            if (code.StartsWith("ja")) return SystemLanguage.Japanese;
            if (code.StartsWith("ko")) return SystemLanguage.Korean;
            if (code == "zh-hant" || code == "zh-tw" || code == "zh-hk" || code == "zh-mo") return SystemLanguage.ChineseTraditional;
            if (code.StartsWith("zh")) return SystemLanguage.ChineseSimplified;
            return SystemLanguage.English;
        }
    }
}
#endif
