using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VGC.Gimmicks_MA.Lib.Localization
{
    [CustomEditor(typeof(LocalizationTable))]
    public class LocalizationTableEditor : Editor
    {
        private TreeViewState _state;
        private MultiColumnHeader _header;
        private LocalizationTableView _view;
        private Vector2 _scroll;

        private void OnEnable()
        {
            var table = (LocalizationTable)target;

            _state ??= new TreeViewState();

            var headerState = CreateHeader(table);
            _header = new MultiColumnHeader(headerState);

            _view = new LocalizationTableView(_state, _header, table);
        }

        public override void OnInspectorGUI()
        {
            var rect = GUILayoutUtility.GetRect(0, 10000, 0, 300);
            _view.OnGUI(rect);

            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawDetail();
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawDetail()
        {
            var entry = _view.SelectedEntry;

            if (entry == null)
            {
                EditorGUILayout.HelpBox("Select a row", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Detail", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // ID
            EditorGUILayout.LabelField("ID", entry.Id);

            EditorGUILayout.Space();

            foreach (var val in entry.Values)
            {
                EditorGUILayout.LabelField(val.Language);

                var text = val.Text;
                
                var style = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true
                };

                var normalized = Normalize(text);

                float width = EditorGUIUtility.currentViewWidth - 40;

                float height = style.CalcHeight(
                    new GUIContent(normalized),
                    width
                );

                height = Mathf.Max(40, height);

                EditorGUILayout.TextArea(
                    normalized,
                    style,
                    GUILayout.Height(height)
                );
                
                EditorGUILayout.Space();
            }
        }
        
        string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            return s
                   .Replace("\\n", "\n")
                   .Replace("\r\n", "\n")
                   .Replace("\r", "\n");
        }

        private MultiColumnHeaderState CreateHeader(LocalizationTable table)
        {
            var languages = GetLanguages(table);

            var columns = new MultiColumnHeaderState.Column[languages.Length + 1];

            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("ID"),
                width = 200,
                autoResize = true
            };

            for (int i = 0; i < languages.Length; i++)
            {
                columns[i + 1] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(languages[i]),
                    width = 150,
                    autoResize = true
                };
            }

            return new MultiColumnHeaderState(columns);
        }

        private string[] GetLanguages(LocalizationTable table)
        {
            var set = new System.Collections.Generic.HashSet<string>();

            foreach (var e in table.Entries)
            {
                foreach (var v in e.Values)
                {
                    set.Add(v.Language);
                }
            }

            var arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }
    }
}