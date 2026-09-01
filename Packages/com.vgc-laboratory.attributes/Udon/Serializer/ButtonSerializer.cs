#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;

namespace VGC.Attributes.Udon.Runtime
{
    public static class ButtonSerializer
    {
        public static void AddSendCustomEvent(
            Button button,
            UdonSharpBehaviour target,
            string eventName)
        {
            var udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(target);
            UnityEventTools.AddStringPersistentListener(button.onClick, udonBehaviour.SendCustomEvent, eventName);
            EditorUtility.SetDirty(button);
        }
        
        public static void RemoveAllSendCustomEvent(Button button, UdonSharpBehaviour target, string eventName)
        {
            var udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(target);

            var so = new SerializedObject(button);
            var onClickProp = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");

            for (int i = onClickProp.arraySize - 1; i >= 0; i--)
            {
                var element = onClickProp.GetArrayElementAtIndex(i);

                var targetProp = element.FindPropertyRelative("m_Target");
                var methodNameProp = element.FindPropertyRelative("m_MethodName");
                var argsProp = element.FindPropertyRelative("m_Arguments");
                var stringArgProp = argsProp.FindPropertyRelative("m_StringArgument");

                if (targetProp.objectReferenceValue == udonBehaviour &&
                    methodNameProp.stringValue == nameof(udonBehaviour.SendCustomEvent) &&
                    stringArgProp.stringValue == eventName)
                {
                    // ObjectReferenceを持つ要素へのDeleteArrayElementAtIndexは
                    // 1回目が「nullにするだけ」になりゴミが残るため、先にnullを入れておく
                    targetProp.objectReferenceValue = null;
                    onClickProp.DeleteArrayElementAtIndex(i);
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(button);
        }
        
        public static void RemoveAllEvent(Button button)
        {
            var evt = button.onClick;
            int count = evt.GetPersistentEventCount();

            for (int i = count - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(evt, i);
            }

            EditorUtility.SetDirty(button);
        }
    }
}
#endif