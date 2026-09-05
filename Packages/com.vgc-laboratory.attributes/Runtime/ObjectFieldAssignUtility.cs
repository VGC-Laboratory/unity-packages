#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// 解決済みのオブジェクト列を SerializedProperty 経由でフィールドへ書き込む共通処理。
    /// 検索方法（Hierarchy／Project）に依存しないので Executor 系の属性から共用する。
    /// </summary>
    public static class ObjectFieldAssignUtility
    {
        /// <summary>
        /// 配列・List フィールドなら要素型を、それ以外はその型自身を返す。
        /// </summary>
        public static Type ResolveElementType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];

            return type;
        }

        /// <summary>
        /// values をフィールドへ代入する。配列・List フィールドなら全件、単一フィールドなら先頭のみ。
        /// values が空なら配列は要素数0、単一フィールドは null になる。
        /// </summary>
        /// <param name="changed">実際に値を書き換えたときだけ true。呼び出し側はこれを Execute の戻り値にして、変更が無いときの CopyProxyToUdon を省く</param>
        public static void Apply(
            MonoBehaviour target,
            FieldInfo field,
            Object[] values,
            out SerializedProperty property,
            out bool changed)
        {
            changed = false;
            property = null;

            if (target == null || field == null || values == null)
                return;

            var serializedObject = new SerializedObject(target);
            property = serializedObject.FindProperty(field.Name);

            if (property == null) return;

            if (property.isArray)
            {
                bool needsUpdate = false;
                if (property.arraySize != values.Length)
                {
                    needsUpdate = true;
                }
                else
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        var current = property.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (current != values[i])
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                }

                if (needsUpdate)
                {
                    property.arraySize = values.Length;
                    for (int i = 0; i < values.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                    }
                    serializedObject.ApplyModifiedProperties();
                    changed = true;
                }
            }
            else
            {
                // 単一オブジェクトの場合
                if (values.Length > 0 && values[0] != null && field.FieldType.IsInstanceOfType(values[0]))
                {
                    if (property.objectReferenceValue != values[0])
                    {
                        property.objectReferenceValue = values[0];
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
