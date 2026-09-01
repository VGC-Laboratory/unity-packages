#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    public class FindObjectsCache
    {
        private readonly Dictionary<Type, Object[]> _includeInactiveNoneTargets = new();
        private readonly Dictionary<Type, Object[]> _includeInactiveInstanceIdTargets = new();
        private readonly Dictionary<Type, Object[]> _excludeInactiveNoneTargets = new();
        private readonly Dictionary<Type, Object[]> _excludeInactiveInstanceIdTargets = new();

        public T[] FindObjectsByType<T>(
            FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode) where T : Object
            => (T[])FindObjectsByType(typeof(T), findObjectsInactive, sortMode);
        
        public Object[] FindObjectsByType(
            Type type,
            FindObjectsInactive findObjectsInactive,
            FindObjectsSortMode sortMode)
        {
            if (findObjectsInactive == FindObjectsInactive.Include)
            {
                return sortMode switch
                {
                    FindObjectsSortMode.None =>
                        IncludeInactiveNone(type),

                    FindObjectsSortMode.InstanceID =>
                        IncludeInactiveInstanceId(type),

                    _ => throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, null)
                };
            }

            return sortMode switch
            {
                FindObjectsSortMode.None =>
                    ExcludeInactiveNone(type),

                FindObjectsSortMode.InstanceID =>
                    ExcludeInactiveInstanceId(type),

                _ => throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, null)
            };
        }

        private Object[] IncludeInactiveNone(Type type)
        {
            if (_includeInactiveNoneTargets.TryGetValue(type, out var targets))
                return targets;

            targets = Object.FindObjectsByType(
                type,
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            _includeInactiveNoneTargets[type] = targets;
            return targets;
        }

        private Object[] IncludeInactiveInstanceId(Type type)
        {
            if (_includeInactiveInstanceIdTargets.TryGetValue(type, out var targets))
                return targets;

            targets = (Object[])IncludeInactiveNone(type).Clone();

            Array.Sort(targets, CompareInstanceId);

            _includeInactiveInstanceIdTargets[type] = targets;
            return targets;
        }

        private Object[] ExcludeInactiveNone(Type type)
        {
            if (_excludeInactiveNoneTargets.TryGetValue(type, out var targets))
                return targets;

            var source = IncludeInactiveNone(type);
            var activeTargets = new List<Object>(source.Length);

            foreach (var target in source)
            {
                if (IsActive(target))
                    activeTargets.Add(target);
            }

            // List<Object>.ToArray() は実行時型が Object[] になり、
            // FindObjectsByType<T>() 側の (T[]) キャストが InvalidCastException になるため、
            // 要素型 type の配列として確保する
            targets = (Object[])Array.CreateInstance(type, activeTargets.Count);
            activeTargets.CopyTo(targets);

            _excludeInactiveNoneTargets[type] = targets;
            return targets;
        }

        private Object[] ExcludeInactiveInstanceId(Type type)
        {
            if (_excludeInactiveInstanceIdTargets.TryGetValue(type, out var targets))
                return targets;

            targets = (Object[])ExcludeInactiveNone(type).Clone();

            Array.Sort(targets, CompareInstanceId);

            _excludeInactiveInstanceIdTargets[type] = targets;
            return targets;
        }

        private int CompareInstanceId(Object a, Object b)
        {
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }

        private bool IsActive(Object target)
        {
            if (target is Component component)
                return component.gameObject.activeInHierarchy;

            if (target is GameObject gameObject)
                return gameObject.activeInHierarchy;

            return true;
        }

        public void Reset()
        {
            _includeInactiveNoneTargets.Clear();
            _includeInactiveInstanceIdTargets.Clear();
            _excludeInactiveNoneTargets.Clear();
            _excludeInactiveInstanceIdTargets.Clear();
        }
    }
}
#endif
