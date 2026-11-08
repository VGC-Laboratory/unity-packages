using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace VGC.Gimmicks
{
    [System.Serializable]
    public class GimmickVersionInfo
    {
        public string sheetName;
        public string version;
        public string url;
    }

    [System.Serializable]
    public class GimmickVersionResponse
    {
        public GimmickVersionInfo[] items;
    }
    
    [InitializeOnLoad]
    public static class GimmickVersionManager
    {
        // 開発用ツール(GimmickVersionPublisher)からも参照する
        public const string URL ="https://script.google.com/macros/s/AKfycby0cK2uzLT3rXKNk2ufW6CjBwIugqBTWqtG-yC1AShn3msKXk-rtLmnaoAZwIBWwGc2zg/exec";

        // 取得結果と取得時刻を EditorPrefs に残し、一定時間は取り直さない。
        // [InitializeOnLoad] なのでスクリプトが再読み込みされるたびに走り、
        // そのまま毎回 GET すると Apps Script の実行回数の上限に近づくため。
        private const string CacheKey = "VGC.Gimmicks.GimmickVersionCache";
        private const string CacheTimeKey = "VGC.Gimmicks.GimmickVersionCacheTime";

        /// <summary>この時間を過ぎていなければ取り直さない。</summary>
        public static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);

        private static readonly Dictionary<string, GimmickVersionInfo> _remote = new();
        private static bool _isFetching;

        public static bool IsFetching => _isFetching;

        private static UniTask _currentTask;

        static GimmickVersionManager()
        {
            // まずキャッシュから戻す。これをしないと、取得を省いたときに
            // 中身が空のまま「更新なし」と表示してしまう
            LoadCache();

            if (IsCacheStale())
                Fetch().Forget(Debug.LogException);
        }

        /// <summary>
        /// 版の一覧を取り直す。
        /// </summary>
        /// <param name="force">
        /// true なら、キャッシュが新しくても取り直す。利用者が明示的に押したときに使う。
        /// </param>
        public static UniTask Fetch(bool force = false)
        {
            if (_isFetching)
                return _currentTask;

            if (!force && !IsCacheStale())
                return UniTask.CompletedTask;

            _isFetching = true;
            return _currentTask = FetchInternal();
        }

        /// <summary>最後に取得してからの経過時間。取得したことが無ければ null。</summary>
        public static TimeSpan? SinceLastFetch
        {
            get
            {
                var at = LastFetchedAt;
                return at == null ? (TimeSpan?)null : DateTime.UtcNow - at.Value;
            }
        }

        private static DateTime? LastFetchedAt
        {
            get
            {
                var raw = EditorPrefs.GetString(CacheTimeKey, "");
                return long.TryParse(raw, out var ticks)
                    ? new DateTime(ticks, DateTimeKind.Utc)
                    : (DateTime?)null;
            }
        }

        private static bool IsCacheStale()
        {
            var since = SinceLastFetch;
            return since == null || since.Value >= CacheLifetime;
        }

        private static void LoadCache()
        {
            var json = EditorPrefs.GetString(CacheKey, "");
            if (!string.IsNullOrEmpty(json))
                Apply(json);
        }

        private static void Apply(string json)
        {
            GimmickVersionResponse data;
            try
            {
                data = JsonUtility.FromJson<GimmickVersionResponse>(json);
            }
            catch (Exception e)
            {
                // キャッシュが壊れている場合に、エディタを開くたびエラーにしない
                Debug.LogWarning($"[VGC.Gimmicks] 版の一覧を読めませんでした: {e.Message}");
                return;
            }

            _remote.Clear();

            if (data?.items == null)
                return;

            foreach (var item in data.items)
            {
                if (item != null && !string.IsNullOrEmpty(item.sheetName))
                    _remote[item.sheetName] = item;
            }
        }
        
        private static async UniTask FetchInternal()
        {
            string fetched = null;

            try
            {
                using var req = UnityWebRequest.Get(URL);
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(req.error);
                    return;
                }

                fetched = req.downloadHandler.text;
                Apply(fetched);
            }
            finally
            {
                _isFetching = false;

                await UniTask.SwitchToMainThread();

                // EditorPrefs はメインスレッドで触る
                if (fetched != null)
                {
                    EditorPrefs.SetString(CacheKey, fetched);
                    EditorPrefs.SetString(CacheTimeKey, DateTime.UtcNow.Ticks.ToString());
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        public static bool HasUpdate(string id, string localVersion, out string sheetVersion)
        {
            sheetVersion = null;
            
            if (!_remote.TryGetValue(id, out var remote))
                return false;
            
            sheetVersion = remote.version;

            return CompareVersion(remote.version, localVersion) > 0;
        }

        public static string GetUrl(string id)
        {
            return _remote.TryGetValue(id, out var info) ? info.url : null;
        }

        // "1.2.3" 形式を比較する。"1.0.0-beta" や空文字でも例外にせず、数値として読めない部分は0として扱う
        // (インスペクタ描画中に呼ばれるため、例外で毎フレームエラーにならないようにする)
        private static int CompareVersion(string a, string b)
        {
            var pa = SplitVersion(a);
            var pb = SplitVersion(b);

            for (int i = 0; i < Mathf.Max(pa.Length, pb.Length); i++)
            {
                int va = i < pa.Length ? pa[i] : 0;
                int vb = i < pb.Length ? pb[i] : 0;

                if (va != vb)
                    return va.CompareTo(vb);
            }

            return 0;
        }

        private static int[] SplitVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return System.Array.Empty<int>();

            // "-beta" や "+build" 以降は比較しない
            var core = version.Trim().Split('-', '+')[0];
            var parts = core.Split('.');
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                int.TryParse(parts[i], out result[i]);
            }

            return result;
        }
    }
}