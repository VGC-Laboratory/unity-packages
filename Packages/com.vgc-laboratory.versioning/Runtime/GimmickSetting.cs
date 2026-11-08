using System.IO;
using UnityEditor;
using UnityEngine;
using VGC.Localization;
namespace VGC.Versioning
{
    [CreateAssetMenu(fileName = nameof(GimmickSetting), menuName = "VGC/" + nameof(GimmickSetting))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GimmickSetting : ScriptableObject
    {
        [SerializeField] private string _version = "1.0.0";
        [SerializeField] private GameObject _maPrefab;
        [SerializeField] private GameObject[] _dependencies;
        [SerializeField] private GameObject _crossPlatformPrefab;
        [SerializeField] private bool _crossPlatformReady;
        [SerializeField] private bool _useSetting;
        
        LocalizationTable _cachedTable;
        
#if UNITY_EDITOR
        LocalizationTable Table
        {
            get
            {
                if (_cachedTable != null) return _cachedTable;

                var myPath = AssetDatabase.GetAssetPath(this);
                var dir = Path.GetDirectoryName(myPath);

                var guids = AssetDatabase.FindAssets($"t:{nameof(LocalizationTable)}", new[] { dir });

                if (guids.Length == 0)
                {
                    Debug.LogError($"LocalizationTable not found in folder: {dir}", this);
                    return null;
                }

                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _cachedTable = AssetDatabase.LoadAssetAtPath<LocalizationTable>(path);

                return _cachedTable;
            }
        }
        
        public string SheetName => Table.SheetName;
#endif
        public string Version => _version;
        public GameObject Prefab => _maPrefab;
        public GameObject[] Dependencies => _dependencies;
        public GameObject CrossPlatformPrefab => _crossPlatformPrefab;
        public bool CrossPlatformReady => _crossPlatformReady;
        public bool UseSetting => _useSetting;
        public string Name => 
#if UNITY_EDITOR
            Table.Get("GimmickName");
#else
            "GimmickName";
#endif
    }
}
