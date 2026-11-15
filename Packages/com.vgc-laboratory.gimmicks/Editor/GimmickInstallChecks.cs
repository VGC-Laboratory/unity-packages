using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VGC.Gimmicks
{
    /// <summary>
    /// プロジェクトにある <see cref="IGimmickInstallCheck"/> を探して呼ぶ。導入 UI が使う。
    /// </summary>
    public static class GimmickInstallChecks
    {
        // 描くたびに(1 回の描画で何度も)呼ばれるので、結果を少しの間使い回す。
        // ヒエラルキーの変更では拾えない変化(Animator の Avatar の差し替えなど)もあるので、時間でも捨てる
        private const double CacheSeconds = 0.5;

        private static readonly IReadOnlyList<GimmickInstallCheckResult> Empty = Array.Empty<GimmickInstallCheckResult>();

        private static List<IGimmickInstallCheck> _checks;
        private static double _staleBefore;
        private static readonly Dictionary<(int setting, int root), (double time, IReadOnlyList<GimmickInstallCheckResult> results)> Cache = new();

        // 同じ例外を描くたびに出さないように、出したものを覚えておく
        private static readonly HashSet<string> Reported = new();

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.hierarchyChanged += Invalidate;
            EditorApplication.projectChanged += Invalidate;
            Undo.undoRedoPerformed += Invalidate;
        }

        // 覚えている結果は消さずに「古い」とだけ印を付ける。
        // 消すと、レイアウトと描画の間に呼ばれたときに、描画の側だけ結果が変わってしまう
        private static void Invalidate() => _staleBefore = EditorApplication.timeSinceStartup;

        /// <summary>
        /// そのギミックを入れる先について、当てはまるチェックの結果を返す(問題なしのものは含まない)。
        /// チェックを置いていないギミックでは空。
        /// </summary>
        public static IReadOnlyList<GimmickInstallCheckResult> Check(GimmickSetting setting, GameObject root)
        {
            if (setting == null || root == null)
                return Empty;

            var checks = Checks();
            if (checks.Count == 0)
                return Empty;

            // レイアウトと描画で結果が食い違うと GUI が崩れるので、作り直すのはレイアウトのとき(と GUI の外)だけ
            var key = (setting.GetInstanceID(), root.GetInstanceID());
            var now = EditorApplication.timeSinceStartup;
            var canRefresh = Event.current == null || Event.current.type == EventType.Layout;
            if (Cache.TryGetValue(key, out var cached) && (!canRefresh || (now - cached.time < CacheSeconds && cached.time > _staleBefore)))
                return cached.results;

            List<GimmickInstallCheckResult> results = null;
            foreach (var check in checks)
            {
                try
                {
                    if (!check.Applies(setting))
                        continue;

                    var result = check.Check(setting, root);
                    if (result.Level == GimmickInstallCheckLevel.None)
                        continue;

                    results ??= new List<GimmickInstallCheckResult>();
                    results.Add(result);
                }
                catch (Exception e)
                {
                    // チェックが落ちても、導入の操作は続けられるようにする
                    if (Reported.Add($"{check.GetType().FullName}:{e.GetType().FullName}:{e.Message}"))
                        Debug.LogException(e);
                }
            }

            var stored = results != null ? results : Empty;
            Cache[key] = (now, stored);
            return stored;
        }

        /// <summary>結果の中に、導入できないもの(Error)があるか。</summary>
        public static bool HasError(IReadOnlyList<GimmickInstallCheckResult> results)
        {
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Level == GimmickInstallCheckLevel.Error)
                    return true;
            }

            return false;
        }

        // スクリプトをコンパイルし直すと static は作り直されるので、型の一覧は 1 回探せば足りる
        private static List<IGimmickInstallCheck> Checks()
        {
            if (_checks != null)
                return _checks;

            _checks = new List<IGimmickInstallCheck>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IGimmickInstallCheck>().OrderBy(t => t.FullName, StringComparer.Ordinal))
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                try
                {
                    _checks.Add((IGimmickInstallCheck)Activator.CreateInstance(type, true));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VGC] {type.FullName} を作れませんでした(引数なしのコンストラクタが要ります): {e.Message}");
                }
            }

            return _checks;
        }
    }
}
