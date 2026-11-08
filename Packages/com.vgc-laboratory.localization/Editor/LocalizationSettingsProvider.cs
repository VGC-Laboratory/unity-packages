using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace VGC.Localization
{
    /// <summary>Project Settings > VGC > Localization。使う言語の一覧と既定言語を設定する。</summary>
    internal class LocalizationSettingsProvider : SettingsProvider
    {
        private ReorderableList _list;
        private List<SystemLanguage> _languages;

        private LocalizationSettingsProvider()
            : base("Project/VGC/Localization", SettingsScope.Project, new[] { "VGC", "Localization", "Language" })
        {
        }

        [SettingsProvider]
        public static SettingsProvider Create() => new LocalizationSettingsProvider();

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            _languages = LocalizationProjectSettings.instance.Languages.ToList();
            _list = new ReorderableList(_languages, typeof(SystemLanguage), true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Languages"),
                drawElementCallback = (rect, index, _, _) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += 2;
                    EditorGUI.BeginChangeCheck();
                    var value = (SystemLanguage)EditorGUI.EnumPopup(rect, _languages[index]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _languages[index] = value;
                        Save();
                    }
                },
                onAddCallback = list =>
                {
                    var unused = System.Enum.GetValues(typeof(SystemLanguage))
                                       .Cast<SystemLanguage>()
                                       .FirstOrDefault(l => !_languages.Contains(l));
                    _languages.Add(unused);
                    Save();
                },
                onRemoveCallback = list =>
                {
                    _languages.RemoveAt(list.index);
                    Save();
                },
                onReorderCallback = _ => Save(),
            };
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.HelpBox(
                "Languages used by VGC localization tables in this project. " +
                "The order is the order shown in language menus (and the index used by world-side localization).",
                MessageType.None);

            _list.DoLayoutList();

            var settings = LocalizationProjectSettings.instance;
            var names = _languages.Select(LanguageNames.NativeName).ToArray();
            var index = Mathf.Max(0, _languages.IndexOf(settings.DefaultLanguage));

            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("Default Language", index, names);
            if (EditorGUI.EndChangeCheck() && index < _languages.Count)
                settings.SetDefaultLanguage(_languages[index]);

            EditorGUILayout.Space();
            LocalizeUIHelper.InspectorGUI();
        }

        private void Save()
        {
            LocalizationProjectSettings.instance.SetLanguages(_languages);
            _languages = LocalizationProjectSettings.instance.Languages.ToList();
            _list.list = _languages;
        }
    }
}
