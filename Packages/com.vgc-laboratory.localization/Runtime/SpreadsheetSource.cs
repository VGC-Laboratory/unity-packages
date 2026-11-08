using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VGC.Localization
{
    public enum SpreadsheetSourceKind
    {
        /// <summary>
        /// 公開しているスプレッドシートを gviz の CSV 出力で直接読む。シート ID だけで使える。
        /// シートは「リンクを知っている全員が閲覧可」にしておく。
        /// </summary>
        GoogleSheetsCsv,

        /// <summary>
        /// Apps Script の Web アプリ(doGet)から読む。URL に ?sheet=シート名 を付けて GET する。
        /// 応答は翻訳の配列 [{id, values:[{language, text}]}] か、版の一覧 {items:[{sheetName, version, url}]}。
        /// </summary>
        AppsScript,
    }

    /// <summary>
    /// 翻訳や版の一覧を読むスプレッドシート。翻訳表(LocalizationTable)や版の取得元(VersionSource)から参照する。
    /// 1 つのスプレッドシートを複数の表で使い回せるよう、アセットに分けている。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(SpreadsheetSource), menuName = "VGC/Localization/Spreadsheet Source")]
    public class SpreadsheetSource : ScriptableObject
    {
        private static readonly Regex IdInUrl = new(@"/spreadsheets/d/([a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        [SerializeField] private SpreadsheetSourceKind _kind = SpreadsheetSourceKind.GoogleSheetsCsv;

        [Tooltip("スプレッドシートの ID。URL をそのまま貼ってもよい")]
        [SerializeField] private string _spreadsheetId;

        [Tooltip("Apps Script の Web アプリの URL(…/exec)")]
        [SerializeField] private string _appsScriptUrl;

        public SpreadsheetSourceKind Kind => _kind;
        public string SpreadsheetId => NormalizeId(_spreadsheetId);
        public string AppsScriptUrl => _appsScriptUrl?.Trim();

        /// <summary>コードから一時的な取得元を作る(既定の取得元など、アセットにしないもの)。</summary>
        public static SpreadsheetSource Create(SpreadsheetSourceKind kind, string idOrUrl)
        {
            var source = CreateInstance<SpreadsheetSource>();
            source.hideFlags = HideFlags.HideAndDontSave;
            source._kind = kind;
            if (kind == SpreadsheetSourceKind.AppsScript)
                source._appsScriptUrl = idOrUrl;
            else
                source._spreadsheetId = idOrUrl;
            return source;
        }

        public bool IsValid => _kind == SpreadsheetSourceKind.AppsScript
            ? !string.IsNullOrEmpty(AppsScriptUrl)
            : !string.IsNullOrEmpty(SpreadsheetId);

        /// <summary>取得に使う URL。</summary>
        public string BuildUrl(string sheetName)
        {
            var sheet = Uri.EscapeDataString(sheetName ?? "");
            if (_kind == SpreadsheetSourceKind.AppsScript)
            {
                var url = AppsScriptUrl;
                if (string.IsNullOrEmpty(sheetName))
                    return url;
                return url + (url.Contains("?") ? "&" : "?") + "sheet=" + sheet;
            }

            // headers=1: 1 行目を見出しとして扱わせる(gviz が見出し行を推測して複数行をまとめるのを防ぐ)
            return $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/gviz/tq?tqx=out:csv&headers=1&sheet={sheet}";
        }

        /// <summary>ブラウザで開く URL。Apps Script のときはスプレッドシートが分からないので null。</summary>
        public string BuildEditUrl()
        {
            return _kind == SpreadsheetSourceKind.GoogleSheetsCsv && !string.IsNullOrEmpty(SpreadsheetId)
                ? $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/edit"
                : null;
        }

        /// <summary>キャッシュのキー。同じ取得先なら同じ値になる。</summary>
        public string CacheKey => _kind == SpreadsheetSourceKind.AppsScript ? "gas:" + AppsScriptUrl : "csv:" + SpreadsheetId;

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var match = IdInUrl.Match(value);
            return match.Success ? match.Groups[1].Value : value.Trim();
        }
    }
}
