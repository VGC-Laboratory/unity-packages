#if MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#if UNITY_EDITOR
using nadena.dev.ndmf;
#endif
#endif
using System;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace VGC.Localization.Avatar
{
    [AddComponentMenu("VGC/VGC Localization Ex Menu")]
    public class LocalizationExMenu : LocalizedBehaviour, IEditorOnly
    {
        [SerializeField] private LocalizeMenuInfo _menuInfo;
#if MODULAR_AVATAR
        [SerializeField] private ModularAvatarMenuInstaller _installer;
#endif
        public void Configure()
        {
#if UNITY_EDITOR && MODULAR_AVATAR
            if (_installer == null)
            {
                Debug.LogError($"Not Setting installer");
                return;
            }
            
            var oldMenu = _installer.menuToAppend;
            var newMenu = _menuInfo.GetMenu();
            _installer.menuToAppend = newMenu;
            if (oldMenu != null && newMenu != null && oldMenu != newMenu)
            {
                ObjectRegistry.TryRegisterReplacedObject(ObjectRegistry.GetReference(oldMenu), newMenu);
            }
#endif
        }
    }

    [Serializable]
    public class LocalizeMenuInfo
    {
        [SerializeField] private VRCExpressionsMenu _menuEn;
        [SerializeField] private VRCExpressionsMenu _menuJa;
        [SerializeField] private VRCExpressionsMenu _menuKr;
        [SerializeField] private VRCExpressionsMenu _menuCs;
        [SerializeField] private VRCExpressionsMenu _menuCt;

        public VRCExpressionsMenu GetMenu()
        {
#if UNITY_EDITOR
            switch (LanguageSetting.Current)
            {
                case SystemLanguage.English:
                    break;
                case SystemLanguage.Japanese:
                    if(_menuJa != null)
                        return _menuJa;
                    break;
                case SystemLanguage.Korean:
                    if(_menuKr != null)
                        return _menuKr;
                    break;
                case SystemLanguage.ChineseSimplified:
                    if(_menuCs != null)
                        return _menuCs;
                    break;
                case SystemLanguage.ChineseTraditional:
                    if(_menuCt != null)
                        return _menuCt;
                    break;
            }
#endif
            return _menuEn;
        }
    }
}