using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VGC.Localization.World
{
    /// <summary>
    /// ワールド内の表示言語を管理する。シーンに 1 つ置く。
    ///
    /// 言語の一覧は Project Settings > VGC > Localization の並び順で、ビルド時に焼き込まれる。
    /// 起動時は VRChat 本体の表示言語に合わせ、利用者が <see cref="_SetLanguageIndex"/> で選んだ後はそちらを優先する。
    /// 言語が変わると、焼き込まれた LocalizedUdonBehaviour すべての _OnLanguageChanged を呼ぶ。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LocalizationSystem : UdonSharpBehaviour
    {
        [Tooltip("VRChat 本体の表示言語に合わせる(利用者が選び直すまで)")]
        [SerializeField] private bool _followVRChatLanguage = true;

        // ===== ビルド時に焼き込む =====
        [SerializeField, HideInInspector] private string[] _languageNames = { "English" };
        [SerializeField, HideInInspector] private string[] _languageCodes = { "en" };
        [SerializeField, HideInInspector] private int[] _codeLanguageIndices = { 0 };
        [SerializeField, HideInInspector] private int _defaultIndex;
        [SerializeField, HideInInspector] private LocalizedUdonBehaviour[] _listeners = { };

        private int _currentIndex;
        private bool _selectedByUser;
        private bool _initialized;

        public int LanguageCount => _languageNames.Length;
        public int CurrentIndex => _currentIndex;
        public int DefaultIndex => _defaultIndex;

        private void Start()
        {
            _Initialize();
        }

        /// <summary>まだなら初期化する。他の Start から先に呼ばれてもよい。</summary>
        public void _Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            _currentIndex = _followVRChatLanguage ? FindIndex(VRCPlayerApi.GetCurrentLanguage()) : _defaultIndex;
            Notify();
        }

        public override void OnLanguageChanged(string language)
        {
            if (!_followVRChatLanguage || _selectedByUser)
                return;

            Apply(FindIndex(language));
        }

        /// <summary>利用者が言語を選ぶ。以後は VRChat 本体の言語が変わっても追従しない。</summary>
        public void _SetLanguageIndex(int index)
        {
            _selectedByUser = true;
            Apply(index);
        }

        /// <summary>
        /// VRChat の言語コード(ja, en, ko, zh-CN ...)で言語を選ぶ。一覧に無ければ何もしない。
        /// 固定の言語ボタンから呼ぶときに使う。
        /// </summary>
        public void _SetLanguageCode(string code)
        {
            var index = _FindLanguageIndex(code);
            if (index >= 0)
                _SetLanguageIndex(index);
        }

        /// <summary>言語コードから言語の番号を探す。一覧に無ければ -1。</summary>
        public int _FindLanguageIndex(string code)
        {
            if (string.IsNullOrEmpty(code))
                return -1;

            for (var i = 0; i < _languageCodes.Length; i++)
            {
                if (_languageCodes[i] == code || Head(_languageCodes[i]) == code)
                    return _codeLanguageIndices[i];
            }

            return -1;
        }

        /// <summary>次の言語へ切り替える(最後なら最初へ)。</summary>
        public void _NextLanguage()
        {
            _SetLanguageIndex((_currentIndex + 1) % _languageNames.Length);
        }

        /// <summary>言語の自称(日本語、English など)。</summary>
        public string _GetLanguageName(int index)
        {
            if (index < 0 || index >= _languageNames.Length)
                return "";
            return _languageNames[index];
        }

        /// <summary>実行中に生成したオブジェクトを通知先に加える。</summary>
        public void _AddListener(LocalizedUdonBehaviour listener)
        {
            if (listener == null)
                return;

            for (var i = 0; i < _listeners.Length; i++)
            {
                if (_listeners[i] == listener)
                    return;
            }

            var next = new LocalizedUdonBehaviour[_listeners.Length + 1];
            for (var i = 0; i < _listeners.Length; i++)
                next[i] = _listeners[i];
            next[_listeners.Length] = listener;
            _listeners = next;

            if (_initialized)
                listener._OnLanguageChanged();
        }

        private void Apply(int index)
        {
            if (index < 0 || index >= _languageNames.Length)
                return;

            _currentIndex = index;
            Notify();
        }

        private void Notify()
        {
            for (var i = 0; i < _listeners.Length; i++)
            {
                if (_listeners[i] != null)
                    _listeners[i]._OnLanguageChanged();
            }
        }

        /// <summary>
        /// VRChat の言語コード(en, ja, zh-CN ...)から言語の番号を探す。
        /// 完全一致 → ハイフンより前の一致 → 既定言語 の順。
        /// </summary>
        private int FindIndex(string code)
        {
            if (string.IsNullOrEmpty(code))
                return _defaultIndex;

            for (var i = 0; i < _languageCodes.Length; i++)
            {
                if (_languageCodes[i] == code)
                    return _codeLanguageIndices[i];
            }

            var head = Head(code);
            for (var i = 0; i < _languageCodes.Length; i++)
            {
                if (Head(_languageCodes[i]) == head)
                    return _codeLanguageIndices[i];
            }

            return _defaultIndex;
        }

        private string Head(string code)
        {
            var hyphen = code.IndexOf('-');
            return hyphen < 0 ? code : code.Substring(0, hyphen);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        /// <summary>
        /// エディタの焼き込み処理から呼ぶ。languageCodes[i] は codeLanguageIndices[i] 番の言語に対応する
        /// (1 つの言語に複数のコードがあってよい。zh-HK と zh-TW など)。
        /// </summary>
        public void EditorApplyBake(string[] languageNames, string[] languageCodes, int[] codeLanguageIndices,
                                    int defaultIndex, LocalizedUdonBehaviour[] listeners)
        {
            _languageNames = languageNames;
            _languageCodes = languageCodes;
            _codeLanguageIndices = codeLanguageIndices;
            _defaultIndex = defaultIndex;
            _listeners = listeners;
        }
#endif
    }
}
