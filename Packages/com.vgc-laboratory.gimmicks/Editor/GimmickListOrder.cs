using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VGC.Gimmicks
{
    /// <summary>
    /// 導入 UI でのギミックの並び順を決める。画面には触らない(テストしやすいように分けてある)。
    ///
    /// 1. <see cref="GimmickSetting.Order"/> の小さい順。
    /// 2. 同じ値なら渡された順のまま(安定ソート)。
    /// 3. 最後に、依存しているギミックを依存先のすぐ後ろへ動かす。
    ///    依存先は <see cref="GimmickSetting.Dependencies"/> のプレハブを Prefab か CrossPlatformPrefab に持つギミック。
    ///    依存先が一覧に無ければ動かさない。依存先が複数あるときは、2. までの並びで一番後ろにあるものの後ろへ。
    ///    依存がたどって自分に戻る(循環する)ギミックは動かさない。
    /// </summary>
    public static class GimmickListOrder
    {
        public static List<GimmickSetting> Sort(IEnumerable<GimmickSetting> settings)
        {
            if (settings == null)
                return new List<GimmickSetting>();

            // 1. と 2.(OrderBy は安定ソート)
            var list = settings.Where(s => s != null)
                               .Distinct()
                               .OrderBy(s => s.Order)
                               .ToList();
            var count = list.Count;

            // プレハブ → 一覧での位置
            var ownerIndex = new Dictionary<GameObject, int>();
            for (var i = 0; i < count; i++)
            {
                AddOwner(ownerIndex, list[i].Prefab, i);
                AddOwner(ownerIndex, list[i].CrossPlatformPrefab, i);
            }

            // 3. 依存先(親)を決める。複数あれば一番後ろのもの。無ければ -1
            var parent = new int[count];
            for (var i = 0; i < count; i++)
            {
                parent[i] = -1;
                var deps = list[i].Dependencies;
                if (deps == null) continue;

                foreach (var dep in deps)
                {
                    if (dep == null) continue;
                    if (ownerIndex.TryGetValue(dep, out var owner) && owner != i && owner > parent[i])
                        parent[i] = owner;
                }
            }

            // 親をたどって自分に戻るものは循環。循環しているものは親を外して元の位置のままにする
            var inCycle = new bool[count];
            for (var i = 0; i < count; i++)
            {
                var p = parent[i];
                for (var step = 0; p >= 0 && step < count; step++)
                {
                    if (p == i)
                    {
                        inCycle[i] = true;
                        break;
                    }
                    p = parent[p];
                }
            }
            for (var i = 0; i < count; i++)
            {
                if (inCycle[i])
                    parent[i] = -1;
            }

            // 親ごとの子(2. までの並びのまま)
            var children = new List<int>[count];
            for (var i = 0; i < count; i++)
            {
                if (parent[i] < 0) continue;
                if (children[parent[i]] == null)
                    children[parent[i]] = new List<int>();
                children[parent[i]].Add(i);
            }

            // 親の無いものを順に出し、それぞれのすぐ後ろに子(とその子)を出す。
            // 循環を外したあとは木になっているので、それぞれ 1 回ずつ出る
            var result = new List<GimmickSetting>(count);
            var stack = new Stack<int>();
            for (var i = 0; i < count; i++)
            {
                if (parent[i] >= 0) continue;

                stack.Push(i);
                while (stack.Count > 0)
                {
                    var n = stack.Pop();
                    result.Add(list[n]);

                    var c = children[n];
                    if (c == null) continue;
                    for (var k = c.Count - 1; k >= 0; k--)
                        stack.Push(c[k]);
                }
            }

            return result;
        }

        private static void AddOwner(Dictionary<GameObject, int> ownerIndex, GameObject prefab, int index)
        {
            // 同じプレハブを持つギミックが複数あれば、先にあるほうを依存先にする
            if (prefab != null && !ownerIndex.ContainsKey(prefab))
                ownerIndex.Add(prefab, index);
        }
    }
}
