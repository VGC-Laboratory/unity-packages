using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace VGC.Localization
{
    /// <summary>インスペクタの先頭に言語の選択を出す MonoBehaviour の基底。</summary>
    public class LocalizedBehaviour : MonoBehaviour
    {
    }

#if UNITY_EDITOR
    public static class LocalizeUIHelper
    {
        /// <summary>エディタの表示言語を選ぶポップアップ(使用言語の一覧から選ぶ)。</summary>
        public static void InspectorGUI()
        {
            var languages = LanguageSetting.Available;
            if (languages.Count == 0)
                return;

            var current = LanguageSetting.Current;
            var index = Mathf.Max(0, languages.ToList().IndexOf(current));
            var names = languages.Select(LanguageNames.NativeName).ToArray();

            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.Popup("Language", index, names);
            if (EditorGUI.EndChangeCheck())
                LanguageSetting.Current = languages[next];
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
