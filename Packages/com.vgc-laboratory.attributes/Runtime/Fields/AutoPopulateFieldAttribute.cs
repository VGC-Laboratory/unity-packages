using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
#endif

namespace VGC.Attributes.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoPopulateFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
        /// <summary>
        /// 検索する型やインターフェースを指定します。
        /// nullの場合はフィールドの型を使用します。
        /// </summary>
        protected Type TargetType { get; }
        protected ExecutorScope Scope { get; }
        protected ExecutorOrder Order { get; }
        
        /// <summary>
        /// NearestParent系Scopeで検索基準となる親オブジェクトの型を指定します。
        /// nullの場合は解決後のTargetTypeを使用します。
        /// </summary>
        protected Type AnchorType { get; }
        
        protected bool IncludeInactive { get; }
        
        /// <summary>
        /// 有効化されているComponentのみ対象にします(GameObjectのActive状態は含みません)
        /// </summary>
        protected bool OnlyEnabled { get; }
        
        /// <summary>
        /// 値が見つからなかった場合に警告を表示します。
        /// </summary>
        protected bool Required { get; }

        public AutoPopulateFieldAttribute(Type targetType = null, ExecutorScope scope = ExecutorScope.Scene, ExecutorOrder order = ExecutorOrder.None, Type anchorType = null, bool includeInactive = true, bool onlyEnabled = false, bool required = false)
        {
            TargetType = targetType;
            Scope = scope;
            Order = order;
            AnchorType = anchorType;
            IncludeInactive = includeInactive;
            OnlyEnabled = onlyEnabled;
            Required = required;
        }

#if UNITY_EDITOR
        protected Type ResolveTargetType(FieldInfo field)
        {
            return ResolveTargetType(field?.FieldType);
        }
        
        protected Type ResolveTargetType(Type type)
        {
            if (TargetType != null)
                return TargetType;

            if (type == null)
                return null;

            return ResolveElementType(type);
        }
        
        protected static Type ResolveElementType(Type type)
        {
            return ObjectFieldAssignUtility.ResolveElementType(type);
        }
        
        protected Type ResolveAnchorType(Type targetType)
        {
            return AnchorType ?? targetType;
        }
        
        public virtual bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
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
                out _,
                out var changed);

            // 常にtrueを返すと、値が変わっていなくても全UdonSharpBehaviourに
            // CopyProxyToUdon が走る。実際に書き換えたときだけ返す
            return changed;
        }
        
        [CustomPropertyDrawer(typeof(AutoPopulateFieldAttribute))]
        public class AutoPopulatePropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var attr = (AutoPopulateFieldAttribute)attribute;
                if (attr.Scope == ExecutorScope.NearestParent || attr.Scope == ExecutorScope.NearestParentHierarchy)
                {
                    IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, $"AutoPopulate({attr.Scope})");
                }
                else
                {
                    var targetType = attr.ResolveTargetType(property.serializedObject.targetObject.GetType());
                    IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, $"AutoPopulate({attr.Scope}){attr.ResolveAnchorType(targetType)}");
                }
            }
        }
#endif
    }
}