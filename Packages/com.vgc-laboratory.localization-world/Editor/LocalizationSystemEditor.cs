using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace VGC.Localization.World
{
    [CustomEditor(typeof(LocalizationSystem))]
    public class LocalizationSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            base.OnInspectorGUI();

            EditorGUILayout.Space();
            var languages = LanguageSetting.Available;
            EditorGUILayout.LabelField("Languages", string.Join(", ", languages.Select(LanguageNames.NativeName)));
            EditorGUILayout.LabelField("Default", LanguageNames.NativeName(LanguageSetting.Default));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Project Settings"))
                    SettingsService.OpenProjectSettings("Project/VGC/Localization");
                if (GUILayout.Button("Bake Now"))
                    LocalizationBaker.BakeOpenScenes();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);
            LocalizeUIHelper.InspectorGUI();
        }

        [MenuItem("GameObject/VGC/Localization System", false, 10)]
        private static void Create(MenuCommand command)
        {
            var go = new GameObject("VGC_LocalizationSystem");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Localization System");
            UdonSharpUndo.AddComponent(go, typeof(LocalizationSystem));
            Selection.activeGameObject = go;
        }
    }
}
