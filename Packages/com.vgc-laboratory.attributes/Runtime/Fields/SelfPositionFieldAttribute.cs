using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace VGC.Attributes.Runtime
{
    public enum SelfPositionState
    {
        X, Y, Z, All
    }
    /// <summary>
    /// ビルド時に座標の初期値を自動的にアタッチします。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SelfPositionFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
        SelfPositionState State { get; }
        public SelfPositionFieldAttribute()
        {
            State = SelfPositionState.All;
        }
        
        public SelfPositionFieldAttribute(SelfPositionState state)
        {
            State = state;
        }
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"SelfPosition:{field.Name}, {monoBehaviour.name}");
            }
            
            ExecuteField(monoBehaviour, field);
            return true;
        }
        
        private void ExecuteField(MonoBehaviour monoBehaviour, FieldInfo field)
        {
            switch (State)
            {
                case SelfPositionState.X:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.position.x);
                    break;
                case SelfPositionState.Y:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.position.y);
                    break;
                case SelfPositionState.Z:
                    field.SetValue(monoBehaviour, monoBehaviour.transform.position.z);
                    break;
                case SelfPositionState.All:
                {
                    if (field.FieldType != typeof(Vector3))
                    {
                        Debug.LogError("[SelfPositionAttribute] Not Vector3 Field", monoBehaviour.gameObject);
                        break;
                    }
                        
                    field.SetValue(monoBehaviour, monoBehaviour.transform.position);
                    break;
                }
            }
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SelfPositionFieldAttribute))]
    public class SelfPositionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "SelfPosition");
        }
    }
#endif
}