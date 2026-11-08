using System.Collections.Generic;
using System.Linq;
// ReSharper disable once RedundantUsingDirective
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Localization
{
    public static class LocalizationTableResolver
    {
        private static Dictionary<string, LocalizationTable> _map;

        private static void Initialize()
        {
            _map = new Dictionary<string, LocalizationTable>();

#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationTable)}");
            var tables = guids.Select(AssetDatabase.GUIDToAssetPath)
                              .Select(AssetDatabase.LoadAssetAtPath<LocalizationTable>)
                              .Where(t => t != null)
                              .ToList();
#else
            var tables = Resources.LoadAll<LocalizationTable>("");
#endif
            foreach (var table in tables)
            {
                if (string.IsNullOrEmpty(table.SheetName)) continue;

                _map[table.SheetName] = table;
            }
        }

        public static LocalizationTable Get(string sheetName)
        {
            if (_map == null)
            {
                Initialize();
            }

            // 後から取り込んだテーブルや、再インポートで破棄されたテーブルに対応するため、
            // 見つからない(またはUnity上で破棄済み)ときは1回だけ探し直す
            if (!_map.TryGetValue(sheetName, out var table) || table == null)
            {
                Initialize();
                _map.TryGetValue(sheetName, out table);
            }

            return table;
        }
    }
}