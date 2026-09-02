using System;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// <see cref="ExecutorScope"/> 指定での単体検索
    /// </summary>
    public static class ExecuteScopeHelper
    {
        public static UnityEngine.Object FindTarget(Transform t, Type targetType, ExecutorScope scope)
        {
            if (t == null || targetType == null)
                return null;

            switch (scope)
            {
                case ExecutorScope.Self:
                    return t.GetComponent(targetType);

                case ExecutorScope.Children:
                    return t.GetComponentInChildren(targetType, true);

                case ExecutorScope.ChildrenExcludeSelf:
                {
                    foreach (Transform child in t)
                    {
                        var comp = child.GetComponentInChildren(targetType, true);
                        if (comp != null)
                            return comp;
                    }
                    return null;
                }

                case ExecutorScope.Parents:
                    return t.GetComponentInParent(targetType, true);

                case ExecutorScope.Parent:
                {
                    var parent = t.parent;
                    if (parent != null)
                        return parent.GetComponent(targetType);
                    return null;
                }

                case ExecutorScope.ParentHierarchy:
                {
                    // 親が無い場合は自身の階層を対象にする(AutoPopulateUtilityと同じ挙動)
                    var root = t.parent != null ? t.parent : t;
                    return root.GetComponentInChildren(targetType, true);
                }

                case ExecutorScope.NearestParent:
                {
                    var nearest = FindNearestParentWithComponent(t, targetType);
                    if (nearest != null)
                        return nearest.GetComponent(targetType);
                    return null;
                }

                case ExecutorScope.NearestParentHierarchy:
                {
                    var nearest = FindNearestParentWithComponent(t, targetType);
                    if (nearest != null)
                        return nearest.GetComponentInChildren(targetType, true);
                    return null;
                }

                case ExecutorScope.Root:
                    return t.root.GetComponent(targetType);

                case ExecutorScope.RootHierarchy:
                    return t.root.GetComponentInChildren(targetType, true);

                case ExecutorScope.Scene:
                    return UnityEngine.Object.FindFirstObjectByType(targetType);

                default:
                    Debug.LogWarning($"<color=#FF9900>[VGC.ExecuteScopeHelper] Unhandled scope '{scope}'. " +
                                     $"Returning null for type {targetType.FullName}.</color>", t);
                    return null;
            }
        }

        /// <summary>
        /// 最も近い親階層で <paramref name="targetType"/> を持つ Transform を返します。
        /// 非アクティブ状態は無視します。
        /// </summary>
        private static Transform FindNearestParentWithComponent(Transform t, Type targetType)
        {
            var current = t.parent;

            while (current != null)
            {
                if (current.GetComponent(targetType) != null)
                    return current;

                current = current.parent;
            }

            return null;
        }
    }
}
