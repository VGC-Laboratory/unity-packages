using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.Linq;
#endif

namespace VGC.Attributes.Runtime
{
    public class FindFirstFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"FindFirst:{field.Name}, {monoBehaviour.name}");
            }
            
            field.SetValue(monoBehaviour, GetTargetObject(field.FieldType));
            return true;
        }
        
        private Object GetTargetObject(Type type)
        {
            if (type == typeof(GameObject))
            {
                return SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault();
            }
            
            return ExecutorSharedCache.GetTargets(type).FirstOrDefault();
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FindFirstFieldAttribute))]
    public class FindFirstDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "FindFirst");
        }
    }
#endif
}
