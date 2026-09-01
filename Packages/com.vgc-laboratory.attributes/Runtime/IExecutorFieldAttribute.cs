using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace VGC.Attributes.Runtime
{
    public interface IExecutorFieldAttribute
    {
#if UNITY_EDITOR
        bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo);
        
        public static void ReadOnlyTagGUI(Rect position, SerializedProperty property, GUIContent label, string attributeName)
        {
            var tempPos = position;
            var content = new GUIContent($"{attributeName}:");
            var labelWidth = GUI.skin.label.CalcSize(content).x;
            position.width = labelWidth;
            EditorGUI.LabelField(position, content);
            position.x += labelWidth;
            position.width = tempPos.width - labelWidth;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
#endif
    }
}
