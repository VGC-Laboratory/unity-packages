using UnityEngine;
using VRC.SDKBase;

namespace VGC.Gimmicks
{
    public class GimmickMarker : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private bool _useCrossPlatformPrefab;
        public bool UseCrossPlatformPrefab => _useCrossPlatformPrefab;
    }
}
