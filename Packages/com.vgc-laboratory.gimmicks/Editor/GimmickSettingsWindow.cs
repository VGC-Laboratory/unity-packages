using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VGC.Localization;

namespace VGC.Gimmicks
{
    /// <summary>
    /// ギミックの設定ウィンドウ。<see cref="IGimmickSettings"/> を付けたコンポーネントのインスペクタだけを表示する。
    ///
    /// ギミックの GameObject を選ぶと、インスペクタには MA のコンポーネントや GimmickMarker も並ぶ。
    /// 設定項目だけを見せるために、ギミック共通のウィンドウを 1 つ用意している(ギミックごとには作らない)。
    /// 表示には各コンポーネントのエディタ([CustomEditor] があればそれ)をそのまま使う。
    /// </summary>
    public class GimmickSettingsWindow : EditorWindow
    {
        // ドメインのリロードのあとも開いたまま使えるように、対象は Object 参照でシリアライズする
        [SerializeField] private GameObject[] _instances = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject _current;
        [SerializeField] private Vector2 _scroll;

        // 表示中のエディタ。対象が変わったときとウィンドウを閉じたときに破棄する
        private readonly List<Editor> _editors = new();

        /// <summary>
        /// インスタンス(とその子)に、設定用のコンポーネント(<see cref="IGimmickSettings"/>)があるか。
        /// </summary>
        public static bool HasSettings(GameObject instance)
        {
            return FindSettings(instance).Count > 0;
        }

        /// <summary>
        /// ギミックのインスタンスを指定して開く。設定用のコンポーネントを持つインスタンスが複数あれば、上部で切り替えられる。
        /// </summary>
        public static void Open(GameObject[] instances)
        {
            var alive = (instances ?? System.Array.Empty<GameObject>()).Where(go => go != null).Distinct().ToArray();
            var withSettings = alive.Where(HasSettings).ToArray();

            var window = GetWindow<GimmickSettingsWindow>();
            window.minSize = new Vector2(360, 240);
            // 設定を持つものが 1 つも無いときは、そのまま渡して案内を出す
            window._instances = withSettings.Length > 0 ? withSettings : alive;
            window._current = window._instances.FirstOrDefault();
            window._scroll = Vector2.zero;
            window.ClearEditors();
            window.UpdateTitle();
            window.Show();
            window.Focus();
        }

        private static List<MonoBehaviour> FindSettings(GameObject instance)
        {
            if (instance == null)
                return new List<MonoBehaviour>();

            // スクリプトが外れているコンポーネントは null で返るが、is で落ちる
            return instance.GetComponentsInChildren<MonoBehaviour>(true)
                           .Where(c => c is IGimmickSettings)
                           .ToList();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += Repaint;
            UpdateTitle();
        }

        private void OnDisable()
        {
            // 閉じたときのほか、ドメインのリロードの前にも呼ばれる
            Undo.undoRedoPerformed -= Repaint;
            ClearEditors();
        }

        // 導入・削除やコンポーネントの付け外しで対象が変わったら描き直す
        private void OnHierarchyChange() => Repaint();

        private void OnInspectorUpdate()
        {
            if (_editors.Any(e => e != null && e.RequiresConstantRepaint()))
                Repaint();
        }

        private void OnGUI()
        {
            var localize = GimmickTexts.Installer;
            UpdateTitle();

            // レイアウトと描画で中身が食い違わないように、対象の作り直しはレイアウトのときだけ行う
            if (Event.current.type == EventType.Layout)
                SyncEditors();

            var alive = _instances.Where(go => go != null).ToArray();
            if (alive.Length == 0 || _current == null || _editors.Count == 0)
            {
                // アンインストールした・シーンを閉じた・目印のあるコンポーネントが無い
                EditorGUILayout.HelpBox(Text(localize, "SettingsWindow.NotFound"), MessageType.Info);
                if (_current != null)
                    DrawSelectButton(localize);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (alive.Length > 1)
                {
                    // 同じ名前が並ぶので番号を付ける。"/" は階層の区切りになるので置き換える
                    var names = alive.Select((go, i) => $"{i + 1}: {go.name.Replace('/', '∕')}").ToArray();
                    var index = Mathf.Max(0, System.Array.IndexOf(alive, _current));
                    EditorGUI.BeginChangeCheck();
                    index = EditorGUILayout.Popup(index, names);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _current = alive[index];
                        _scroll = Vector2.zero;
                        // このフレームの残りは前の対象のまま描き、次のレイアウトで作り直す
                        Repaint();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(_current.name, EditorStyles.boldLabel);
                }

                DrawSelectButton(localize, GUILayout.ExpandWidth(false));
            }

            EditorGUILayout.Space(4);

            // インスペクタと同じ見た目にする(ラベルの幅と、Vector などを 1 行に並べる幅)
            var oldWide = EditorGUIUtility.wideMode;
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.wideMode = position.width > 330f;
            EditorGUIUtility.labelWidth = Mathf.Max(position.width * 0.45f - 40f, 120f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < _editors.Count; i++)
            {
                var editor = _editors[i];
                // 描いている途中で対象が消えることがある(Undo など)。次のレイアウトで作り直す
                if (editor == null || editor.target == null)
                    continue;

                if (_editors.Count > 1)
                {
                    if (i > 0)
                        EditorGUILayout.Space(8);
                    EditorGUILayout.LabelField(ObjectNames.GetInspectorTitle(editor.target), EditorStyles.boldLabel);
                }

                editor.OnInspectorGUI();
            }
            EditorGUILayout.EndScrollView();

            EditorGUIUtility.wideMode = oldWide;
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        /// <summary>今までの動き(GameObject を選ぶ)。MA のコンポーネントなどを見たい人向け。</summary>
        private void DrawSelectButton(LocalizationTable localize, params GUILayoutOption[] options)
        {
            if (!GUILayout.Button(Text(localize, "SettingsWindow.SelectInHierarchy"), options))
                return;

            Selection.activeGameObject = _current;
            EditorGUIUtility.PingObject(_current);
        }

        /// <summary>
        /// 表示中のエディタを、今の対象のコンポーネントに合わせる。変わっていなければ何もしない。
        /// </summary>
        private void SyncEditors()
        {
            if (_current == null)
                _current = _instances.FirstOrDefault(go => go != null);

            var components = FindSettings(_current);
            if (_editors.Count == components.Count &&
                !_editors.Where((e, i) => e == null || e.target != components[i]).Any())
                return;

            ClearEditors();
            foreach (var component in components)
                _editors.Add(Editor.CreateEditor(component));
        }

        private void ClearEditors()
        {
            foreach (var editor in _editors)
            {
                if (editor != null)
                    DestroyImmediate(editor);
            }

            _editors.Clear();
        }

        private void UpdateTitle()
        {
            var title = Text(GimmickTexts.Installer, "SettingsWindow.Title");
            if (titleContent.text != title)
                titleContent = new GUIContent(title);
        }

        // 翻訳表にキーが無いとき(古い表を取り込み直したときなど)はキーがそのまま出てしまうので、英語で出す
        private static readonly Dictionary<string, string> Fallback = new()
        {
            { "SettingsWindow.Title", "Gimmick Settings" },
            { "SettingsWindow.SelectInHierarchy", "Select in Hierarchy" },
            { "SettingsWindow.NotFound", "No gimmick settings to show. Open this window again from the Setting button of Gimmick Setup." },
        };

        private static string Text(LocalizationTable localize, string key)
        {
            return localize != null && localize.ContainsKey(key) ? localize.Get(key) : Fallback[key];
        }
    }
}
