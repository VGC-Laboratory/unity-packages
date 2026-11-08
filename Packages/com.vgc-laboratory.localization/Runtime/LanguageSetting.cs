using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Localization
{
#if UNITY_EDITOR
    /// <summary>
    /// プロジェクトで使う言語の一覧と既定言語。ProjectSettings/VGCLocalizationSettings.asset に保存する。
    /// Project Settings > VGC > Localization で編集する。
    /// </summary>
    [FilePath("ProjectSettings/VGCLocalizationSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocalizationProjectSettings : ScriptableSingleton<LocalizationProjectSettings>
    {
        [SerializeField] private List<SystemLanguage> _languages = new()
        {
            SystemLanguage.Japanese,
            SystemLanguage.English,
            SystemLanguage.Korean,
            SystemLanguage.ChineseSimplified,
            SystemLanguage.ChineseTraditional,
        };

        [SerializeField] private SystemLanguage _defaultLanguage = SystemLanguage.English;

        public IReadOnlyList<SystemLanguage> Languages => _languages;
        public SystemLanguage DefaultLanguage => _defaultLanguage;

        public void SetLanguages(IEnumerable<SystemLanguage> languages)
        {
            _languages = languages.Distinct().ToList();
            if (_languages.Count == 0)
                _languages.Add(SystemLanguage.English);
            if (!_languages.Contains(_defaultLanguage))
                _defaultLanguage = _languages[0];
            Save(true);
            LanguageSetting.NotifyChanged(LanguageSetting.Current);
        }

        public void SetDefaultLanguage(SystemLanguage language)
        {
            _defaultLanguage = language;
            if (!_languages.Contains(language))
                _languages.Add(language);
            Save(true);
            LanguageSetting.NotifyChanged(LanguageSetting.Current);
        }
    }

    /// <summary>
    /// エディタの表示言語。VRChat の SDK に依存しない。
    ///
    /// 既定では EditorPrefs に保存する。アバター用プロジェクトでは
    /// VGC.Localization.Avatar.NdmfLanguageProvider が [InitializeOnLoad] で Getter / Setter を差し替え、
    /// NDMF / Modular Avatar と共通の言語設定(LanguagePrefs)に相乗りする。
    /// </summary>
    public static class LanguageSetting
    {
        private const string PrefsKey = "VGC.Localization.Language";

        /// <summary>現在の言語を返す。差し替えられていなければ EditorPrefs を読む。</summary>
        public static Func<SystemLanguage> Getter = GetFromEditorPrefs;

        /// <summary>言語を設定する。差し替えられていなければ EditorPrefs に書く。</summary>
        public static Action<SystemLanguage> Setter = SetToEditorPrefs;

        /// <summary>言語(または使用言語の一覧)が変わったときに呼ばれる。</summary>
        public static event Action<SystemLanguage> Changed;

        /// <summary>プロジェクトで使う言語の一覧(Project Settings で設定する)。</summary>
        public static IReadOnlyList<SystemLanguage> Available => LocalizationProjectSettings.instance.Languages;

        /// <summary>訳が無いときに使う言語。</summary>
        public static SystemLanguage Default => LocalizationProjectSettings.instance.DefaultLanguage;

        public static SystemLanguage Current
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

        /// <summary>差し替えた側から、言語が変わったことを知らせる。</summary>
        public static void NotifyChanged(SystemLanguage language)
        {
            Changed?.Invoke(language);
        }

        private static SystemLanguage GetFromEditorPrefs()
        {
            var raw = EditorPrefs.GetString(PrefsKey, "");
            if (Enum.TryParse(raw, out SystemLanguage parsed))
                return parsed;

            // 初回は OS の言語が一覧にあればそれ、無ければ既定言語
            var system = Application.systemLanguage;
            return Available.Contains(system) ? system : Default;
        }

        private static void SetToEditorPrefs(SystemLanguage language)
        {
            EditorPrefs.SetString(PrefsKey, language.ToString());
        }
    }
#endif

    /// <summary>言語の自称(日本語、English など)。言語の選択肢に出す。</summary>
    public static class LanguageNames
    {
        public static string NativeName(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Japanese: return "日本語";
                case SystemLanguage.English: return "English";
                case SystemLanguage.Korean: return "한국어";
                case SystemLanguage.ChineseSimplified: return "简体中文";
                case SystemLanguage.ChineseTraditional: return "繁體中文";
                case SystemLanguage.Chinese: return "中文";
                case SystemLanguage.French: return "Français";
                case SystemLanguage.German: return "Deutsch";
                case SystemLanguage.Spanish: return "Español";
                case SystemLanguage.Portuguese: return "Português";
                case SystemLanguage.Italian: return "Italiano";
                case SystemLanguage.Russian: return "Русский";
                case SystemLanguage.Thai: return "ไทย";
                case SystemLanguage.Vietnamese: return "Tiếng Việt";
                case SystemLanguage.Indonesian: return "Bahasa Indonesia";
                default: return language.ToString();
            }
        }

        /// <summary>シートの見出しなどの文字列を言語に変換する。大文字小文字は区別しない。</summary>
        public static bool TryParse(string value, out SystemLanguage language)
        {
            language = SystemLanguage.Unknown;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();

            // SystemLanguage は 0 から連番なので、"5" のような数字は Enum.TryParse が通してしまい
            // Enum.IsDefined も true を返す。メモ用に数字で見出しを付けた列を言語と誤認しないよう、
            // 文字で始まらないものは弾く(言語名はすべて文字で始まる)。
            if (!char.IsLetter(trimmed[0]))
                return false;

            return Enum.TryParse(trimmed, true, out language) &&
                   Enum.IsDefined(typeof(SystemLanguage), language);
        }
    }

    /// <summary>
    /// 言語が変わったときに知らせてほしい EditorWindow / Editor が実装する。
    /// 開いているものには VGC.Localization.Editor の LanguageChangeNotifier が呼び出す。
    /// </summary>
    public interface ILocalizable
    {
        void OnLanguageChanged(SystemLanguage language);
    }
}
