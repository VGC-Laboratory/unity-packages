using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace VGC.Attributes.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AutoAssignIndexFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
        Type AnchorType { get; }
        ExecutorScope Scope { get; }
        ExecutorOrder Order { get; }
        
        public AutoAssignIndexFieldAttribute(
            Type anchorType = null,
            ExecutorScope scope = ExecutorScope.NearestParent,
            ExecutorOrder order = ExecutorOrder.Hierarchy)
        {
            AnchorType = anchorType;
            Scope = scope;
            Order = order;
        }
        
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"AutoAssignIndex:{field.Name}, {monoBehaviour.name}");
            }
            
            if (GetCustomAttribute(field, typeof(AutoAssignIndexFieldAttribute)) is AutoAssignIndexFieldAttribute)
            {
                AutoAssignIndexUtility.ExecuteField(monoBehaviour, AnchorType, field, Scope, Order);
                return true;
            }
            return false;
        }
        
        [CustomPropertyDrawer(typeof(AutoAssignIndexFieldAttribute))]
        public class AutoAssignIndexPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var attr = (AutoAssignIndexFieldAttribute)attribute;
                IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, $"AutoAssignIndex({attr.Scope.ToString()})");
            }
        }
#endif
    }
}