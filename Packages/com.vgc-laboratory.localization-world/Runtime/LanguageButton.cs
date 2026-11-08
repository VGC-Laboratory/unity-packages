using TMPro;
using UdonSharp;
using UnityEngine;

namespace VGC.Localization.World
{
    /// <summary>
    /// 押すと決まった言語に切り替えるボタン。Button の OnClick から _Select を呼ぶ。
    /// 言語の番号は Project Settings > VGC > Localization の並び順。-1 なら押すたびに次の言語へ進む。
    /// ラベルを指定すると、その言語の自称(日本語、English など)を表示する。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LanguageButton : LocalizedUdonBehaviour
    {
        [SerializeField] private int _languageIndex = -1;
        [SerializeField] private TextMeshProUGUI _label;

        public void _Select()
        {
            if (_localizationSystem == null)
                return;

            if (_languageIndex < 0)
                _localizationSystem._NextLanguage();
            else
                _localizationSystem._SetLanguageIndex(_languageIndex);
        }

        public override void _OnLanguageChanged()
        {
            base._OnLanguageChanged();

            if (_label == null || _localizationSystem == null)
                return;

            var index = _languageIndex < 0 ? _localizationSystem.CurrentIndex : _languageIndex;
            _label.text = _localizationSystem._GetLanguageName(index);
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        public int EditorLanguageIndex
        {
            get => _languageIndex;
            set => _languageIndex = value;
        }

        public TextMeshProUGUI EditorLabel
        {
            get => _label;
            set => _label = value;
        }
#endif
    }
}
