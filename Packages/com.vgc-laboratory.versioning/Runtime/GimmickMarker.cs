using UnityEngine;
using VRC.SDKBase;

namespace VGC.Versioning
{
    public class GimmickMarker : MonoBehaviour, IEditorOnly
    {
        [SerializeField] private bool _useCrossPlatformPrefab;
        public bool UseCrossPlatformPrefab => _useCrossPlatformPrefab;
    }
}
