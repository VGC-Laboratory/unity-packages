#if MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;
using VGC.Gimmicks_MA.Lib.Localization;

[assembly: ExportsPlugin(typeof(LocalizationExMenuPlugin))]

namespace VGC.Gimmicks_MA.Lib.Localization
{
    internal sealed class LocalizationExMenuPlugin : Plugin<LocalizationExMenuPlugin>
    {
        public override string QualifiedName => "vgc.localization";
        public override string DisplayName => "VGC Expression Menu Localize plugin";

        protected override void Configure()
        {
            // 旧方式: 言語ごとのメニュー資産を差し替える
            InPhase(BuildPhase.FirstChance)
                .Run("VGC Expression Menu Localize", ctx =>
                {
                    foreach (var component in ctx.AvatarRootObject.GetComponentsInChildren<LocalizationExMenu>())
                    {
                        component.Configure();
                    }
                });

            // 新方式: MA Menu Item のラベルをキーで置き換える(MAがメニューを組み立てる前に行う)
            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("VGC Localize Menu Labels", ctx =>
                {
                    var language = LocalizeConfig.instance.Language;
                    foreach (var labels in ctx.AvatarRootObject.GetComponentsInChildren<LocalizedMenuLabels>(true))
                    {
                        if (labels.Table == null)
                        {
                            Debug.LogWarning("VGC Localized Menu Labels: LocalizationTable が設定されていません。", labels);
                            continue;
                        }

                        foreach (var item in labels.GetComponentsInChildren<ModularAvatarMenuItem>(true))
                        {
                            Localize(item, labels, language);
                        }
                    }
                });
        }

        private static void Localize(ModularAvatarMenuItem item, LocalizedMenuLabels labels, Language language)
        {
            var table = labels.Table;

            // 表示名: label が設定されていればそれをキーに、無ければ GameObject 名をキーにする
            var key = labels.KeyPrefix + (string.IsNullOrEmpty(item.label) ? item.gameObject.name : item.label);
            if (TryGet(table, key, language, out var text))
                item.label = text;

            // Radial / 2軸 / 4軸 Puppet の軸ラベル
            var subLabels = item.Control?.labels;
            if (subLabels == null) return;

            for (var i = 0; i < subLabels.Length; i++)
            {
                if (string.IsNullOrEmpty(subLabels[i].name)) continue;

                if (TryGet(table, labels.KeyPrefix + subLabels[i].name, language, out var labelText))
                    subLabels[i].name = labelText;
            }
        }

        // 選択言語 → 英語 の順で探す。どちらも無ければ元の文字を残す
        private static bool TryGet(LocalizationTable table, string key, Language language, out string text)
        {
            return table.TryGet(key, language, out text) ||
                   (language != Language.English && table.TryGet(key, Language.English, out text));
        }
    }
}
#endif
