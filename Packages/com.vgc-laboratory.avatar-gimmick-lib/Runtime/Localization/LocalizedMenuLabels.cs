using UnityEngine;
using VRC.SDKBase;

namespace VGC.Gimmicks_MA.Lib.Localization
{
    /// <summary>
    /// 子孫の MA Menu Item のラベルを、ビルド時に LocalizationTable の訳へ差し替える。
    /// キーは「keyPrefix + GameObject名」。訳が無ければ MA の通常どおり GameObject 名(またはlabel)が表示される。
    /// 言語ごとにメニュー資産を複製する代わりに使う(言語はビルドする人のエディタ言語)。
    /// </summary>
    [AddComponentMenu("VGC/VGC Localized Menu Labels")]
    public class LocalizedMenuLabels : LocalizeConfigBehaviour, IEditorOnly
    {
        [SerializeField] private LocalizationTable _table;
        [SerializeField] private string _keyPrefix = "Menu.";

        public LocalizationTable Table => _table;
        public string KeyPrefix => _keyPrefix;
    }
}
