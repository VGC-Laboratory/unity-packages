#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VGC.Localization
{
    /// <summary>
    /// エディタで翻訳表を探す。
    ///
    /// プロジェクト全体をシート名で探すと、別の作者の表と名前が衝突する(「メインウィンドウ」など)。
    /// そのため、GUID・パス・フォルダ・隣のアセットのどれかで場所を絞って探す。
    /// 自分のパッケージに入れた決まった表は GUID で読むとよい(移動・改名しても変わらない)。
    /// </summary>
    public static class LocalizationTableResolver
    {
        private static readonly Dictionary<string, LocalizationTable> Cache = new();

        /// <summary>GUID を指定して読む。</summary>
        public static LocalizationTable LoadByGuid(string guid)
        {
            return string.IsNullOrEmpty(guid) ? null : LoadAtPath(AssetDatabase.GUIDToAssetPath(guid));
        }

        /// <summary>パスを指定して読む。</summary>
        public static LocalizationTable LoadAtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (Cache.TryGetValue(assetPath, out var cached) && cached != null)
                return cached;

            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(assetPath);
            if (table != null)
                Cache[assetPath] = table;
            return table;
        }

        /// <summary>
        /// フォルダの中(サブフォルダを含む)から探す。sheetName を省くと最初に見つかった表を返す。
        /// </summary>
        public static LocalizationTable FindInFolder(string folder, string sheetName = null)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                return null;

            var key = folder + "|" + sheetName;
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(LocalizationTable)}", new[] { folder }))
            {
                var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table == null)
                    continue;

                if (string.IsNullOrEmpty(sheetName) || table.SheetName == sheetName)
                {
                    Cache[key] = table;
                    return table;
                }
            }

            return null;
        }

        /// <summary>アセットと同じフォルダから探す。</summary>
        public static LocalizationTable FindNextTo(Object asset, string sheetName = null)
        {
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
            if (string.IsNullOrEmpty(path))
                return null;

            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            return FindInFolder(folder, sheetName);
        }
    }
}
#endif
