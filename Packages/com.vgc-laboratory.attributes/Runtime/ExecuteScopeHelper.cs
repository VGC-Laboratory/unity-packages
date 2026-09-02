#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// <see cref="ExecutorScope"/> の解釈を一手に引き受けるヘルパ。
    /// scope に対する switch はこのクラスの <see cref="FindTargets"/> 1 箇所だけに置き、
    /// 他の Executor は必ずここを経由すること。
    /// (解釈が複数箇所にあると片方だけ更新され、黙って null を返す不具合になる)
    /// </summary>
    internal static class ExecuteScopeHelper
    {
        /// <summary>
        /// <paramref name="scope"/> の範囲から <paramref name="targetType"/> を実装する
        /// Component を全て取得します。順序は Unity の走査順のままです。
        /// </summary>
        /// <param name="t">検索の基準となる Transform</param>
        /// <param name="targetType">取得したい型。interface も指定できます</param>
        /// <param name="scope">検索範囲</param>
        /// <param name="anchorType">
        /// NearestParent / NearestParentHierarchy で親を特定するための型。
        /// null の場合は <paramref name="targetType"/> 自身をアンカーとして扱います
        /// </param>
        /// <param name="includeInactive">非アクティブな GameObject を含めるか</param>
        public static Component[] FindTargets(
            Transform t,
            Type targetType,
            ExecutorScope scope,
            Type anchorType = null,
            bool includeInactive = true)
        {
            if (t == null || targetType == null)
                return Array.Empty<Component>();

            if (anchorType == null)
                anchorType = targetType;

            switch (scope)
            {
                case ExecutorScope.Self:
                    return GetComponents(t, targetType, includeInactive);

                case ExecutorScope.Children:
                    return t.GetComponentsInChildren(targetType, includeInactive);

                case ExecutorScope.ChildrenExcludeSelf:
                    return t.GetComponentsInChildren(targetType, includeInactive)
                            .Where(c => c.transform != t)
                            .ToArray();

                case ExecutorScope.Parents:
                    return t.GetComponentsInParent(targetType, includeInactive);

                case ExecutorScope.Parent:
                {
                    var parent = t.parent;
                    return parent != null
                        ? GetComponents(parent, targetType, includeInactive)
                        : Array.Empty<Component>();
                }

                case ExecutorScope.ParentHierarchy:
                {
                    // 親が無い場合は自身の階層を対象にする
                    var root = t.parent != null ? t.parent : t;
                    return root.GetComponentsInChildren(targetType, includeInactive);
                }

                case ExecutorScope.NearestParent:
                {
                    var nearest = FindNearestParentWithComponent(t, anchorType);
                    return nearest != null
                        ? GetComponents(nearest, targetType, includeInactive)
                        : Array.Empty<Component>();
                }

                case ExecutorScope.NearestParentHierarchy:
                {
                    var nearest = FindNearestParentWithComponent(t, anchorType);
                    return nearest != null
                        ? nearest.GetComponentsInChildren(targetType, includeInactive)
                        : Array.Empty<Component>();
                }

                case ExecutorScope.Root:
                    return GetComponents(t.root, targetType, includeInactive);

                case ExecutorScope.RootHierarchy:
                    return t.root.GetComponentsInChildren(targetType, includeInactive);

                case ExecutorScope.Scene:
                    return FindInScene(targetType, includeInactive);

                default:
                    Debug.LogWarning($"<color=#FF9900>[VGC.ExecuteScopeHelper] Unhandled scope '{scope}'.\n" +
                                     $"Type: {targetType.FullName}</color>", t);
                    return Array.Empty<Component>();
            }
        }

        /// <summary>
        /// <see cref="FindTargets"/> の先頭 1 件だけを返します。
        /// </summary>
        public static UnityEngine.Object FindTarget(Transform t, Type targetType, ExecutorScope scope)
        {
            var found = FindTargets(t, targetType, scope);
            return found.Length > 0 ? found[0] : null;
        }

        /// <summary>
        /// シーン全体から検索します。
        /// FindObjectsByType は interface を受け付けないため、
        /// interface 指定時は MonoBehaviour を全走査して絞り込みます。
        /// </summary>
        private static Component[] FindInScene(Type targetType, bool includeInactive)
        {
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                return ExecutorSharedCache.GetTargets(targetType, includeInactive)
                                          .OfType<Component>()
                                          .ToArray();
            }

            return ExecutorSharedCache.GetTargets<MonoBehaviour>(includeInactive)
                                      .Where(m => targetType.IsAssignableFrom(m.GetType()))
                                      .Cast<Component>()
                                      .ToArray();
        }

        /// <summary>
        /// 最も近い親階層で <paramref name="anchorType"/> を持つ Transform を返します。
        /// 親探索時は非アクティブ状態を無視します。
        /// </summary>
        private static Transform FindNearestParentWithComponent(Transform t, Type anchorType)
        {
            var current = t.parent;

            while (current != null)
            {
                if (current.GetComponent(anchorType) != null)
                    return current;

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// 単一 GameObject から取得します。
        /// includeInactive が false のとき、非アクティブな GameObject は対象外です。
        /// </summary>
        private static Component[] GetComponents(Transform t, Type targetType, bool includeInactive)
        {
            if (includeInactive || t.gameObject.activeInHierarchy)
                return t.GetComponents(targetType);

            return Array.Empty<Component>();
        }
    }
}
#endif
