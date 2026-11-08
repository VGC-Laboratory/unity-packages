using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VGC.Localization
{
    public class LocalizedBehaviour : MonoBehaviour
    {
    }
    
#if UNITY_EDITOR
    public static class LocalizeUIHelper
    {
        public static void InspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            var newLang = (Language)EditorGUILayout.EnumPopup("Language", LanguageSetting.Current);

            if (EditorGUI.EndChangeCheck())
            {
                LanguageSetting.Current = newLang;
            }
        }
    }
    
    [CustomEditor(typeof(LocalizedBehaviour), true)]
    public class LocalizedBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LocalizeUIHelper.InspectorGUI();
            base.OnInspectorGUI();
        }
    }
#endif
}