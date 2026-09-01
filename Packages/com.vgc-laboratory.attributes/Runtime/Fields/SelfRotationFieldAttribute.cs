using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace VGC.Attributes.Runtime 
{
    /// <summary>
    /// ビルド時にRotationの初期値を自動的にアタッチします。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfRotationFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (field.FieldType != typeof(Quaternion))
            {
                Debug.LogError("[SelfRotationAttribute] Not Quaternion Field", monoBehaviour.gameObject);
                return false;
            }
            
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"SelfRotation:{field.Name}, {monoBehaviour.name}");
            }
            
            field.SetValue(monoBehaviour, monoBehaviour.transform.rotation);
            return true;
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SelfRotationFieldAttribute))]
    public class SelfInitRotationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "SelfRotation");
        }
    }
#endif
}