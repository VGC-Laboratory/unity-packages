using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VGC.Localization.Import;

namespace VGC.Localization
{
    [CustomEditor(typeof(LocalizationTable))]
    public class LocalizationTableEditor : Editor
    {
        private TreeViewState _state;
        private LocalizationTableView _view;
        private Vector2 _scroll;
        private bool _importing;
        private string _message;
        private MessageType _messageType;

        private SerializedProperty _source;
        private SerializedProperty _sheetName;
        private SerializedProperty _defaultLanguage;

        private void OnEnable()
        {
            _source = serializedObject.FindProperty("_source");
            _sheetName = serializedObject.FindProperty("_sheetName");
            _defaultLanguage = serializedObject.FindProperty("_defaultLanguage");
            RebuildView();
        }

        private void RebuildView()
        {
            var table = (LocalizationTable)target;
            _state ??= new TreeViewState();
            var header = new MultiColumnHeader(CreateHeader(table));
            _view = new LocalizationTableView(_state, header, table);
        }

        public override void OnInspectorGUI()
        {
            var table = (LocalizationTable)target;

            serializedObject.Update();
            EditorGUILayout.PropertyField(_source);
            EditorGUILayout.PropertyField(_sheetName);
            EditorGUILayout.PropertyField(_defaultLanguage);
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_importing || table.Source == null || !table.Source.IsValid ||
                                                   string.IsNullOrEmpty(table.SheetName)))
                {
                    if (GUILayout.Button(_importing ? "Importing..." : "Import", GUILayout.Height(24)))
                        Import(table);
                }

                var editUrl = table.Source != null ? table.Source.BuildEditUrl() : null;
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(editUrl)))
                {
                    if (GUILayout.Button("Open Sheet", GUILayout.Width(100), GUILayout.Height(24)))
                        Application.OpenURL(editUrl);
                }
            }

            if (!string.IsNullOrEmpty(_message))
                EditorGUILayout.HelpBox(_message, _messageType);

            EditorGUILayout.Space();
            LocalizeUIHelper.InspectorGUI();

            var rect = GUILayoutUtility.GetRect(0, 10000, 0, 300);
            _view.OnGUI(rect);

            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDetail();
            EditorGUILayout.EndScrollView();
        }

        private void Import(LocalizationTable table)
        {
            _importing = true;
            _message = null;
            LocalizationTableImporter.Import(table, (success, message) =>
            {
                _importing = false;
                _message = message;
                _messageType = success ? MessageType.Info : MessageType.Error;
                if (success)
                    RebuildView();
                Repaint();
            });
        }

        private void DrawDetail()
        {
            var entry = _view.SelectedEntry;

            if (entry == null)
            {
                EditorGUILayout.HelpBox("Select a row", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("ID", entry.Id);
            EditorGUILayout.Space();

            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            var width = EditorGUIUtility.currentViewWidth - 40;

            foreach (var val in entry.Values)
            {
                EditorGUILayout.LabelField(LabelOf(val.Language));

                var normalized = Normalize(val.Text);
                var height = Mathf.Max(40, style.CalcHeight(new GUIContent(normalized), width));
                EditorGUILayout.TextArea(normalized, style, GUILayout.Height(height));
                EditorGUILayout.Space();
            }
        }

        private static string LabelOf(string language)
        {
            return LanguageNames.TryParse(language, out var parsed)
                ? $"{LanguageNames.NativeName(parsed)} ({language})"
                : language;
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            return s.Replace("\\n", "\n")
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");
        }

        private static MultiColumnHeaderState CreateHeader(LocalizationTable table)
        {
            var languages = LocalizationTableView.GetLanguages(table);
            var columns = new MultiColumnHeaderState.Column[languages.Length + 1];

            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("ID"),
                width = 200,
                autoResize = true
            };

            for (var i = 0; i < languages.Length; i++)
            {
                columns[i + 1] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(LabelOf(languages[i])),
                    width = 150,
                    autoResize = true
                };
            }

            return new MultiColumnHeaderState(columns);
        }
    }
}
