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

        public string Id => id;
        public LangValue[] Values => values;
    }

    [Serializable]
    public class LangValue
    {
        [SerializeField] private string language;
        [SerializeField] private string text;

        public string Language => language;
        public string Text => text;
    }
    
    public class LocalizationTable : ScriptableObject
    {
        [SerializeField] private string _sheetName;
        [SerializeField] private Entry[] _entries;
        public string SheetName => _sheetName;
        public Entry[] Entries => _entries;

        private Dictionary<string, Dictionary<string, string>> _dict;

        public void Initialize(string sheetName, LocalizationTableData data)
        {
            _sheetName = sheetName;
            _entries = data.Entries;
            Build();
        }

        public string Get(string id, params object[] args)
        {
            return string.Format(Get(id), args);
        }

        public string Get(string id, Language fallback = Language.English)
        {
#if UNITY_EDITOR
            var lang = LanguageSetting.Current;
#else
            var lang = fallback;       
#endif
            return Get(id, lang, fallback);
        }
        
        public string Get(string id, Language lang, Language fallback)
        {
            if (_dict == null)
                Build();


            if (_dict != null && _dict.TryGetValue(id, out var dictionary))
            {
                if (dictionary.TryGetValue(lang.ToString(), out var text))
                    return text;

                if (dictionary.TryGetValue(fallback.ToString(), out var fb))
                    return fb;
            }

            return id;
        }

        /// <summary>
        /// 指定した言語の文字列を探す。その言語に無い(または空の)ときはfalseで、他の言語にはフォールバックしない。
        /// NDMFのLocalizerやメニュー翻訳のように、フォールバックを呼び出し側で行うときに使う。
        /// </summary>
        public bool TryGet(string id, Language lang, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(id))
                return false;

            if (_dict == null)
                Build();

            return _dict != null &&
                   _dict.TryGetValue(id, out var dictionary) &&
                   dictionary.TryGetValue(lang.ToString(), out text) &&
                   !string.IsNullOrEmpty(text);
        }

        private void Build()
        {
            _dict = new Dictionary<string, Dictionary<string, string>>();

            foreach (var e in _entries)
            {
                var langDict = new Dictionary<string, string>();

                foreach (var v in e.Values)
                {
                    langDict[v.Language] = v.Text?.Replace("\\n", "\n");
                }

                _dict[e.Id] = langDict;
            }
        }
    }
}