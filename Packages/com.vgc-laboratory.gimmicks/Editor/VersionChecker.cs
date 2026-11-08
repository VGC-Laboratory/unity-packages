using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VGC.Localization.Import;

namespace VGC.Gimmicks
{
    /// <summary>
    /// 取得元(VersionSource)ごとに最新版の一覧を読み、ローカルの版と比べる。
    ///
    /// 取得結果と取得時刻は取得元ごとに EditorPrefs に残し、<see cref="CacheLifetime"/> の間は取り直さない
    /// (インスペクタ描画やスクリプトの再読み込みのたびに取りに行くと、Apps Script の実行回数の上限に近づくため)。
    /// 取得は最初に問い合わせがあったときに始まる。
    /// </summary>
    public static class VersionChecker
    {
        /// <summary>
        /// VGC のギミックの版を返す Apps Script(読み取り専用デプロイ)。
        /// Config/VGCSpreadsheet.asset と同じ URL。コードから組み立てたいとき用に公開している。
        /// </summary>
        public const string VGCAppsScriptUrl =
            "https://script.google.com/macros/s/AKfycby0cK2uzLT3rXKNk2ufW6CjBwIugqBTWqtG-yC1AShn3msKXk-rtLmnaoAZwIBWwGc2zg/exec";

        /// <summary>この時間を過ぎていなければ取り直さない。</summary>
        public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

        private const string CachePrefix = "VGC.Gimmicks.VersionCache.";

        private class Info
        {
            public string Version;
            public string Url;
        }

        private class State
        {
            public readonly Dictionary<string, Info> Items = new();
            public bool Loaded;
            public bool Fetching;
        }

        private static readonly Dictionary<string, State> States = new();

        public static bool IsFetching(VersionSource source) => source != null && GetState(source).Fetching;

        /// <summary>最後に取得してからの経過時間。取得したことが無ければ null。</summary>
        public static TimeSpan? SinceLastFetch(VersionSource source)
        {
            if (source == null)
                return null;

            var raw = EditorPrefs.GetString(TimeKey(source), "");
            return long.TryParse(raw, out var ticks)
                ? DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)
                : (TimeSpan?)null;
        }

        /// <summary>
        /// 版の一覧を取り直す。force が false なら、キャッシュが新しいうちは何もしない。
        /// 利用者が明示的に押したときは force を true にする。
        /// </summary>
        public static void Fetch(VersionSource source, bool force = false)
        {
            if (source == null || !source.IsValid)
                return;

            var state = GetState(source);
            if (state.Fetching)
                return;

            if (!force && !IsStale(source))
                return;

            state.Fetching = true;
            SpreadsheetReader.ReadRows(source.Source, source.SheetName, rows =>
            {
                state.Fetching = false;
                Apply(state, rows);
                EditorPrefs.SetString(DataKey(source), Serialize(state));
                EditorPrefs.SetString(TimeKey(source), DateTime.UtcNow.Ticks.ToString());
                InternalEditorUtility.RepaintAllViews();
            }, error =>
            {
                state.Fetching = false;
                Debug.LogWarning($"[VGC.Gimmicks] Failed to fetch versions: {error}");
                // 失敗しても時刻は記録し、描画のたびに取り直さないようにする
                EditorPrefs.SetString(TimeKey(source), DateTime.UtcNow.Ticks.ToString());
            });
        }

        /// <summary>シート上の版がローカルより新しいか。まだ取得していなければ取得を始めて false を返す。</summary>
        public static bool HasUpdate(VersionSource source, string key, string localVersion, out string remoteVersion)
        {
            remoteVersion = null;
            if (source == null || string.IsNullOrEmpty(key))
                return false;

            Fetch(source);

            if (!GetState(source).Items.TryGetValue(key, out var info))
                return false;

            remoteVersion = info.Version;
            return CompareVersion(info.Version, localVersion) > 0;
        }

        /// <summary>シート上の最新版。無ければ null。</summary>
        public static string GetVersion(VersionSource source, string key)
        {
            return source != null && key != null && GetState(source).Items.TryGetValue(key, out var info) ? info.Version : null;
        }

        /// <summary>シート上のダウンロード URL。無ければ null。</summary>
        public static string GetUrl(VersionSource source, string key)
        {
            return source != null && key != null && GetState(source).Items.TryGetValue(key, out var info) ? info.Url : null;
        }

        /// <summary>
        /// "1.2.3" 形式を比べる。"1.0.0-beta" や空文字でも例外にせず、数値として読めない部分は 0 として扱う
        /// (インスペクタ描画中に呼ばれるため)。"-" や "+" 以降は比べない。
        /// </summary>
        public static int CompareVersion(string a, string b)
        {
            var pa = SplitVersion(a);
            var pb = SplitVersion(b);

            for (var i = 0; i < Mathf.Max(pa.Length, pb.Length); i++)
            {
                var va = i < pa.Length ? pa[i] : 0;
                var vb = i < pb.Length ? pb[i] : 0;

                if (va != vb)
                    return va.CompareTo(vb);
            }

            return 0;
        }

        private static int[] SplitVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return Array.Empty<int>();

            var core = version.Trim().TrimStart('v', 'V').Split('-', '+')[0];
            var parts = core.Split('.');
            var result = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                int.TryParse(parts[i], out result[i]);

            return result;
        }

        // ===== 内部 =====

        private static State GetState(VersionSource source)
        {
            var key = source.CacheKey;
            if (!States.TryGetValue(key, out var state))
                States[key] = state = new State();

            if (!state.Loaded)
            {
                state.Loaded = true;
                // まずキャッシュから戻す。これをしないと、取得を省いたときに中身が空のまま「更新なし」と表示してしまう
                Deserialize(state, EditorPrefs.GetString(DataKey(source), ""));
            }

            return state;
        }

        private static bool IsStale(VersionSource source)
        {
            var since = SinceLastFetch(source);
            return since == null || since.Value >= CacheLifetime;
        }

        private static void Apply(State state, List<string[]> rows)
        {
            state.Items.Clear();
            // 1 行目は見出し
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Length < 2 || string.IsNullOrWhiteSpace(row[0]))
                    continue;

                state.Items[row[0].Trim()] = new Info
                {
                    Version = row[1]?.Trim(),
                    Url = row.Length > 2 ? row[2]?.Trim() : null,
                };
            }
        }

        private static string Serialize(State state)
        {
            var sb = new StringBuilder();
            foreach (var pair in state.Items)
                sb.Append(pair.Key).Append('\t').Append(pair.Value.Version).Append('\t').Append(pair.Value.Url).Append('\n');
            return sb.ToString();
        }

        private static void Deserialize(State state, string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            foreach (var line in data.Split('\n'))
            {
                var cells = line.Split('\t');
                if (cells.Length >= 2 && !string.IsNullOrEmpty(cells[0]))
                    state.Items[cells[0]] = new Info { Version = cells[1], Url = cells.Length > 2 ? cells[2] : null };
            }
        }

        private static string DataKey(VersionSource source) => CachePrefix + Hash(source.CacheKey);
        private static string TimeKey(VersionSource source) => CachePrefix + Hash(source.CacheKey) + ".Time";

        private static string Hash(string value)
        {
            return Hash128.Compute(value ?? "").ToString();
        }
    }
}
