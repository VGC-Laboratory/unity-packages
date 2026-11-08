using UnityEngine;
using VGC.Localization;

namespace VGC.Gimmicks
{
    /// <summary>
    /// 最新版の一覧を読むシート。ギミック(GimmickSetting)や任意のツールの更新チェックが参照する。
    ///
    /// シートの形: 1 行目は見出し。A 列がキー(ギミックの識別名)、B 列が最新版(1.2.3)、C 列がダウンロード URL。
    /// Apps Script のときは {items:[{sheetName, version, url}]} を返す doGet にする。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(VersionSource), menuName = "VGC/Gimmicks/Version Source")]
    public class VersionSource : ScriptableObject
    {
        [SerializeField] private SpreadsheetSource _source;

        [Tooltip("版の一覧があるシート(タブ)の名前。Apps Script で不要なら空でよい")]
        [SerializeField] private string _sheetName;

        public SpreadsheetSource Source => _source;
        public string SheetName => _sheetName;

        public bool IsValid => _source != null && _source.IsValid;

        /// <summary>同じ取得先なら同じ値になる。キャッシュのキーに使う。</summary>
        public string CacheKey => (_source != null ? _source.CacheKey : "") + "|" + _sheetName;

        /// <summary>コードから一時的な取得元を作る(既定の取得元など、アセットにしないもの)。</summary>
        public static VersionSource Create(SpreadsheetSource source, string sheetName = null)
        {
            var instance = CreateInstance<VersionSource>();
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance._source = source;
            instance._sheetName = sheetName;
            return instance;
        }
    }
}
