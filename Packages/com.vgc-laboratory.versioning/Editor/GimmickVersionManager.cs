using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace VGC.Versioning
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

        private static readonly Dictionary<string, GimmickVersionInfo> _remote = new();
        private static bool _isFetching;

        public static bool IsFetching => _isFetching;

        private static UniTask _currentTask;

        static GimmickVersionManager()
        {
            Fetch().Forget(Debug.LogException);
        }
        
        public static UniTask Fetch()
        {
            if (_isFetching)
                return _currentTask;

            _isFetching = true;
            return _currentTask = FetchInternal();
        }
        
        private static async UniTask FetchInternal()
        {
            try
            {
                using var req = UnityWebRequest.Get(URL);
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(req.error);
                    return;
                }

                var json = req.downloadHandler.text;
                var data = JsonUtility.FromJson<GimmickVersionResponse>(json);

                _remote.Clear();

                if (data?.items != null)
                {
                    foreach (var item in data.items)
                    {
                        _remote[item.sheetName] = item;
                    }
                }
            }
            finally
            {
                _isFetching = false;

                await UniTask.SwitchToMainThread();
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