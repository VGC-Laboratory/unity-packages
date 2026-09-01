#if UDONSHARP
using System;
using UdonSharp;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using VGC.Attributes.Runtime;
using System.Reflection;
using UnityEngine.UI;
#endif

namespace VGC.Attributes.Udon.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AddButtonSendCustomEventFieldAttribute : AutoPopulateFieldAttribute
    {
        private Type TargetUdonType { get; }
        private ExecutorScope TargetUdonScope { get; }
        private string EventName { get; }
        private bool AddIndex { get; }

        public AddButtonSendCustomEventFieldAttribute(
            string eventName,
            Type targetUdonType = null,
            ExecutorScope targetUdonScope = ExecutorScope.Self,
            bool addIndex = true,
            Type targetType = null,
            ExecutorScope scope = ExecutorScope.Self,
            ExecutorOrder order = ExecutorOrder.Hierarchy,
            Type anchorType = null,
            bool includeInactive = true,
            bool onlyEnabled = false,
            bool required = false) : base(targetType, scope, order, anchorType, includeInactive, onlyEnabled, required)
        {
            TargetUdonType = targetUdonType;
            TargetUdonScope = targetUdonScope;
            EventName = eventName;
            AddIndex = addIndex;
        }
#if UNITY_EDITOR
        public override bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            var targetUdon = TargetUdonType == null ? monoBehaviour as UdonSharpBehaviour : ExecuteScopeHelper.FindTarget(monoBehaviour.transform, TargetUdonType, TargetUdonScope) as UdonSharpBehaviour;
            if(targetUdon == null)
                return false;
            
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"AutoPopulate:{field.Name}, {monoBehaviour.name}");
            }
            
            var targetType = ResolveTargetType(field);
            AutoPopulateUtility.ExecuteField(monoBehaviour,
                field,
                targetType,
                Scope,
                Order,
                ResolveAnchorType(targetType),
                IncludeInactive,
                OnlyEnabled,
                Required,
                out var property,
                out var changed);

            if(property == null)
                return changed;

            if (property.isArray)
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    var current = property.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (current is Button button)
                    {
                        var eventName = AddIndex ? $"{EventName}_{i}" : EventName;
                        ButtonSerializer.RemoveAllSendCustomEvent(button, targetUdon, eventName);
                        ButtonSerializer.AddSendCustomEvent(button, targetUdon, eventName);
                    }
                }
            }
            else
            {
                if (property.objectReferenceValue is Button button)
                {
                    ButtonSerializer.RemoveAllSendCustomEvent(button, targetUdon, EventName);
                    ButtonSerializer.AddSendCustomEvent(button, targetUdon, EventName);
                }
            }

            // Buttonへの配線はButton側のコンポーネントを書き換えるもので、
            // ButtonSerializer内で SetDirty 済み。CopyProxyToUdon の要否は
            // フィールドを実際に書き換えたかどうかだけで決まる
            return changed;
        }
        
        [CustomPropertyDrawer(typeof(AddButtonSendCustomEventFieldAttribute))]
        public class AddButtonSendCustomEventPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var attr = (AddButtonSendCustomEventFieldAttribute)attribute;
                if (attr.Scope == ExecutorScope.NearestParent || attr.Scope == ExecutorScope.NearestParentHierarchy)
                {
                    IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, $"AddButtonSendCustomEvent({attr.Scope})");
                }
                else
                {
                    var targetType = attr.ResolveTargetType(property.serializedObject.targetObject.GetType());
                    IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, $"AddButtonSendCustomEvent({attr.Scope}){attr.ResolveAnchorType(targetType)}");
                }
            }
        }
#endif
    }
}
#endif