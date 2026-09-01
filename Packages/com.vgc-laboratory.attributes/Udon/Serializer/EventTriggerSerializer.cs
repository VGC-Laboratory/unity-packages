#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UdonSharp;
using UdonSharpEditor;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using VRC.Udon;

namespace VGC.Attributes.Udon.Runtime
{
    public static class EventTriggerSerializer
    {
        public static void AddSendCustomEvent(
            EventTrigger trigger,
            EventTriggerType type,
            UdonSharpBehaviour target,
            string eventName)
        {
            var udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(target);
            var so = new SerializedObject(trigger);
            var delegates = so.FindProperty("m_Delegates");
            
            var entry = GetEntry(delegates, type);
            bool isCreateEntry = entry == null;
            if (isCreateEntry)
            {
                // Entry追加
                delegates.InsertArrayElementAtIndex(delegates.arraySize);
                entry = delegates.GetArrayElementAtIndex(delegates.arraySize - 1);
                
                // eventID設定
                entry.FindPropertyRelative("eventID").enumValueIndex = (int)type;
            }
            
            // callback取得
            var callback = entry.FindPropertyRelative("callback");
            var calls = callback.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            // 新規作成Entryの場合は、ゴミを削除
            if(isCreateEntry)
                calls.ClearArray();
            
            // 重複登録の場合はスキップ
            if (ExistsCall(calls, udonBehaviour, eventName))
            {
                Debug.LogError($"EventTrigger callback already exists: {eventName}", udonBehaviour);
                return;
            }
            
            // Call追加
            calls.InsertArrayElementAtIndex(calls.arraySize);
            var call = calls.GetArrayElementAtIndex(calls.arraySize - 1);
            call.FindPropertyRelative("m_Target").objectReferenceValue = udonBehaviour;
            call.FindPropertyRelative("m_MethodName").stringValue = "SendCustomEvent";
            call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.String;
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2;

            // 引数
            var args = call.FindPropertyRelative("m_Arguments");
            args.FindPropertyRelative("m_StringArgument").stringValue = eventName;

            // Assembly情報
            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
                typeof(UdonBehaviour).AssemblyQualifiedName;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(trigger);
        }

        public static void RemoveAllSendCustomEvent(
            EventTrigger trigger,
            EventTriggerType type,
            UdonSharpBehaviour target,
            string eventName)
        {
            var udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(target);
            var so = new SerializedObject(trigger);
            var delegates = so.FindProperty("m_Delegates");

            for (int i = delegates.arraySize - 1; i >= 0; i--)
            {
                var entry = delegates.GetArrayElementAtIndex(i);
                var eventID = entry.FindPropertyRelative("eventID");

                if (eventID.enumValueIndex != (int)type)
                    continue;

                var callback = entry.FindPropertyRelative("callback");
                var calls = callback.FindPropertyRelative("m_PersistentCalls.m_Calls");
                
                for (int j = calls.arraySize - 1; j >= 0; j--)
                {
                    var call = calls.GetArrayElementAtIndex(j);

                    var targetProp = call.FindPropertyRelative("m_Target");
                    var methodProp = call.FindPropertyRelative("m_MethodName");
                    var argProp = call.FindPropertyRelative("m_Arguments")
                                      .FindPropertyRelative("m_StringArgument");

                    if (targetProp.objectReferenceValue == udonBehaviour &&
                        methodProp.stringValue == nameof(udonBehaviour.SendCustomEvent) &&
                        argProp.stringValue == eventName)
                    {
                        // ObjectReferenceを持つ要素へのDeleteArrayElementAtIndexは
                        // 1回目が「nullにするだけ」になりゴミが残るため、先にnullを入れておく
                        targetProp.objectReferenceValue = null;
                        calls.DeleteArrayElementAtIndex(j);
                    }
                }

                // callsが空ならEntryごと削除
                if (calls.arraySize == 0)
                {
                    delegates.DeleteArrayElementAtIndex(i);
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(trigger);
        }

        public static void RemoveAllEvent(EventTrigger trigger, EventTriggerType type)
        {
            var so = new SerializedObject(trigger);
            var delegates = so.FindProperty("m_Delegates");

            for (int i = delegates.arraySize - 1; i >= 0; i--)
            {
                var entry = delegates.GetArrayElementAtIndex(i);
                var eventID = entry.FindPropertyRelative("eventID");

                if (eventID.enumValueIndex == (int)type)
                {
                    delegates.DeleteArrayElementAtIndex(i);
                }
            }

            so.ApplyModifiedProperties();
        }

        private static SerializedProperty GetEntry(SerializedProperty delegates, EventTriggerType type)
        {
            SerializedProperty entry = null;

            for (int i = 0; i < delegates.arraySize; i++)
            {
                var e = delegates.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("eventID").enumValueIndex == (int)type)
                {
                    entry = e;
                    break;
                }
            }
            
            return entry;
        }
        
        private static bool ExistsCall(SerializedProperty calls, UdonBehaviour target, string eventName)
        {
            for (int i = 0; i < calls.arraySize; i++)
            {
                var c = calls.GetArrayElementAtIndex(i);

                var t = c.FindPropertyRelative("m_Target").objectReferenceValue;
                var method = c.FindPropertyRelative("m_MethodName").stringValue;
                var arg = c.FindPropertyRelative("m_Arguments")
                           .FindPropertyRelative("m_StringArgument").stringValue;

                if (t == target &&
                    method == "SendCustomEvent" &&
                    arg == eventName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
#endif
