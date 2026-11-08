using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VGC.Localization.Import
{
    /// <summary>
    /// スプレッドシートから翻訳表(LocalizationTable)に取り込む。
    ///
    /// シートの形: 1 行目は見出し。A 列はキー、B 列以降の見出しは SystemLanguage の名前(English, Japanese ...)。
    /// 言語として読めない見出しの列(メモ欄など)は読み飛ばす。キーが空の行も読み飛ばす。
    /// </summary>
    public static class LocalizationTableImporter
    {
        public static void Import(LocalizationTable table, Action<bool, string> onComplete = null)
        {
            if (table == null)
            {
                onComplete?.Invoke(false, "Table is null.");
                return;
            }

            if (string.IsNullOrEmpty(table.SheetName))
            {
                onComplete?.Invoke(false, "Sheet name is empty.");
                return;
            }

            SpreadsheetReader.ReadRows(table.Source, table.SheetName, rows =>
            {
                if (table == null)
                    return;

                var entries = ToEntries(rows, out var message);
                if (entries == null)
                {
                    onComplete?.Invoke(false, message);
                    return;
                }

                Undo.RecordObject(table, "Import Localization Table");
                table.SetEntries(table.SheetName, entries);
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssetIfDirty(table);
                onComplete?.Invoke(true, message);
            }, error => onComplete?.Invoke(false, error));
        }

        /// <summary>行の配列を翻訳の項目にする。見出しが不正なら null。</summary>
        public static Entry[] ToEntries(IReadOnlyList<string[]> rows, out string message)
        {
            message = null;
            if (rows == null || rows.Count == 0)
            {
                message = "The sheet is empty.";
                return null;
            }

            var header = rows[0];
            var columns = new List<(int index, SystemLanguage language)>();
            var skipped = new List<string>();

            for (var i = 1; i < header.Length; i++)
            {
                if (LanguageNames.TryParse(header[i], out var language))
                    columns.Add((i, language));
                else if (!string.IsNullOrWhiteSpace(header[i]))
                    skipped.Add(header[i]);
            }

            if (columns.Count == 0)
            {
                message = "No language column found. Row 1 must be: key, English, Japanese, ...";
                return null;
            }

            var entries = new List<Entry>();
            var seen = new HashSet<string>();

            for (var r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                var id = row.Length > 0 ? row[0]?.Trim() : null;
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;

                var values = new List<LangValue>();
                foreach (var (index, language) in columns)
                {
                    var text = index < row.Length ? row[index] : null;
                    if (!string.IsNullOrEmpty(text))
                        values.Add(new LangValue(language, text));
                }

                entries.Add(new Entry(id, values.ToArray()));
            }

            message = $"{entries.Count} keys, {columns.Count} languages";
            if (skipped.Count > 0)
                message += $" (skipped columns: {string.Join(", ", skipped)})";
            return entries.ToArray();
        }
    }
}
