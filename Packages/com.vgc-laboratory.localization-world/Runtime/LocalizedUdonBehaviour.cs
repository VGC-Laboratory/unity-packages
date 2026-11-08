using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace VGC.Localization.World
{
    /// <summary>
    /// 翻訳を使う UdonSharpBehaviour の基底。
    ///
    /// 同じ GameObject に LocalizationBindings を付けると、指定した翻訳表の全キーと、
    /// 「どの文字欄にどのキーを出すか」がビルド時に焼き込まれる。言語が変わると文字欄は自動で書き換わる。
    /// コードからは <see cref="_Get"/> でキーの文字列を引ける。
    /// 独自の処理をしたいときは <see cref="_OnLanguageChanged"/> を override し、base を呼ぶ。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalizedUdonBehaviour : UdonSharpBehaviour
    {
        // ===== ビルド時に焼き込む =====
        [SerializeField, HideInInspector] protected LocalizationSystem _localizationSystem;
        [SerializeField, HideInInspector] private string[] _localizedKeys = { };

        /// <summary>[言語の番号][キーの番号]。訳が無いところは既定言語(それも無ければキー)で埋めてある。</summary>
        /// ジャグ配列は Unity が直列化できないので OdinSerialize で保存する(UdonSharp の指定)。
        [SerializeField, HideInInspector, VRC.Udon.Serialization.OdinSerializer.OdinSerialize]
        private string[][] _localizedTexts = { };

        [SerializeField, HideInInspector] private Text[] _boundTexts = { };
        [SerializeField, HideInInspector] private TextMeshProUGUI[] _boundTmpTexts = { };
        [SerializeField, HideInInspector] private int[] _boundKeyIndices = { };

        /// <summary>
        /// 現在の言語の番号(Project Settings > VGC > Localization の並び順)。
        /// エディタで再生していないときは、エディタの表示言語の番号を返す(プレビュー用)。
        /// </summary>
        protected int CurrentLanguageIndex
        {
            get
            {
#if !COMPILER_UDONSHARP && UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying)
                    return EditorLanguageIndex();
#endif
                if (_localizationSystem == null)
                    return 0;
                return _localizationSystem.CurrentIndex;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private static int EditorLanguageIndex()
        {
            var languages = VGC.Localization.LanguageSetting.Available;
            var current = VGC.Localization.LanguageSetting.Current;
            for (var i = 0; i < languages.Count; i++)
            {
                if (languages[i] == current)
                    return i;
            }

            return 0;
        }
#endif

        /// <summary>キーの文字列(現在の言語)。キーが無ければキーをそのまま返す。</summary>
        public string _Get(string key)
        {
            for (var i = 0; i < _localizedKeys.Length; i++)
            {
                if (_localizedKeys[i] == key)
                    return _GetByIndex(i);
            }

            return key;
        }

        /// <summary>キーの番号で文字列を引く(現在の言語)。</summary>
        public string _GetByIndex(int keyIndex)
        {
            var language = CurrentLanguageIndex;
            if (language < 0 || language >= _localizedTexts.Length)
                language = 0;
            if (_localizedTexts.Length == 0 || keyIndex < 0 || keyIndex >= _localizedTexts[language].Length)
                return keyIndex >= 0 && keyIndex < _localizedKeys.Length ? _localizedKeys[keyIndex] : "";
            return _localizedTexts[language][keyIndex];
        }

        /// <summary>言語が変わったときに LocalizationSystem から呼ばれる。override するときは base を呼ぶ。</summary>
        public virtual void _OnLanguageChanged()
        {
            ApplyBindings();
        }

        protected void ApplyBindings()
        {
            for (var i = 0; i < _boundKeyIndices.Length; i++)
            {
                var text = _GetByIndex(_boundKeyIndices[i]);
                if (_boundTexts[i] != null)
                    _boundTexts[i].text = text;
                if (_boundTmpTexts[i] != null)
                    _boundTmpTexts[i].text = text;
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        /// <summary>エディタの焼き込み処理から呼ぶ。</summary>
        public void EditorApplyBake(LocalizationSystem system, string[] keys, string[][] texts,
                                    Text[] boundTexts, TextMeshProUGUI[] boundTmpTexts, int[] boundKeyIndices)
        {
            _localizationSystem = system;
            _localizedKeys = keys;
            _localizedTexts = texts;
            _boundTexts = boundTexts;
            _boundTmpTexts = boundTmpTexts;
            _boundKeyIndices = boundKeyIndices;
        }
#endif
    }
}
