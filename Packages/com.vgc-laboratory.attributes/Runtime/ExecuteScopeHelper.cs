using System;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// <see cref="ExecutorScope"/> 指定での単体検索
    /// </summary>
    public static class ExecuteScopeHelper
    {
        public static UnityEngine.Object FindTarget(Transform t, Type targetType, ExecutorScope scope)
        {
            switch (scope)
            {
                case ExecutorScope.Self:
                    return t.GetComponent(targetType);

                case ExecutorScope.Parents:
                    return t.GetComponentInParent(targetType, true);

                case ExecutorScope.Parent:
                {
                    var parent = t.parent;
                    if(parent != null)
                        return parent.GetComponent(targetType);
                    return null;
                }

                case ExecutorScope.NearestParent:
                {
                    var current = t.parent;
                    while (current != null)
                    {
                        var comp = current.GetComponent(targetType);
                        if (comp != null)
                            return comp;

                        current = current.parent;
                    }
                    return null;
                }

                case ExecutorScope.Children:
                    return t.GetComponentInChildren(targetType, true);

                case ExecutorScope.Scene:
                    return UnityEngine.Object.FindFirstObjectByType(targetType);

                default:
                    return null;
            }
        }
    }
}
