using UnityEngine;

namespace VGC.Attributes.Runtime
{
    public interface IExecutorClassAttribute
    {
#if UNITY_EDITOR
        bool Execute(MonoBehaviour monoBehaviour, bool registerUndo);
#endif
    }
}