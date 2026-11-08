using System.Linq;
using UnityEditor;
using UnityEngine;
using VGC.Gimmicks_MA.Lib.Localization;
using VRC.SDK3.Avatars.Components;

namespace VGC.Gimmicks_MA.Lib
{
    public class AvatarGimmickSetupWindow : EditorWindow
    {
        private LocalizationTable _localize;
        private AvatarGimmickSetting[] _settings;
        
        private GameObject _targetAvatar;
        private VRCAvatarDescriptor _descriptor;
        private AvatarGimmickInstaller _installer;
        
        [MenuItem("VGC/" + nameof(AvatarGimmickSetupWindow))]
        static void Open()
        {
            var window = GetWindow<AvatarGimmickSetupWindow>(nameof(AvatarGimmickSetupWindow));
            window.minSize = new Vector2(500, 700);
        }

        private void OnEnable()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(AvatarGimmickSetting)}");
            _settings = guids.Select(AssetDatabase.GUIDToAssetPath)
                             .Select(AssetDatabase.LoadAssetAtPath<AvatarGimmickSetting>)
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
                        _installer = _targetAvatar.GetComponent<AvatarGimmickInstaller>();
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
                _installer = _targetAvatar.GetComponent<AvatarGimmickInstaller>();
                if(!_installer) 
                    _installer = _targetAvatar.AddComponent<AvatarGimmickInstaller>();
            }
            
            GUILayout.Space(20);

            if (_targetAvatar)
            {
                AvatarGimmickInstallerEditor.DrawSettings(_targetAvatar, _settings, _localize);
            }
        }
    }
}
