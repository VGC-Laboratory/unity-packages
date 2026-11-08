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
        
        // nameof では「SetupWindow」になってしまうので、読める名前を直接書く
        [MenuItem("VGC/Gimmick Setup")]
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
                _localize = LocalizationTableResolver.Get("メインウィンドウ");
            
            LocalizeUIHelper.InspectorGUI();
            
            EditorGUI.BeginChangeCheck();
            _targetAvatar = EditorGUILayout.ObjectField(_localize.Get("TargetAvatar"), _targetAvatar, typeof(GameObject), true) as GameObject;
            if (EditorGUI.EndChangeCheck())
            {
                if (_targetAvatar)
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
            
            if (!_installer && _targetAvatar)
            {
                _installer = _targetAvatar.GetComponent<GimmickInstaller>();
                if(!_installer) 
                    _installer = _targetAvatar.AddComponent<GimmickInstaller>();
            }
            
            GUILayout.Space(20);

            if (_targetAvatar)
            {
                GimmickInstallerEditor.DrawSettings(_targetAvatar, _settings, _localize);
            }
        }
    }
}
