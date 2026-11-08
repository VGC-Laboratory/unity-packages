using System;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace VGC.Localization.World
{
    /// <summary>
    /// LocalizedUdonBehaviour に焼き込む翻訳の指定。同じ GameObject に付ける。ビルドには含まれない(IEditorOnly)。
    ///
    /// Tables のすべてのキーが焼き込まれ、コードから _Get(key) で引ける(同じキーは後ろの表が優先)。
    /// Bindings に「文字欄とキー」を並べると、言語が変わったときに自動で書き換わる。
    /// </summary>
    [AddComponentMenu("VGC/Localization/Localization Bindings")]
    [DisallowMultipleComponent]
    public class LocalizationBindings : MonoBehaviour, IEditorOnly
    {
        [Serializable]
        public class Binding
        {
            [Tooltip("Text か TextMeshProUGUI")]
            public MaskableGraphic target;
            public string key;

            public Binding() { }

            public Binding(MaskableGraphic target, string key)
            {
                this.target = target;
                this.key = key;
            }
        }

        [SerializeField] private LocalizationTable[] _tables = Array.Empty<LocalizationTable>();
        [SerializeField] private Binding[] _bindings = Array.Empty<Binding>();

        public LocalizationTable[] Tables
        {
            get => _tables ?? Array.Empty<LocalizationTable>();
            set => _tables = value;
        }

        public Binding[] Bindings
        {
            get => _bindings ?? Array.Empty<Binding>();
            set => _bindings = value;
        }
    }
}
