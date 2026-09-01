using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using VGC.UIExtension.Runtime;
using VRC.SDK3.Components.Editor;

namespace VGC.UIExtension.Editor
{
    public static class ButtonExtensionEditor
    {
        [MenuItem("GameObject/UI/ButtonExtension(VGC)", false, 9)]
        public static void AddButton(MenuCommand menuCommand)
        {
            GameObject go = TMP_DefaultControls.CreateButton(GetStandardResourcesByReflection());
            go.name = "ButtonExtension";
            
            TMP_Text textComponent = go.GetComponentInChildren<TMP_Text>();
            textComponent.fontSize = 24;

            go.AddComponent<ButtonExtension>();
            PlaceUIElementRootByReflection(go, menuCommand);
        }
        
        private static TMP_DefaultControls.Resources GetStandardResourcesByReflection()
        {
            var type = typeof(VRCTMP);

            var method = type.GetMethod(
                "GetStandardResources",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            return (TMP_DefaultControls.Resources)method!.Invoke(null, null);
        }
        
        private static void PlaceUIElementRootByReflection(GameObject element, MenuCommand menuCommand)
        {
            var type = typeof(VRCTMP);

            var method = type.GetMethod(
                "PlaceUIElementRoot",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            method!.Invoke(null, new object[] { element, menuCommand });
        }
    }
}
