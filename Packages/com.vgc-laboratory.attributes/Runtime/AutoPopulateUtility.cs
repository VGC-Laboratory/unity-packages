#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
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

            // 対象のComponentを取得。
            // ExecutorScope の解釈は ExecuteScopeHelper に集約してある
            var foundObjects = ExecuteScopeHelper.FindTargets(
                target.transform,
                targetType,
                scope,
                anchorType,
                includeInactive);

            // Behaviour 以外(Transform 等)は enabled を持たないので対象のまま残す
            if (onlyEnabled)
                foundObjects = foundObjects.Where(c => !(c is Behaviour b) || b.enabled).ToArray();

            // Hierarchy順
            if (order == ExecutorOrder.Hierarchy)
            {
                foundObjects = foundObjects
                               // ReSharper disable once StringCompareToIsCultureSpecific
                               .OrderBy(c => ExecutorSharedCache.GetHierarchyIndex(c.transform))
                               .ToArray();
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
    }
}
#endif
