using UnityEngine;
using VGC.Localization;
#if VGC_VRCSDK_BASE
using VRC.SDKBase;
#endif

namespace VGC.Gimmicks
{
    // IEditorOnly は VRChat の SDK(com.vrchat.base)があるときだけ付ける。記号 VGC_VRCSDK_BASE は asmdef の versionDefines で定義する。
    // SDK の無いプロジェクトでもこのパッケージをコンパイルできるようにするため。インターフェースの有無でシリアライズは変わらない。
    [AddComponentMenu("VGC/VGC AvatarGimmick Installer")]
    public class GimmickInstaller : LocalizedBehaviour
#if VGC_VRCSDK_BASE
        , IEditorOnly
#endif
    {
    }
}