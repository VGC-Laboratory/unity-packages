using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace VGC.Localization.Import
{
    /// <summary>
    /// スプレッドシートの 1 シートを「行の配列」として読む。1 行目は見出し。
    ///
    /// 取得元が gviz の CSV でも Apps Script の JSON でも、同じ形(string[][])にそろえて返す。
    /// 呼び出し側は取得方法を気にせず、見出しと行だけを見ればよい。
    /// </summary>
    public static class SpreadsheetReader
    {
        public static void ReadRows(SpreadsheetSource source, string sheetName,
                                    Action<List<string[]>> onSuccess, Action<string> onError)
        {
            if (source == null || !source.IsValid)
            {
                onError?.Invoke("Spreadsheet source is not set.");
                return;
            }

            var url = source.BuildUrl(sheetName);
            Get(url, text =>
            {
                try
                {
                    var rows = source.Kind == SpreadsheetSourceKind.AppsScript
                        ? ParseAppsScript(text)
                        : CsvParser.Parse(text);
                    onSuccess?.Invoke(rows);
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                }
            }, onError);
        }

        /// <summary>
        /// GET してメインスレッドで結果を返す。UniTask などに依存しないよう、EditorApplication.update で完了を待つ。
        /// </summary>
        public static void Get(string url, Action<string> onSuccess, Action<string> onError)
        {
            var request = UnityWebRequest.Get(url);
            request.SendWebRequest();

            void Poll()
            {
                if (!request.isDone)
                    return;

                EditorApplication.update -= Poll;
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                        onSuccess?.Invoke(request.downloadHandler.text);
                    else
                        onError?.Invoke($"{request.error} ({url})");
                }
                finally
                {
                    request.Dispose();
                }
            }

            EditorApplication.update += Poll;
        }

        // ===== Apps Script の応答 =====

        [Serializable]
        private class ErrorResponse
        {
            public string error;
        }

        [Serializable]
        private class TableResponse
        {
            public Entry[] entries;
        }

        [Serializable]
        private class VersionItem
        {
            public string sheetName;
            public string version;
            public string url;
        }

        [Serializable]
        private class VersionResponse
        {
            public VersionItem[] items;
        }

        /// <summary>
        /// Apps Script の応答を行の配列にする。対応する形は 2 つ。
        ///   翻訳: [{id, values:[{language, text}]}]   → [key, 言語1, 言語2, ...]
        ///   版  : {items:[{sheetName, version, url}]} → [key, version, url]
        /// </summary>
        private static List<string[]> ParseAppsScript(string json)
        {
            json = json?.Trim() ?? "";

            if (json.StartsWith("["))
            {
                // JsonUtility は先頭が配列の JSON を読めないので包む
                var table = JsonUtility.FromJson<TableResponse>("{\"entries\":" + json + "}");
                return FromEntries(table?.entries ?? Array.Empty<Entry>());
            }

            var error = JsonUtility.FromJson<ErrorResponse>(json);
            if (!string.IsNullOrEmpty(error?.error))
                throw new Exception(error.error);

            var versions = JsonUtility.FromJson<VersionResponse>(json);
            if (versions?.items != null)
            {
                var rows = new List<string[]> { new[] { "key", "version", "url" } };
                rows.AddRange(versions.items
                                      .Where(i => i != null && !string.IsNullOrEmpty(i.sheetName))
                                      .Select(i => new[] { i.sheetName, i.version ?? "", i.url ?? "" }));
                return rows;
            }

            throw new Exception("Unknown Apps Script response.");
        }

        private static List<string[]> FromEntries(Entry[] entries)
        {
            var languages = entries.SelectMany(e => e.Values)
                                   .Select(v => v.Language)
                                   .Where(l => !string.IsNullOrEmpty(l))
                                   .Distinct()
                                   .ToList();

            var rows = new List<string[]> { new[] { "key" }.Concat(languages).ToArray() };
            foreach (var e in entries)
            {
                var row = new string[languages.Count + 1];
                row[0] = e.Id;
                foreach (var v in e.Values)
                {
                    var index = languages.IndexOf(v.Language);
                    if (index >= 0)
                        row[index + 1] = v.Text;
                }

                rows.Add(row);
            }

            return rows;
        }
    }
}
