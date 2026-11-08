using System;
using System.Collections.Generic;
using nadena.dev.ndmf.localization;

namespace VGC.Gimmicks_MA.Lib.Localization
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
                ("en-US", key => Lookup(table, key, Language.English)),
                ("ja-JP", key => Lookup(table, key, Language.Japanese)),
                ("ko-KR", key => Lookup(table, key, Language.Korean)),
                ("zh-Hans", key => Lookup(table, key, Language.ChineseSimplified)),
                ("zh-Hant", key => Lookup(table, key, Language.ChineseTraditional)),
            });

            Cache[table] = localizer;
            return localizer;
        }

        // 見つからないときはnullを返す(NDMFが次の候補言語へフォールバックする)
        private static string Lookup(LocalizationTable table, string key, Language language)
        {
            return table != null && table.TryGet(key, language, out var text) ? text : null;
        }
    }
}
