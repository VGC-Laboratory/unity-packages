using UnityEngine;
using VRC.SDKBase;

namespace VGC.Gimmicks_MA.Lib
{
    public class AvatarGimmickMarker : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private bool _useCrossPlatformPrefab;
        public bool UseCrossPlatformPrefab => _useCrossPlatformPrefab;
    }
}
