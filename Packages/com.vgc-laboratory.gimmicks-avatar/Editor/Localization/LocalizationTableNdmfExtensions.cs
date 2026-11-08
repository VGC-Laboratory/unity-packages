using System;
using System.Collections.Generic;
using nadena.dev.ndmf.localization;
using UnityEngine;

namespace VGC.Localization.Avatar
{
    /// <summary>
    /// LocalizationTable(スプレッドシート由来)をNDMFのLocalizerとして使うための変換。
    /// ErrorReport.ReportError(localizer, ErrorSeverity.Error, "キー", ...) でNDMF Consoleに翻訳済みのエラーを出せる。
    /// スプシには "キー"(タイトル), "キー:description"(本文), "キー:hint"(ヒント) を置く。
    /// </summary>
    public static class LocalizationTableNdmfExtensions
    {
        private static readonly Dictionary<LocalizationTable, Localizer> Cache = new();

        public static Localizer ToNdmfLocalizer(this LocalizationTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (Cache.TryGetValue(table, out var cached))
                return cached;

            var localizer = new Localizer("en-US", () => new List<(string, Func<string, string>)>
            {
                ("en-US", key => Lookup(table, key, SystemLanguage.English)),
                ("ja-JP", key => Lookup(table, key, SystemLanguage.Japanese)),
                ("ko-KR", key => Lookup(table, key, SystemLanguage.Korean)),
                ("zh-Hans", key => Lookup(table, key, SystemLanguage.ChineseSimplified)),
                ("zh-Hant", key => Lookup(table, key, SystemLanguage.ChineseTraditional)),
            });

            Cache[table] = localizer;
            return localizer;
        }

        // 見つからないときはnullを返す(NDMFが次の候補言語へフォールバックする)
        private static string Lookup(LocalizationTable table, string key, SystemLanguage language)
        {
            return table != null && table.TryGet(key, language, out var text) ? text : null;
        }
    }
}
