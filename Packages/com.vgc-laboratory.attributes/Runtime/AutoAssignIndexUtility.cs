#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VGC.Attributes.Runtime
{
    public static class AutoAssignIndexCache
    {
        public static readonly Dictionary<(Transform, Type, ExecutorScope), UnityEngine.Object> AnchorCache = new();

        /// <summary>
        /// 処理済みの(コンポーネント型, フィールド名)。
        /// 1回のExecuteFieldで同型の全インスタンスにIndexを割り振るため、
        /// 2個目以降のインスタンスでは再実行しない
        /// </summary>
        public static readonly HashSet<(Type, string)> ProcessedFields = new();

        public static void Clear()
        {
            AnchorCache.Clear();
            ProcessedFields.Clear();
        }
    }
    
    public static class AutoAssignIndexUtility
    {
        internal static void ExecuteField(
            MonoBehaviour target,
            Type anchorType,
            FieldInfo field,
            ExecutorScope scope,
            ExecutorOrder order)
        {
            var targetType = target.GetType();

            // このメソッドは同型の全インスタンスにIndexを割り振るため、
            // AttributeExecutorがN個のインスタンスを走査するとN回同じ処理が走る(O(N^2))。
            // 最初の1回だけ実行する
            if (!AutoAssignIndexCache.ProcessedFields.Add((targetType, field.Name)))
                return;

            // Targetsキャッシュ
            var targets = (MonoBehaviour[])ExecutorSharedCache.GetTargets(targetType);
            
            // グループ化
            var paired = targets
                         .Select(b => new
                         {
                             Behaviour = b,
                             Anchor = anchorType == null
                                 ? (object)"__ALL__"
                                 : GetAnchorCached(b.transform, anchorType, scope)
                         })
                         .ToArray();

            // Anchorが見つからなかったインスタンスにはIndexが割り振られない。
            // 黙って初期値のまま残ると気付けないため警告する
            var orphans = paired.Where(x => x.Anchor == null).ToArray();
            if (orphans.Length > 0)
            {
                Debug.LogWarning($"<color=#FF9900>[VGC.AutoAssignIndex] Anchor was not found. Index was not assigned.\n" +
                                 $"Component: {targetType.Name}\n" +
                                 $"Field: {field.Name}\n" +
                                 $"AnchorType: {anchorType.FullName}\n" +
                                 $"Scope: {scope}\n" +
                                 $"Count: {orphans.Length}</color>"
                    , orphans[0].Behaviour);
            }

            var grouped = paired
                          .Where(x => x.Anchor != null)
                          .GroupBy(x => x.Anchor);

            foreach (var group in grouped)
            {
                var list = group.Select(x => x.Behaviour).ToList();
                
                // Hierarchy順
                if (order == ExecutorOrder.Hierarchy)
                {
                    list.Sort((a, b) =>
                        // ReSharper disable once StringCompareToIsCultureSpecific
                        ExecutorSharedCache.GetHierarchyIndex(a.transform)
                                           .CompareTo(ExecutorSharedCache.GetHierarchyIndex(b.transform)));
                }
                
                // Index割り振り
                for (int i = 0; i < list.Count; i++)
                {
                    field.SetValue(list[i], i);
                    EditorUtility.SetDirty(list[i]);
                }
            }
        }
        
        private static UnityEngine.Object GetAnchorCached(
            Transform t,
            Type anchorType,
            ExecutorScope scope)
        {
            var key = (t, anchorType, scope);

            if (AutoAssignIndexCache.AnchorCache.TryGetValue(key, out var cached))
                return cached;

            var found = ExecuteScopeHelper.FindTarget(t, anchorType, scope);

            // 破棄済みUnityObjectはobjectへboxingすると `!= null` を通過してしまう。
            // Unityのnull比較演算子で判定できるここで純粋なnullへ正規化しておく
            UnityEngine.Object result = found != null ? found : null;

            AutoAssignIndexCache.AnchorCache[key] = result;

            return result;
        }
    }
}
#endif
