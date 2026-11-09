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
            var urls = VersionChecker.GetUrls(source, key);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var message = texts != null ? texts.Get("NewVersion") : "New version";
                EditorGUILayout.LabelField($"{message}: v{localVersion} → v{remote}", EditorStyles.boldLabel);

                // 配布先が書かれていなければ、お知らせだけ出してボタンは出さない
                if (urls.Length > 0)
                {
                    var label = texts != null ? texts.Get("Update") : "Update";
                    DrawOpenButton(label, urls, GUILayout.Width(100));
                }
            }

            return true;
        }

        /// <summary>
        /// 配布先を開くボタン。URL が 1 つならそのまま開き、2 つ以上なら行き先を選ぶメニューを出す。
        /// URL が無いときは押せない状態で描く(並びを崩さないため)。
        /// </summary>
        public static void DrawOpenButton(string label, string[] urls, params GUILayoutOption[] options)
        {
            urls ??= System.Array.Empty<string>();
            using (new EditorGUI.DisabledScope(urls.Length == 0))
            {
                if (!GUILayout.Button(label, options))
                    return;
            }

            if (urls.Length == 1)
            {
                Application.OpenURL(urls[0]);
                return;
            }

            // GenericMenu は "/" を階層の区切りにするので、URL そのものではなくホスト名を出す
            var menu = new GenericMenu();
            for (var i = 0; i < urls.Length; i++)
            {
                var url = urls[i];
                var host = System.Uri.TryCreate(url, System.UriKind.Absolute, out var uri) ? uri.Host : url;
                menu.AddItem(new GUIContent($"{i + 1}. {host}"), false, () => Application.OpenURL(url));
            }

            menu.ShowAsContext();
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
