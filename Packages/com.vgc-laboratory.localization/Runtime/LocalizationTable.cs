using System;
using System.Collections.Generic;
using UnityEngine;

namespace VGC.Localization
{
    [Serializable]
    public class LocalizationTableData
    {
        [SerializeField] private Entry[] entries;
        public Entry[] Entries => entries;
    }

    [Serializable]
    public class Entry
    {
        [SerializeField] private string id;
        [SerializeField] private LangValue[] values;

        public Entry() { }

        public Entry(string id, LangValue[] values)
        {
            this.id = id;
            this.values = values;
        }

        public string Id => id;
        public LangValue[] Values => values ?? Array.Empty<LangValue>();
    }

    [Serializable]
    public class LangValue
    {
        /// <summary>SystemLanguage の名前(English, Japanese など)。</summary>
        [SerializeField] private string language;
        [SerializeField] private string text;

        public LangValue() { }

        public LangValue(SystemLanguage language, string text)
        {
            this.language = language.ToString();
            this.text = text;
        }

        public string Language => language;
        public string Text => text;
    }

    /// <summary>
    /// キー × 言語の翻訳表。スプレッドシートの 1 シートに対応する。
    ///
    /// シートの形: 1 行目は見出しで、A 列が「key」、B 列以降が SystemLanguage の名前(English, Japanese ...)。
    /// 2 行目以降が 1 キー 1 行。インスペクタの「Import」で <see cref="Source"/> から取り込む。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(LocalizationTable), menuName = "VGC/Localization/Localization Table")]
    public class LocalizationTable : ScriptableObject
    {
        [Tooltip("取り込み元のスプレッドシート")]
        [SerializeField] private SpreadsheetSource _source;

        [Tooltip("取り込むシート(タブ)の名前")]
        [SerializeField] private string _sheetName;

        [Tooltip("指定した言語に訳が無いときに使う言語")]
        [SerializeField] private SystemLanguage _defaultLanguage = SystemLanguage.English;

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        public SpreadsheetSource Source => _source;
        public string SheetName => _sheetName;
        public SystemLanguage DefaultLanguage => _defaultLanguage;
        public Entry[] Entries => _entries ?? Array.Empty<Entry>();

        private Dictionary<string, Dictionary<string, string>> _dict;
        private List<SystemLanguage> _languages;

        private void OnEnable()
        {
            _dict = null;
            _languages = null;
        }

        /// <summary>表に含まれる言語(列)。</summary>
        public IReadOnlyList<SystemLanguage> Languages
        {
            get
            {
                if (_dict == null)
                    Build();
                return _languages;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var e in Entries)
                    yield return e.Id;
            }
        }

        public bool ContainsKey(string id)
        {
            if (_dict == null)
                Build();
            return id != null && _dict.ContainsKey(id);
        }

        /// <summary>中身を差し替える。取り込み処理から呼ぶ。</summary>
        public void SetEntries(string sheetName, Entry[] entries)
        {
            _sheetName = sheetName;
            _entries = entries ?? Array.Empty<Entry>();
            Build();
        }

        public void Initialize(string sheetName, LocalizationTableData data)
        {
            SetEntries(sheetName, data?.Entries);
        }

        /// <summary>
        /// 現在の言語の文字列。エディタではエディタの表示言語、ビルド後は既定言語を使う。
        /// </summary>
        public string Get(string id)
        {
#if UNITY_EDITOR
            return Get(id, LanguageSetting.Current);
#else
            return Get(id, _defaultLanguage);
#endif
        }

        /// <summary>現在の言語の文字列を string.Format で整形する。</summary>
        public string Get(string id, params object[] args)
        {
            var format = Get(id);
            return args == null || args.Length == 0 ? format : string.Format(format, args);
        }

        /// <summary>
        /// 指定した言語の文字列。無ければ表の既定言語、それも無ければ最初に見つかった訳、どれも無ければ id を返す。
        /// </summary>
        public string Get(string id, SystemLanguage language)
        {
            if (string.IsNullOrEmpty(id))
                return id;

            if (_dict == null)
                Build();

            if (!_dict.TryGetValue(id, out var values))
                return id;

            if (values.TryGetValue(language.ToString(), out var text) && !string.IsNullOrEmpty(text))
                return text;

            if (values.TryGetValue(_defaultLanguage.ToString(), out text) && !string.IsNullOrEmpty(text))
                return text;

            foreach (var v in values.Values)
            {
                if (!string.IsNullOrEmpty(v))
                    return v;
            }

            return id;
        }

        /// <summary>
        /// 指定した言語の文字列を探す。その言語に無い(または空の)ときは false で、他の言語にはフォールバックしない。
        /// NDMF の Localizer やメニュー翻訳のように、フォールバックを呼び出し側で行うときに使う。
        /// </summary>
        public bool TryGet(string id, SystemLanguage language, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(id))
                return false;

            if (_dict == null)
                Build();

            return _dict.TryGetValue(id, out var values) &&
                   values.TryGetValue(language.ToString(), out text) &&
                   !string.IsNullOrEmpty(text);
        }

        private void Build()
        {
            _dict = new Dictionary<string, Dictionary<string, string>>();
            _languages = new List<SystemLanguage>();

            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id))
                    continue;

                var values = new Dictionary<string, string>();

                foreach (var v in e.Values)
                {
                    if (v == null || string.IsNullOrEmpty(v.Language))
                        continue;

                    values[v.Language] = v.Text?.Replace("\\n", "\n");

                    if (LanguageNames.TryParse(v.Language, out var language) && !_languages.Contains(language))
                        _languages.Add(language);
                }

                _dict[e.Id] = values;
            }
        }
    }
}
