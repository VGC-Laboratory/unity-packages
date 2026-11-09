using UnityEngine;
#if VGC_VRCSDK_BASE
using VRC.SDKBase;
#endif

namespace VGC.Gimmicks
{
    // IEditorOnly は VRChat の SDK があるときだけ付ける(理由は GimmickInstaller と同じ)。
    public class GimmickMarker : MonoBehaviour
#if VGC_VRCSDK_BASE
        , IEditorOnly
#endif
    {
        [SerializeField] private bool _useCrossPlatformPrefab;
        public bool UseCrossPlatformPrefab => _useCrossPlatformPrefab;
    }
}
