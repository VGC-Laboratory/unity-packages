using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Gimmicks_MA.Lib.Localization
{
    public class LocalizeConfigBehaviour : MonoBehaviour
    {
    }
    
#if UNITY_EDITOR
    public static class LocalizeUIHelper
    {
        public static void InspectorGUI()
        {
            var config = LocalizeConfig.instance;

            EditorGUI.BeginChangeCheck();

            var newLang = (Language)EditorGUILayout.EnumPopup("Language", config.Language);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Change Language");
                config.Language = newLang;
            }
        }
    }
    
    [CustomEditor(typeof(LocalizeConfigBehaviour), true)]
    public class LocalizeConfigBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LocalizeUIHelper.InspectorGUI();
            base.OnInspectorGUI();
        }
    }
#endif
}