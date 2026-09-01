#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    public static class ExecutorSharedCache
    {
        private static readonly FindObjectsCache Cache = new();
        private static readonly Dictionary<Transform, string> HierarchyIndexCache = new();

        public static T[] GetTargets<T>(bool includeInactive = true)  where T : MonoBehaviour
        {
            return Cache.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }
        
        public static UnityEngine.Object[] GetTargets(Type type, bool includeInactive = true)
        {
            return Cache.FindObjectsByType(type,
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }
        
        /// <summary>
        /// "0001.0003.0002" みたいな文字列が返る想定
        /// </summary>
        public static string GetHierarchyIndex(Transform target)
        {
            if (HierarchyIndexCache.TryGetValue(target, out var cached))
                return cached;

            var current = target;
            var indices = new List<int>();

            while (current != null)
            {
                indices.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            indices.Reverse();

            var result = string.Join(".", indices.Select(i => i.ToString("D4")));
            HierarchyIndexCache[target] = result;

            return result;
        }

        public static void Clear()
        {
            Cache.Reset();
            HierarchyIndexCache.Clear();
        }
    }
}
#endif