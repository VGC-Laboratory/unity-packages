using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VGC.Versioning.Avatar
{
    public static class AvatarUtility
    {
        public static Transform GetAvatarRoot(Transform installer)
        {
            return installer.GetComponentInParent<VRCAvatarDescriptor>()?.transform;
        }
        
        public static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";

            var path = t.name;
            var current = t.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
