using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Attributes.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SelfFieldAttribute : AutoPopulateFieldAttribute
    {
        public SelfFieldAttribute() : base(scope: ExecutorScope.Self)
        {
        }
        
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(SelfFieldAttribute))]
        public class SelfPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "Self");
            }
        }
#endif
    }
}