#if UDONSHARP
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
using VGC.Attributes.Runtime;
#endif

namespace VGC.Attributes.Udon.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AddEventTriggerSendCustomEventFieldAttribute : PropertyAttribute, IExecutorClassAttribute
    {
        private EventTriggerType EventTriggerType { get; }
        private string EventName { get; }
        public AddEventTriggerSendCustomEventFieldAttribute(EventTriggerType eventTriggerType, string eventName)
        {
            EventTriggerType = eventTriggerType;
            EventName = eventName;
        }
        
#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, bool registerUndo)
        {
            var udon = monoBehaviour as UdonSharpBehaviour;
            if(!udon)
                return false;
            
            var trigger = monoBehaviour.GetComponent<EventTrigger>();
            if(!trigger)
                return false;
            
            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"AddEventTriggerSendCustomEvent:{monoBehaviour.name}");
            }
            
            EventTriggerSerializer.RemoveAllSendCustomEvent(trigger, EventTriggerType, udon, EventName);
            EventTriggerSerializer.AddSendCustomEvent(trigger, EventTriggerType, udon, EventName);

            // 変更対象はUdonBehaviourではなくEventTriggerコンポーネント側で、
            // Serializer内で SetDirty 済み。CopyProxyToUdon は不要なので false を返す
            return false;
        }
#endif
    }
}
#endif