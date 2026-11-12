using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VGC.Localization;
using VRC.SDK3.Avatars.Components;

namespace VGC.Gimmicks.Avatar
{
    public class SetupWindow : EditorWindow
    {
        private LocalizationTable _localize;
        private GimmickSetting[] _settings;
        
        private GameObject _targetAvatar;
        private VRCAvatarDescriptor _descriptor;
        private GimmickInstaller _installer;
        private Vector2 _scroll;

        // モバイルの制限の欄。開閉と「前回 Very Poor だったか」は、ドメインのリロードで開き直さないようにシリアライズする
        [SerializeField] private bool _mobileFoldout;
        [SerializeField] private bool _mobileWasVeryPoor;
        private MobileDynamicsLimits _mobileLimits;
        private bool _mobileDirty = true;
        private double _mobileCalculatedAt;

        // 計算は軽くないので、ヒエラルキーが続けて変わるときも、この間隔より細かくは計算し直さない
        private const double MobileRecalculateInterval = 0.5;

        // nameof では「SetupWindow」になってしまうので、読める名前を直接書く。
        // アバター固有のメニューは VGC/AvatarGimmick にまとめる（gimmicks 本体は VRChat の SDK が無くても使うため）。
        // 優先度 0 で VGC メニューの先頭に出す
        [MenuItem("VGC/AvatarGimmick/Setup", false, 0)]
        static void Open()
        {
            var window = GetWindow<SetupWindow>("Gimmick Setup");
            window.minSize = new Vector2(500, 700);
        }

        private void OnEnable()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(GimmickSetting)}");
            _settings = guids.Select(AssetDatabase.GUIDToAssetPath)
                             .Select(AssetDatabase.LoadAssetAtPath<GimmickSetting>)
                             .Where(setting => setting != null)
                             .ToArray();
        }

        private void OnGUI()
        {
            if(_localize == null)
                _localize = GimmickTexts.Installer;
            
            LocalizeUIHelper.InspectorGUI();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var picked = EditorGUILayout.ObjectField(_localize.Get("TargetAvatar"), _targetAvatar, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                    SetTargetAvatar(picked);

                // ObjectField の選択ウィンドウは、シーンの GameObject を全部並べるので探しにくい。
                // シーン上のアバター(VRCAvatarDescriptor)だけを並べたメニューを横に出す
                var content = new GUIContent(Text("TargetAvatar.SelectFromScene"));
                var rect = GUILayoutUtility.GetRect(content, EditorStyles.popup, GUILayout.ExpandWidth(false));
                if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard, EditorStyles.popup))
                    ShowSceneAvatarMenu(rect);
            }

            if (!_targetAvatar)
                EditorGUILayout.HelpBox(Text("TargetAvatar.Help"), MessageType.Info);

            if (!_installer && _targetAvatar)
            {
                _installer = _targetAvatar.GetComponent<GimmickInstaller>();
                if(!_installer) 
                    _installer = _targetAvatar.AddComponent<GimmickInstaller>();
            }
            
            GUILayout.Space(20);

            if (_targetAvatar)
            {
                // ギミックを入れた結果がいつも見えるように、スクロールの外に置く。
                // レイアウトと描画で中身が食い違わないように、計算し直すのはレイアウトのときだけ
                if (Event.current.type == EventType.Layout)
                    UpdateMobileLimits();
                _mobileFoldout = MobileDynamicsLimitsGUI.Draw(_mobileLimits, _mobileFoldout, _localize);
                EditorGUILayout.Space(4);

                // ギミックが増えると縦に収まらないので、「更新確認」より下だけをスクロールさせる
                GimmickInstallerEditor.DrawVersionCheckButton(_settings, _localize);
                EditorGUILayout.Space(4);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                GimmickInstallerEditor.DrawGimmickList(_targetAvatar, _settings, _localize);
                EditorGUILayout.EndScrollView();
            }
        }

        // ギミックを入れた・外した、コンポーネントを足した、などで数が変わる
        private void OnHierarchyChange()
        {
            _mobileDirty = true;
            Repaint();
        }

        // 間隔を空けるために計算を見送った分を、あとで拾う
        private void OnInspectorUpdate()
        {
            if (_mobileDirty && _targetAvatar)
                Repaint();
        }

        /// <summary>
        /// モバイルの制限を、変わったときだけ計算し直す。Very Poor になった変わり目で欄を開く。
        /// </summary>
        private void UpdateMobileLimits()
        {
            if (!_mobileDirty || EditorApplication.timeSinceStartup - _mobileCalculatedAt < MobileRecalculateInterval)
                return;

            _mobileDirty = false;
            _mobileCalculatedAt = EditorApplication.timeSinceStartup;
            try
            {
                _mobileLimits = MobileDynamicsLimits.Calculate(_targetAvatar);
            }
            catch (System.Exception e)
            {
                // SDK の計算が落ちても、導入の操作は続けられるようにする
                _mobileLimits = null;
                Debug.LogWarning($"[VGC] モバイルの制限を計算できませんでした: {e.Message}");
                return;
            }

            // 開くのは変わり目だけ。Very Poor のまま利用者が畳んだら、そのままにする
            var veryPoor = _mobileLimits.AnyVeryPoor;
            if (veryPoor && !_mobileWasVeryPoor)
                _mobileFoldout = true;
            _mobileWasVeryPoor = veryPoor;
        }

        /// <summary>
        /// インストール先を変える。欄に入れたときも、メニューから選んだときも、同じ確認を通す。
        /// </summary>
        private void SetTargetAvatar(GameObject target)
        {
            _targetAvatar = target;

            // 選び直したら、Very Poor なら 1 回開く(前のアバターの状態は持ち越さない)
            _mobileLimits = null;
            _mobileDirty = true;
            _mobileCalculatedAt = 0;
            _mobileWasVeryPoor = false;

            // Project ウィンドウのプレハブ(アセット)は受け付けない。
            // アセットの中には子としてギミックを置けず、消すこともできないため。
            // プレハブ編集モードで開いた中のオブジェクトはアセットではないので通る
            if (_targetAvatar && EditorUtility.IsPersistent(_targetAvatar))
            {
                EditorUtility.DisplayDialog(_localize.Get("DialogTitle"), Text("TargetAvatar.NotInScene"), _localize.Get("DialogOK"));
                _targetAvatar = null;
                _descriptor = null;
                _installer = null;
            }
            else if (_targetAvatar)
            {
                _descriptor = _targetAvatar.GetComponent<VRCAvatarDescriptor>();
                if (_descriptor)
                {
                    _installer = _targetAvatar.GetComponent<GimmickInstaller>();
                }
                else
                {
                    EditorUtility.DisplayDialog(_localize.Get("DialogTitle"), _localize.Get("DialogMessage"), _localize.Get("DialogOK"));
                    _targetAvatar = null;
                    _installer = null;
                }
            }
            else
            {
                _descriptor = null;
                _installer = null;
            }
        }

        /// <summary>
        /// シーン上のアバターを並べたメニューを出す。選ぶとインストール先になる。
        /// </summary>
        private void ShowSceneAvatarMenu(Rect rect)
        {
            var avatars = FindSceneAvatars();
            var menu = new GenericMenu();
            if (avatars.Count == 0)
                menu.AddDisabledItem(new GUIContent(Text("TargetAvatar.NoneInScene")));

            for (var i = 0; i < avatars.Count; i++)
            {
                var avatar = avatars[i];
                // 同じ名前はメニューで 1 つにまとまってしまうので番号を付ける。"/" は階層の区切りになるので置き換える
                var label = $"{i + 1}: {avatar.name.Replace('/', '∕')}";
                menu.AddItem(new GUIContent(label), avatar == _targetAvatar, () =>
                {
                    // メニューを開いている間に消えていることがある
                    if (!avatar)
                        return;

                    SetTargetAvatar(avatar);
                    Repaint();
                });
            }

            menu.DropDown(rect);
        }

        /// <summary>
        /// 今編集している場所にあるアバターを、ヒエラルキーの上から順に返す(非アクティブも含む)。
        /// プレハブ編集モードではその中だけ、それ以外は読み込んでいるシーン全部から探す。
        /// </summary>
        private static List<GameObject> FindSceneAvatars()
        {
            var roots = new List<GameObject>();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                roots.Add(stage.prefabContentsRoot);
            }
            else
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                        roots.AddRange(scene.GetRootGameObjects());
                }
            }

            return roots.SelectMany(root => root.GetComponentsInChildren<VRCAvatarDescriptor>(true))
                        .Select(descriptor => descriptor.gameObject)
                        .ToList();
        }

        // 翻訳表にキーが無いとき(gimmicks 本体が古いときなど)はキーがそのまま出てしまうので、英語で出す
        private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
        {
            { "TargetAvatar.SelectFromScene", "Select from Scene" },
            { "TargetAvatar.NoneInScene", "No avatars in the scene" },
            { "TargetAvatar.Help", "Assign an avatar in the scene. Prefabs in the Project window cannot be assigned." },
            { "TargetAvatar.NotInScene", "Prefabs in the Project window cannot be assigned. Assign an avatar placed in the scene." },
        };

        private string Text(string key)
        {
            return _localize != null && _localize.ContainsKey(key) ? _localize.Get(key) : Fallback[key];
        }
    }
}
