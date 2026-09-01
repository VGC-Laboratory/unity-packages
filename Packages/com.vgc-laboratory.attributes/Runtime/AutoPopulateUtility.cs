#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    public static class AutoPopulateUtility
    {
        public static void ExecuteField(
            MonoBehaviour target,
            FieldInfo field,
            Type targetType,
            ExecutorScope scope,
            ExecutorOrder order,
            Type anchorType,
            bool includeInactive,
            bool onlyEnabled,
            bool required,
            out SerializedProperty property,
            out bool changed)
        {
            // 実際に値を書き換えたときだけ true。
            // 呼び出し側はこれを Execute の戻り値にして、
            // 変更が無いときの CopyProxyToUdon を省く
            changed = false;

            if(targetType == null || target== null || field == null)
            {
                property = null;
                return;
            }

            // 対象のMonoBehaviourを取得
            MonoBehaviour[] allMonoBehaviours;
            switch (scope)
            {
                case ExecutorScope.Self:
                {
                    allMonoBehaviours = GetComponents(target.gameObject, includeInactive);
                    break;
                }
                case ExecutorScope.Children:
                {
                    allMonoBehaviours = target.GetComponentsInChildren<MonoBehaviour>(includeInactive);
                    break;
                }
                case ExecutorScope.ChildrenExcludeSelf:
                {
                    var exclude = target.GetComponents<MonoBehaviour>();
                    allMonoBehaviours = target.GetComponentsInChildren<MonoBehaviour>(includeInactive)
                                              .Where(m => !exclude.Contains(m))
                                              .ToArray();
                    break;
                }
                case ExecutorScope.Parents:
                {
                    allMonoBehaviours = target.GetComponentsInParent<MonoBehaviour>(includeInactive);
                    break;
                }
                case ExecutorScope.Parent:
                {
                    var parent = target.transform.parent;
                    allMonoBehaviours = parent
                        ? GetComponents(parent.gameObject, includeInactive)
                        : Array.Empty<MonoBehaviour>();
                    break;
                }
                case ExecutorScope.ParentHierarchy:
                {
                    var parent = target.transform.parent;
                    allMonoBehaviours = parent
                        ? parent.GetComponentsInChildren<MonoBehaviour>(includeInactive)
                        : target.GetComponentsInChildren<MonoBehaviour>(includeInactive);
                    break;
                }
                case ExecutorScope.NearestParent:
                {
                    var parent = FindNearestParentWithComponent(target.transform, anchorType);
                    allMonoBehaviours = parent
                        ? GetComponents(parent.gameObject, includeInactive)
                        : Array.Empty<MonoBehaviour>();
                    break;
                }
                case ExecutorScope.NearestParentHierarchy:
                {
                    var parent = FindNearestParentWithComponent(target.transform, anchorType);

                    allMonoBehaviours = parent != null
                        ? parent.GetComponentsInChildren<MonoBehaviour>(includeInactive)
                        : Array.Empty<MonoBehaviour>();
                    break;
                }
                case ExecutorScope.Root:
                {
                    allMonoBehaviours = GetComponents(target.transform.root.gameObject, includeInactive);
                    break;
                }
                case ExecutorScope.RootHierarchy:
                {
                    allMonoBehaviours = target.transform.root.GetComponentsInChildren<MonoBehaviour>(includeInactive);
                    break;
                }
                case ExecutorScope.Scene:
                default:
                {
                    allMonoBehaviours = ExecutorSharedCache.GetTargets<MonoBehaviour>(includeInactive);
                    break;
                }
            }
            
            if(onlyEnabled)
                allMonoBehaviours = allMonoBehaviours.Where(m => m.enabled).ToArray();
            
            // targetType (ITarget等) を実装しているものを抽出
            var foundObjects = FindObjects();
            UnityEngine.Object[] FindObjects()
            {
                var list = new List<Component>();

                foreach (var m in allMonoBehaviours)
                {
                    if (!targetType.IsAssignableFrom(m.GetType()))
                        continue;

                    list.Add(m);
                }

                if (order == ExecutorOrder.Hierarchy)
                {
                    list.Sort((a, b) =>
                        // ReSharper disable once StringCompareToIsCultureSpecific
                        ExecutorSharedCache.GetHierarchyIndex(a.transform)
                                           .CompareTo(ExecutorSharedCache.GetHierarchyIndex(b.transform)));
                }

                return list.Cast<UnityEngine.Object>().ToArray();
            }
            
            // 必須状態で見つからなかった場合の警告表示
            if (required && foundObjects.Length == 0)
            {
                Debug.LogWarning($"<color=#FF9900>[VGC.AutoPopulate] Required target was not found.\n" +
                                 $"Component: {target.GetType().Name}\n" +
                                 $"Field: {field.Name}\n" +
                                 $"Type: {targetType.FullName}\n" +
                                 $"Scope: {scope}</color>"
                    , target);
            }
            
            var serializedObject = new SerializedObject(target);
            property = serializedObject.FindProperty(field.Name);

            if (property == null) return;

            if (property.isArray)
            {
                bool needsUpdate = false;
                if (property.arraySize != foundObjects.Length)
                {
                    needsUpdate = true;
                }
                else
                {
                    for (int i = 0; i < foundObjects.Length; i++)
                    {
                        var current = property.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (current != foundObjects[i])
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                }

                if (needsUpdate)
                {
                    property.arraySize = foundObjects.Length;
                    for (int i = 0; i < foundObjects.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = foundObjects[i];
                    }
                    serializedObject.ApplyModifiedProperties();
                    changed = true;
                }
            }
            else
            {
                // 単一オブジェクトの場合
                if (foundObjects.Length > 0 && field.FieldType.IsAssignableFrom(foundObjects[0].GetType()))
                {
                    if (property.objectReferenceValue != foundObjects[0])
                    {
                        property.objectReferenceValue = foundObjects[0];
                        serializedObject.ApplyModifiedProperties();
                        changed = true;
                    }
                }
                else if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // 型が合わない場合は null にする
                    if (property.objectReferenceValue != null)
                    {
                        property.objectReferenceValue = null;
                        serializedObject.ApplyModifiedProperties();
                        changed = true;
                    }
                }
            }
        }
        
        private static Transform FindNearestParentWithComponent(
            Transform target,
            Type targetType,
            bool includeSelf = false)
        {
            var current = includeSelf ? target : target.parent;

            while (current != null)
            {
                var components = current.GetComponents<MonoBehaviour>();

                foreach (var component in components)
                {
                    if (targetType.IsAssignableFrom(component.GetType()))
                    {
                        return current;
                    }
                }

                current = current.parent;
            }

            return null;
        }
        
        private static MonoBehaviour[] GetComponents(
            GameObject gameObject,
            bool includeInactive)
        {
            if (includeInactive || gameObject.activeInHierarchy)
                return gameObject.GetComponents<MonoBehaviour>();

            return Array.Empty<MonoBehaviour>();
        }
    }
}
#endif