using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace VGC.Gimmicks_MA.Lib.Localization
{
    public class LocalizationTableView : TreeView
    {
        private LocalizationTable _table;
        private string[] _languages;
        
        public Entry SelectedEntry { get; private set; }

        public LocalizationTableView(TreeViewState state, MultiColumnHeader header, LocalizationTable table)
            : base(state, header)
        {
            _table = table;
            _languages = GetLanguages(table);
            Reload();
        }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0)
            {
                SelectedEntry = null;
                return;
            }

            int index = selectedIds[0] - 1;

            if (index >= 0 && index < _table.Entries.Length)
            {
                SelectedEntry = _table.Entries[index];
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1 };
            var rows = new List<TreeViewItem>();

            int id = 1;

            foreach (var entry in _table.Entries)
            {
                rows.Add(new TreeViewItem
                {
                    id = id++,
                    depth = 0,
                    displayName = entry.Id
                });
            }

            SetupParentsAndChildrenFromDepths(root, rows);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var entry = _table.Entries[args.item.id - 1];

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                var rect = args.GetCellRect(i);
                int column = args.GetColumn(i);

                switch (column)
                {
                    case 0:
                        EditorGUI.LabelField(rect, entry.Id);
                        break;

                    default:
                        string lang = _languages[column - 1];
                        string text = GetText(entry, lang);
                        EditorGUI.LabelField(rect, text);
                        break;
                }
            }
        }

        private string GetText(Entry entry, string lang)
        {
            foreach (var v in entry.Values)
            {
                if (v.Language == lang)
                    return v.Text;
            }
            return "";
        }

        private string[] GetLanguages(LocalizationTable table)
        {
            var set = new HashSet<string>();

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