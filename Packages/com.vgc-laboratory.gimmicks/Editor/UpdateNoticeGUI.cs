using UnityEditor;
using UnityEngine;
using VGC.Localization;

namespace VGC.Gimmicks
{
    /// <summary>
    /// 更新のお知らせをインスペクタやウィンドウに 1 行で出す部品。
    ///
    ///   UpdateNoticeGUI.Draw(versionSource, "MyTool", "1.0.0");
    ///
    /// 新しい版がシートにあるときだけ、お知らせとダウンロードのボタンを出す。
    /// </summary>
    public static class UpdateNoticeGUI
    {
        /// <summary>新しい版があればお知らせを出す。出したら true。</summary>
        public static bool Draw(VersionSource source, string key, string localVersion)
        {
            if (!VersionChecker.HasUpdate(source, key, localVersion, out var remote))
                return false;

            var texts = GimmickTexts.Installer;
            var url = VersionChecker.GetUrl(source, key);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var message = texts != null ? texts.Get("NewVersion") : "New version";
                EditorGUILayout.LabelField($"{message}: v{localVersion} → v{remote}", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(url)))
                {
                    var label = texts != null ? texts.Get("Update") : "Update";
                    if (GUILayout.Button(label, GUILayout.Width(100)))
                        Application.OpenURL(url);
                }
            }

            return true;
        }

        /// <summary>「更新を確認」ボタン。押すとキャッシュを無視して取り直す。取得元が無ければ何も出さない。</summary>
        public static void DrawCheckButton(VersionSource source)
        {
            if (source == null)
                return;

            var texts = GimmickTexts.Installer;
            using (new EditorGUI.DisabledScope(VersionChecker.IsFetching(source)))
            {
                var label = texts != null ? texts.Get("VersionCheck") : "Check for updates";
                if (GUILayout.Button(label, GUILayout.Height(24)))
                    VersionChecker.Fetch(source, force: true);
            }
        }
    }

    /// <summary>このパッケージに入っている翻訳表。</summary>
    public static class GimmickTexts
    {
        // Config/GimmickInstallerTable.asset(シート名「メインウィンドウ」)
        private const string InstallerTableGuid = "e140883fe5e3cfc4d89a192a9223dbe1";

        /// <summary>導入画面と更新のお知らせの文言。</summary>
        public static LocalizationTable Installer => LocalizationTableResolver.LoadByGuid(InstallerTableGuid);
    }
}
