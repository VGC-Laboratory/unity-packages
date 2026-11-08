using System.Collections.Generic;
using System.Linq;
using TMPro;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VGC.Localization.World
{
    /// <summary>
    /// シーン内の LocalizationSystem と LocalizedUdonBehaviour に翻訳を焼き込む。
    ///
    /// いつ走るか: ビルド・再生開始(IProcessSceneWithReport)、シーンの保存、エディタの言語の変更。
    /// 言語の一覧は Project Settings > VGC > Localization の並び順。
    /// エディタの言語を変えたときは、焼き込みに加えて文字欄をその言語で書き換える(プレビュー)。
    /// </summary>
    public static class LocalizationBaker
    {
        /// <summary>SystemLanguage と VRChat の言語コードの対応。</summary>
        private static readonly Dictionary<SystemLanguage, string[]> VRChatCodes = new()
        {
            { SystemLanguage.English, new[] { "en" } },
            { SystemLanguage.French, new[] { "fr" } },
            { SystemLanguage.German, new[] { "de" } },
            { SystemLanguage.Italian, new[] { "it" } },
            { SystemLanguage.Japanese, new[] { "ja" } },
            { SystemLanguage.Korean, new[] { "ko" } },
            { SystemLanguage.Spanish, new[] { "es" } },
            { SystemLanguage.Portuguese, new[] { "pt", "pt-BR" } },
            { SystemLanguage.Hebrew, new[] { "he" } },
            { SystemLanguage.Polish, new[] { "pl" } },
            { SystemLanguage.Indonesian, new[] { "id" } },
            { SystemLanguage.ChineseSimplified, new[] { "zh-CN" } },
            { SystemLanguage.ChineseTraditional, new[] { "zh-HK", "zh-TW" } },
            { SystemLanguage.Chinese, new[] { "zh" } },
            { SystemLanguage.Russian, new[] { "ru" } },
            { SystemLanguage.Swedish, new[] { "sv" } },
            { SystemLanguage.Dutch, new[] { "nl" } },
            { SystemLanguage.Ukrainian, new[] { "uk" } },
            { SystemLanguage.Danish, new[] { "da" } },
            { SystemLanguage.Norwegian, new[] { "no" } },
            { SystemLanguage.Thai, new[] { "th" } },
            { SystemLanguage.Finnish, new[] { "fi" } },
            { SystemLanguage.Hungarian, new[] { "hu" } },
            { SystemLanguage.Czech, new[] { "cs" } },
            { SystemLanguage.Turkish, new[] { "tr" } },
            { SystemLanguage.Arabic, new[] { "ar" } },
            { SystemLanguage.Romanian, new[] { "ro" } },
            { SystemLanguage.Vietnamese, new[] { "vi" } },
        };

        /// <summary>開いているシーンをすべて焼き込む。</summary>
        public static void BakeOpenScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Bake(scene);
            }
        }

        public static void Bake(Scene scene)
        {
            var languages = LanguageSetting.Available.ToList();
            if (languages.Count == 0)
                languages.Add(SystemLanguage.English);

            var behaviours = FindInScene<LocalizedUdonBehaviour>(scene);
            var systems = FindInScene<LocalizationSystem>(scene);

            if (systems.Count == 0)
            {
                if (behaviours.Count > 0)
                    Debug.LogWarning($"[VGC.Localization.World] {scene.name}: LocalizationSystem がありません。翻訳は既定言語のままになります。");
            }
            else if (systems.Count > 1)
            {
                Debug.LogWarning($"[VGC.Localization.World] {scene.name}: LocalizationSystem が {systems.Count} 個あります。最初の 1 つだけを使います。", systems[1]);
            }

            var system = systems.FirstOrDefault();
            if (system != null)
                BakeSystem(system, languages, behaviours);

            foreach (var behaviour in behaviours)
                BakeBehaviour(behaviour, system, languages);
        }

        private static void BakeSystem(LocalizationSystem system, List<SystemLanguage> languages,
                                       List<LocalizedUdonBehaviour> listeners)
        {
            var codes = new List<string>();
            var codeIndices = new List<int>();
            for (var i = 0; i < languages.Count; i++)
            {
                if (!VRChatCodes.TryGetValue(languages[i], out var list))
                    continue;
                foreach (var code in list)
                {
                    codes.Add(code);
                    codeIndices.Add(i);
                }
            }

            var defaultIndex = Mathf.Max(0, languages.IndexOf(LanguageSetting.Default));

            system.EditorApplyBake(
                languages.Select(LanguageNames.NativeName).ToArray(),
                codes.ToArray(),
                codeIndices.ToArray(),
                defaultIndex,
                listeners.ToArray());
            Commit(system);
        }

        private static void BakeBehaviour(LocalizedUdonBehaviour behaviour, LocalizationSystem system,
                                          List<SystemLanguage> languages)
        {
            var bindings = behaviour.GetComponent<LocalizationBindings>();

            // キーを集める(同じキーは後ろの表が優先)
            var keys = new List<string>();
            var owners = new Dictionary<string, LocalizationTable>();
            if (bindings != null)
            {
                foreach (var table in bindings.Tables.Where(t => t != null))
                {
                    foreach (var key in table.Keys)
                    {
                        if (string.IsNullOrEmpty(key))
                            continue;
                        if (!owners.ContainsKey(key))
                            keys.Add(key);
                        owners[key] = table;
                    }
                }
            }

            // [言語][キー]。訳が無ければ表の既定言語、それも無ければキー
            var texts = new string[languages.Count][];
            for (var l = 0; l < languages.Count; l++)
            {
                texts[l] = new string[keys.Count];
                for (var k = 0; k < keys.Count; k++)
                    texts[l][k] = owners[keys[k]].Get(keys[k], languages[l]);
            }

            var boundTexts = new List<Text>();
            var boundTmpTexts = new List<TextMeshProUGUI>();
            var boundIndices = new List<int>();
            if (bindings != null)
            {
                foreach (var binding in bindings.Bindings)
                {
                    if (binding == null || binding.target == null || string.IsNullOrEmpty(binding.key))
                        continue;

                    var index = keys.IndexOf(binding.key);
                    if (index < 0)
                    {
                        Debug.LogWarning($"[VGC.Localization.World] キー「{binding.key}」が翻訳表にありません。", bindings);
                        continue;
                    }

                    boundTexts.Add(binding.target as Text);
                    boundTmpTexts.Add(binding.target as TextMeshProUGUI);
                    boundIndices.Add(index);
                }
            }

            behaviour.EditorApplyBake(system, keys.ToArray(), texts,
                                      boundTexts.ToArray(), boundTmpTexts.ToArray(), boundIndices.ToArray());
            Commit(behaviour);
        }

        /// <summary>
        /// エディタの現在の言語でプレビューする。焼き込み済みの各 LocalizedUdonBehaviour の _OnLanguageChanged を
        /// C# として呼ぶ(再生していないときは CurrentLanguageIndex がエディタの表示言語の番号を返す)。
        /// Bindings の文字欄に加えて、各ビヘイビアが override した処理(テレポート先の名前など)も反映される。
        /// </summary>
        public static void PreviewOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var behaviour in FindInScene<LocalizedUdonBehaviour>(scene))
                {
                    try
                    {
                        behaviour._OnLanguageChanged();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[VGC.Localization.World] プレビューに失敗しました: {e.Message}", behaviour);
                    }
                }
            }
        }

        private static void Commit(UdonSharp.UdonSharpBehaviour behaviour)
        {
            UdonSharpEditorUtility.CopyProxyToUdon(behaviour);
            EditorUtility.SetDirty(behaviour);
        }

        private static List<T> FindInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        // ===== いつ焼き込むか =====

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            LanguageSetting.Changed -= OnLanguageChanged;
            LanguageSetting.Changed += OnLanguageChanged;
        }

        private static void OnSceneSaving(Scene scene, string path) => Bake(scene);

        private static void OnLanguageChanged(SystemLanguage language)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            BakeOpenScenes();
            PreviewOpenScenes();
        }

        private class BuildHook : IProcessSceneWithReport
        {
            public int callbackOrder => -100;

            public void OnProcessScene(Scene scene, BuildReport report) => Bake(scene);
        }
    }
}
