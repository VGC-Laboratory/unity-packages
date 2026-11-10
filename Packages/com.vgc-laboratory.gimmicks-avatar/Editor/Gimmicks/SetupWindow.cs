using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
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
            
            EditorGUI.BeginChangeCheck();
            _targetAvatar = EditorGUILayout.ObjectField(_localize.Get("TargetAvatar"), _targetAvatar, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
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
                // ギミックが増えると縦に収まらないので、「更新確認」より下だけをスクロールさせる
                GimmickInstallerEditor.DrawVersionCheckButton(_settings, _localize);
                EditorGUILayout.Space(4);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                GimmickInstallerEditor.DrawGimmickList(_targetAvatar, _settings, _localize);
                EditorGUILayout.EndScrollView();
            }
        }

        // 翻訳表にキーが無いとき(gimmicks 本体が古いときなど)はキーがそのまま出てしまうので、英語で出す
        private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
        {
            { "TargetAvatar.Help", "Assign an avatar in the scene. Prefabs in the Project window cannot be assigned." },
            { "TargetAvatar.NotInScene", "Prefabs in the Project window cannot be assigned. Assign an avatar placed in the scene." },
        };

        private string Text(string key)
        {
            return _localize != null && _localize.ContainsKey(key) ? _localize.Get(key) : Fallback[key];
        }
    }
}
