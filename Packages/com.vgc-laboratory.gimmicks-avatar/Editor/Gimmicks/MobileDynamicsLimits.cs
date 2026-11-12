using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VGC.Localization;
using VRC.Dynamics;
using VRC.SDKBase.Validation.Performance;
using VRC.SDKBase.Validation.Performance.Stats;

namespace VGC.Gimmicks.Avatar
{
    /// <summary>
    /// アバターの PhysBone・Contact・Constraint の数を、モバイル(Android / iOS。Quest も Android)の数え方で調べたもの。
    ///
    /// モバイルでは、下の 7 項目のどれか 1 つでも Poor の上限を超える(Very Poor になる)と、
    /// アバターの PhysBone・Contact・VRC Constraint が全部取り除かれる。Show Avatar でも戻らない。
    /// ギミックをいくつも入れると超えやすいので、導入ウィンドウに出して気づけるようにする。
    ///
    /// 数と上限は SDK から取る(値をここに書かない。SDK の更新で変わっても合うように)。
    /// </summary>
    internal sealed class MobileDynamicsLimits
    {
        /// <summary>ランクの名前。VRChat の表示に合わせて訳さない。</summary>
        public static readonly string[] RankNames = { "Excellent", "Good", "Medium", "Poor", "Very Poor" };

        public const int VeryPoorRank = 4;

        public sealed class Item
        {
            public string Label;
            public int Count;

            /// <summary>Excellent / Good / Medium / Poor の上限(この数までそのランク)。</summary>
            public int[] Limits;

            public int PoorLimit => Limits[Limits.Length - 1];

            /// <summary>0 = Excellent … 4 = Very Poor。</summary>
            public int Rank
            {
                get
                {
                    for (var i = 0; i < Limits.Length; i++)
                    {
                        if (Count <= Limits[i])
                            return i;
                    }

                    return VeryPoorRank;
                }
            }

            public bool IsVeryPoor => Rank == VeryPoorRank;
        }

        /// <summary>いつも出す 3 つ。</summary>
        public Item PhysBone, Contact, Constraint;

        /// <summary>取り除かれるきっかけにはなるが、ふだんは出さない 4 つ(Very Poor のときだけ出す)。</summary>
        public Item[] Extras;

        public IEnumerable<Item> All => new[] { PhysBone, Contact, Constraint }.Concat(Extras);

        /// <summary>7 項目のうち一番悪いランク。</summary>
        public int WorstRank => All.Max(item => item.Rank);

        /// <summary>モバイルで PhysBone・Contact・VRC Constraint が取り除かれる状態か。</summary>
        public bool AnyVeryPoor => WorstRank == VeryPoorRank;

        /// <summary>
        /// アバターを調べる。非アクティブの物も数える。EditorOnly の下にある物は数えない(ビルドで消えるため)。
        /// ビルドのときに NDMF のプラグインが足す物は、まだ無いので数えられない。
        /// メッシュやテクスチャも見るので軽くはない。毎フレームは呼ばないこと。
        /// </summary>
        public static MobileDynamicsLimits Calculate(GameObject avatar)
        {
            var stats = new AvatarPerformanceStats(true);
            AvatarPerformance.CalculatePerformanceStats(avatar.name, avatar, stats, true);

            var levels = new[] { PerformanceRating.Excellent, PerformanceRating.Good, PerformanceRating.Medium, PerformanceRating.Poor }
                         .Select(rating => AvatarPerformanceStats.GetStatLevelForRating(rating, true))
                         .ToArray();
            int[] Limits(Func<AvatarPerformanceStatsLevel, int> pick) => levels.Select(pick).ToArray();

            var physBone = stats.physBone ?? default;
            return new MobileDynamicsLimits
            {
                PhysBone = new Item { Label = "PhysBone", Count = physBone.componentCount, Limits = Limits(l => l.physBone.componentCount) },
                // SDK の計算は、Contact だけ EditorOnly の下の物も数える(3.10.6 で確認)。ビルドで消えるので、自分で数える
                Contact = new Item { Label = "Contact", Count = CountContacts(avatar), Limits = Limits(l => l.contactCount) },
                Constraint = new Item { Label = "Constraint", Count = stats.constraintsCount ?? 0, Limits = Limits(l => l.constraintsCount) },
                Extras = new[]
                {
                    new Item { Label = "PhysBone Transform", Count = physBone.transformCount, Limits = Limits(l => l.physBone.transformCount) },
                    new Item { Label = "PhysBone Collider", Count = physBone.colliderCount, Limits = Limits(l => l.physBone.colliderCount) },
                    new Item { Label = "Collision Check", Count = physBone.collisionCheckCount, Limits = Limits(l => l.physBone.collisionCheckCount) },
                    new Item { Label = "Constraint Depth", Count = stats.constraintDepth ?? 0, Limits = Limits(l => l.constraintDepth) },
                },
            };
        }

        /// <summary>
        /// SDK と同じ数え方(Local Only でない Contact。非アクティブも含む)で、EditorOnly の下にある物を除いて数える。
        /// </summary>
        private static int CountContacts(GameObject avatar)
        {
            return avatar.GetComponentsInChildren<ContactBase>(true)
                         .Count(contact => contact != null && !contact.IsLocalOnly && !IsEditorOnly(contact.transform, avatar.transform));
        }

        private static bool IsEditorOnly(Transform transform, Transform root)
        {
            for (var t = transform; t != null; t = t.parent)
            {
                if (t.CompareTag("EditorOnly"))
                    return true;
                if (t == root)
                    break;
            }

            return false;
        }
    }

    /// <summary>
    /// <see cref="MobileDynamicsLimits"/> を、畳める欄とゲージで描く。
    /// </summary>
    internal static class MobileDynamicsLimitsGUI
    {
        private const float MeterHeight = 18f;

        private static readonly Color Background = new Color(0f, 0f, 0f, 0.25f);
        private static readonly Color Blue = new Color(0.3f, 0.6f, 0.95f);
        private static readonly Color Yellow = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color Red = new Color(0.85f, 0.25f, 0.25f);
        private static readonly Color Divider = new Color(1f, 1f, 1f, 0.6f);

        /// <summary>
        /// 欄を描く。開いているかどうかを返す。limits が null のときは、計算できなかった旨を出す。
        /// </summary>
        public static bool Draw(MobileDynamicsLimits limits, bool foldout, LocalizationTable localize)
        {
            var header = EditorGUILayout.GetControlRect();
            var title = new GUIContent(Text(localize, "MobileLimits.Title"), Text(localize, "MobileLimits.Tooltip"));
            foldout = EditorGUI.Foldout(header, foldout, title, true);

            if (limits == null)
            {
                if (foldout)
                    EditorGUILayout.HelpBox(Text(localize, "MobileLimits.Failed"), MessageType.Warning);
                return foldout;
            }

            // 畳んでいても様子が分かるように、見出しの行に Contact の数と一番悪いランクを出す
            var summary = $"Contact {limits.Contact.Count}/{limits.Contact.PoorLimit} · {MobileDynamicsLimits.RankNames[limits.WorstRank]}";
            var summaryStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            if (limits.AnyVeryPoor)
                summaryStyle.normal.textColor = Red;
            GUI.Label(header, summary, summaryStyle);

            if (!foldout)
                return false;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawMeter(limits.PhysBone, localize);
                DrawMeter(limits.Contact, localize);
                DrawMeter(limits.Constraint, localize);

                // ふだんは出さない項目も、取り除かれるきっかけになったときは理由が分かるように出す
                foreach (var item in limits.Extras)
                {
                    if (item.IsVeryPoor)
                        DrawMeter(item, localize);
                }

                if (limits.AnyVeryPoor)
                    EditorGUILayout.HelpBox(Text(localize, "MobileLimits.Removed"), MessageType.Error);

                EditorGUILayout.LabelField(Text(localize, "MobileLimits.BuildNote"), EditorStyles.miniLabel);
            }

            return true;
        }

        /// <summary>
        /// 左にラベル、右にバー。ランクの境目に区切りを入れ、区間は同じ幅にして、区間の中は数に比例して塗る
        /// (小さい数も見えるように)。上限が手前と同じ区間(PhysBone の Excellent は 0)は幅を取らない。
        /// </summary>
        private static void DrawMeter(MobileDynamicsLimits.Item item, LocalizationTable localize)
        {
            var row = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, MeterHeight));
            var labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, row.width * 0.4f);
            var bar = new Rect(row.x + labelWidth, row.y, row.width - labelWidth, row.height);
            GUI.Label(new Rect(row.x, row.y, labelWidth, row.height), item.Label, EditorStyles.label);

            EditorGUI.DrawRect(bar, Background);

            // 幅を取る区間の上限だけを並べる
            var bounds = new List<int>();
            foreach (var limit in item.Limits)
            {
                if (limit > (bounds.Count > 0 ? bounds[bounds.Count - 1] : 0))
                    bounds.Add(limit);
            }

            var rank = item.Rank;
            var color = rank == MobileDynamicsLimits.VeryPoorRank ? Red : rank == MobileDynamicsLimits.VeryPoorRank - 1 ? Yellow : Blue;
            if (bounds.Count > 0)
            {
                var segment = bar.width / bounds.Count;
                var fill = 0f;
                var previous = 0;
                foreach (var bound in bounds)
                {
                    if (item.Count >= bound)
                    {
                        fill += segment;
                    }
                    else
                    {
                        if (item.Count > previous)
                            fill += segment * (item.Count - previous) / (bound - previous);
                        break;
                    }

                    previous = bound;
                }

                EditorGUI.DrawRect(new Rect(bar.x, bar.y, Mathf.Min(fill, bar.width), bar.height), color);

                // ランクの境目(右端は Poor の上限)
                for (var i = 1; i < bounds.Count; i++)
                    EditorGUI.DrawRect(new Rect(bar.x + segment * i - 1, bar.y, 2, bar.height), Divider);
            }
            else if (item.Count > 0)
            {
                // 上限が全部 0 の項目は、1 つでもあれば振り切れる
                EditorGUI.DrawRect(bar, color);
            }

            // バーの左にランクと残り、右に数
            var remaining = item.PoorLimit - item.Count;
            var detail = remaining >= 0
                ? string.Format(Text(localize, "MobileLimits.Remaining"), remaining)
                : string.Format(Text(localize, "MobileLimits.Over"), -remaining);
            var style = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = Color.white } };
            style.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(bar.x + 4, bar.y, bar.width - 8, bar.height), $"{MobileDynamicsLimits.RankNames[rank]} · {detail}", style);
            style.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(bar.x + 4, bar.y, bar.width - 8, bar.height),
                      item.IsVeryPoor ? $"{item.Count} / {item.PoorLimit} +" : $"{item.Count} / {item.PoorLimit}", style);
        }

        // 翻訳表にキーが無いとき(gimmicks 本体が古いときなど)はキーがそのまま出てしまうので、英語で出す
        private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
        {
            { "MobileLimits.Title", "Mobile limits (Android / iOS)" },
            { "MobileLimits.Tooltip", "Quest is also Android. If any of these becomes Very Poor, all PhysBones, Contacts and VRChat Constraints are removed on mobile." },
            { "MobileLimits.Remaining", "{0} left" },
            { "MobileLimits.Over", "{0} over" },
            { "MobileLimits.Removed", "On mobile, all PhysBones, Contacts and VRChat Constraints will be removed. Gimmicks and swaying parts will not work." },
            { "MobileLimits.BuildNote", "Components added by gimmicks at build time are not included." },
            { "MobileLimits.Failed", "Could not calculate the mobile limits." },
        };

        private static string Text(LocalizationTable localize, string key)
        {
            return localize != null && localize.ContainsKey(key) ? localize.Get(key) : Fallback[key];
        }
    }
}
