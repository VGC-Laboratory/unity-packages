using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace VGC.Attributes.Runtime
{
    public enum SelfLocalScaleState
    {
        X, Y, Z, All
    }
    /// <summary>
    /// ビルド時に座標の初期値を自動的にアタッチします。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfLocalScaleFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
        SelfLocalScaleState State { get; }
        public SelfLocalScaleFieldAttribute()
        {
            State = SelfLocalScaleState.All;
        }
        
        public SelfLocalScaleFieldAttribute(SelfLocalScaleState state)
        {
            State = state;
        }
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"SelfLocalScale:{field.Name}, {monoBehaviour.name}");
            }
            
            ExecuteField(monoBehaviour, field);
            return true;
        }
        
        private void ExecuteField(MonoBehaviour monoBehaviour, FieldInfo field)
        {
            switch (State)
            {
                case SelfLocalScaleState.X:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.localScale.x);
                    break;
                case SelfLocalScaleState.Y:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.localScale.y);
                    break;
                case SelfLocalScaleState.Z:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.localScale.z);
                    break;
                case SelfLocalScaleState.All:
                {
                    if (field.FieldType != typeof(Vector3))
                    {
                        Debug.LogError("[SelfLocalScaleAttribute] Not Vector3 Field", monoBehaviour.gameObject);
                        break;
                    }
                        
                    field.SetValue(monoBehaviour, monoBehaviour.transform.localScale);
                    break;
                }
            }
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SelfLocalScaleFieldAttribute))]
    public class SelfLocalScaleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "SelfLocalScale");
        }
    }
#endif
}