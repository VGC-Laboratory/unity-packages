using System.Linq;
using UnityEditor;
using UnityEngine;
using VGC.Localization;
namespace VGC.Gimmicks
{
    [CustomEditor(typeof(GimmickInstaller), true)]
    public class GimmickInstallerEditor : Editor
    {
        private LocalizationTable _localize;
        private GimmickSetting[] _settings;
        private GimmickInstaller _installer;

        private void OnEnable()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(GimmickSetting)}");
            _settings = guids.Select(AssetDatabase.GUIDToAssetPath)
                             .Select(AssetDatabase.LoadAssetAtPath<GimmickSetting>)
                             .Where(setting => setting != null)
                             .ToArray();
            _installer = (GimmickInstaller)target;
        }

        public override void OnInspectorGUI()
        {
            if (_localize == null)
                _localize = GimmickTexts.Installer;

            LocalizeUIHelper.InspectorGUI();
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            DrawSettings(_installer.gameObject, _settings, _localize);
        }

        public static void DrawSettings(GameObject avatarRoot, GimmickSetting[] settings, LocalizationTable localize = null)
        {
            if (localize == null)
                localize = GimmickTexts.Installer;

            var markers = avatarRoot.GetComponentsInChildren<GimmickMarker>(true);

            // ギミックごとに取得元が違うことがあるので、使われている取得元をすべて取り直す
            var sources = settings.Where(s => s != null).Select(s => s.VersionSource).Distinct().ToArray();
            using (new EditorGUI.DisabledScope(sources.Any(VersionChecker.IsFetching)))
            {
                if (GUILayout.Button(localize.Get("VersionCheck"), GUILayout.Height(24)))
                {
                    // 利用者が押したときはキャッシュが新しくても取り直す
                    foreach (var source in sources)
                        VersionChecker.Fetch(source, force: true);
                }
            }

            // 配布元ごとにまとめて並べる。配布元が空のものを先に、そのあと名前順。
            // 別々の配布元のギミックが同じプロジェクトに入っても取り違えないようにするため。
            var groups = settings
                .Where(s => s != null && s.Prefab != null)
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Publisher) ? "" : s.Publisher.Trim())
                .OrderBy(g => g.Key == "" ? 0 : 1)
                .ThenBy(g => g.Key, System.StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                // 配布元が 1 つしか無いときは見出しを出さない(今までの見た目のまま)
                if (group.Key != "" && settings.Count(s => s != null && s.Prefab != null) > group.Count())
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                }

            foreach (var setting in group)
            {
                if (setting == null) continue;

                var prefab = setting.Prefab;
                var deps = setting.Dependencies;

                if (prefab == null) continue;
                
                var instances = markers
                                .Where(m => m != null && IsGimmickInstance(m.gameObject, setting))
                                .Select(m => m.gameObject)
                                .ToList();

                bool installed = instances.Count > 0;
                bool validDeps = HasValidDependencies(markers, deps);

                bool hasUpdate = VersionChecker.HasUpdate(setting.VersionSource, setting.VersionKey, setting.Version, out string sheetVersion);
                string[] urls = VersionChecker.GetUrls(setting.VersionSource, setting.VersionKey);

                // ===== 色決定 =====
                Color bgColor = Color.clear;

                if (!validDeps)
                    bgColor = new Color(1f, 1f, 0f, 0.1f);
                else if (installed)
                    bgColor = new Color(0.3f, 0.6f, 1f, 0.1f);
                else if (hasUpdate)
                    bgColor = new Color(0.2f, 1f, 0.2f, 0.15f);

                // ===== Box開始 =====
                var rect = EditorGUILayout.BeginVertical("box");
                {
                    if (bgColor.a > 0f)
                        EditorGUI.DrawRect(rect, bgColor);

                    // ===== ヘッダー =====
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(setting.Name, EditorStyles.boldLabel);

                        GUILayout.FlexibleSpace();

                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            if (!validDeps)
                            {
                                GUILayout.Label(localize.Get("InstalledLabel.Missing"));
                            }
                            else if (installed)
                            {
                                GUILayout.Label(localize.Get("InstalledLabel.Installed", instances.Count));
                            }

                            if (hasUpdate)
                            {
                                GUI.color = new Color(0.2f, 0.8f, 0.2f);
                                GUILayout.Label(localize.Get("NewVersion", sheetVersion), EditorStyles.miniBoldLabel);
                                GUI.color = Color.white;
                            }
                        }
                    }
                    
                    // ===== ヘッダー2(CrossPlatform) =====
                    if (!setting.CrossPlatformReady)
                    {
                        EditorGUILayout.LabelField(localize.Get("CrossPlatform.NotSupported"));
                    }
                    else if (setting.CrossPlatformPrefab == null)
                    {
                        EditorGUILayout.LabelField(localize.Get("CrossPlatform.Supported"));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(localize.Get("CrossPlatform.Supported"));
                        var instance = instances.FirstOrDefault();
                        var marker = instance
                            ? instance.GetComponentInChildren<GimmickMarker>()
                            : null;

                        bool useCross = marker != null && marker.UseCrossPlatformPrefab;
                        using (new EditorGUILayout.VerticalScope())
                        {
                            using (new EditorGUI.DisabledScope(marker == null))
                            {
                                EditorGUI.BeginChangeCheck();
                                bool newValue = EditorGUILayout.ToggleLeft(
                                    localize.Get("UseCrossPlatformPrefab"),
                                    useCross
                                );
                                if (EditorGUI.EndChangeCheck() && marker != null &&
                                    EnsureDeletable(instances, localize))
                                {
                                    Undo.RecordObject(marker, "Toggle CrossPlatform");

                                    var so = new SerializedObject(marker);
                                    so.FindProperty("_useCrossPlatformPrefab").boolValue = newValue;
                                    so.ApplyModifiedProperties();

                                    // 最新 markers 取得
                                    var latestMarkers = avatarRoot.GetComponentsInChildren<GimmickMarker>(true);

                                    // 対象だけ削除
                                    foreach (var m in latestMarkers)
                                    {
                                        if (m == null) continue;

                                        if (IsGimmickInstance(m.gameObject, setting))
                                        {
                                            Undo.DestroyObjectImmediate(m.gameObject);
                                        }
                                    }

                                    var newPrefab = newValue ? setting.CrossPlatformPrefab : setting.Prefab;
                                    var newInstance = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab, avatarRoot.transform);
                                    Undo.RegisterCreatedObjectUndo(newInstance, "Switch CrossPlatform Prefab");

                                    // Marker再取得してフラグ維持
                                    var newMarker = newInstance.GetComponentInChildren<GimmickMarker>();
                                    if (newMarker != null)
                                    {
                                        var newSo = new SerializedObject(newMarker);
                                        newSo.FindProperty("_useCrossPlatformPrefab").boolValue = newValue;
                                        newSo.ApplyModifiedProperties();
                                    }

                                    // UI更新用
                                    markers = avatarRoot.GetComponentsInChildren<GimmickMarker>(true);
                                    instances = markers
                                                .Where(m => m != null && IsGimmickInstance(m.gameObject, setting))
                                                .Select(m => m.gameObject)
                                                .ToList();
                                }
                            }

                            EditorGUILayout.HelpBox(
                                localize.Get("CrossPlatformHelp"),
                                MessageType.None
                            );
                        }
                    }

                    // ===== ボタン列 =====
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!validDeps || installed))
                        {
                            if (GUILayout.Button(localize.Get("Install"), GUILayout.Height(24)))
                            {
                                GimmickMarker marker = null;
                                var existing = instances.FirstOrDefault();
                                if (existing)
                                    marker = existing.GetComponentInChildren<GimmickMarker>();

                                var targetPrefab = ResolveInstallPrefab(setting, marker);
                                Install(avatarRoot, targetPrefab);
                            }
                        }

                        using (new EditorGUI.DisabledScope(!installed))
                        {
                            if (GUILayout.Button(localize.Get("Uninstall"), GUILayout.Height(24)))
                            {
                                if (EnsureDeletable(instances, localize))
                                    Uninstall(markers, setting);
                                markers = avatarRoot.GetComponentsInChildren<GimmickMarker>(true);
                            }
                        }

                        using (new EditorGUI.DisabledScope(!installed))
                        {
                            if (setting.UseSetting)
                            {
                                if (GUILayout.Button(localize.Get("Setting", instances.Count), GUILayout.Height(24)))
                                {
                                    Selection.objects = instances.ToArray();
                                }
                            }
                            else
                            {
                                if (GUILayout.Button(localize.Get("Select", instances.Count), GUILayout.Height(24)))
                                {
                                    Selection.objects = instances.ToArray();
                                }
                            }
                        }

                        // 新しい版が無いときは押せない。配布先が複数あれば押したときに選ばせる
                        UpdateNoticeGUI.DrawOpenButton(localize.Get("Update"), hasUpdate ? urls : null, GUILayout.Height(24));
                    }

                    // ===== 依存エラー表示 =====
                    if (!validDeps)
                    {
                        EditorGUILayout.Space(4);

                        EditorGUILayout.HelpBox(localize.Get("DependenciesHelpBox"), MessageType.Info);

                        if (deps != null)
                        {
                            foreach (var dep in deps)
                            {
                                EditorGUILayout.BeginHorizontal();

                                if (dep == null)
                                {
                                    EditorGUILayout.LabelField(localize.Get("Missing"), EditorStyles.miniLabel);
                                }
                                else
                                {
                                    bool found = markers.Any(m => m != null && IsInstanceOf(m.gameObject, dep));

                                    if (!found)
                                    {
                                        EditorGUILayout.LabelField($"• {dep.name}", EditorStyles.miniLabel);

                                        // PrefabのAssetsPingはオフ
                                        /*if (GUILayout.Button("Select", GUILayout.Width(60)))
                                        {
                                            Selection.activeObject = dep;
                                            EditorGUIUtility.PingObject(dep);
                                        }*/
                                    }
                                }

                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                // ===== Box終了 =====

                EditorGUILayout.Space(4);
            }
            } // 配布元ごとのまとめ
        }

        private static void Install(GameObject avatarRoot, GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, avatarRoot.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Install Gimmick");
        }
        
        private static void Uninstall(GimmickMarker[] markers, GimmickSetting setting)
        {
            foreach (var t in markers)
            {
                if (t != null && IsGimmickInstance(t.gameObject, setting))
                {
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
        }

        private static bool HasValidDependencies(GimmickMarker[] markers, GameObject[] deps)
        {
            if (deps == null) return true;

            foreach (var dep in deps)
            {
                if (dep == null) return false;

                var found = markers.Any(g => g != null && IsInstanceOf(g.gameObject, dep));

                if (!found) return false;
            }

            return true;
        }
        
        /// <summary>
        /// その GameObject がギミックのプレハブ(またはクロスプラットフォーム版)のインスタンスか。
        /// </summary>
        private static bool IsGimmickInstance(GameObject go, GimmickSetting setting)
        {
            return IsInstanceOf(go, setting.Prefab) ||
                   (setting.CrossPlatformPrefab != null && IsInstanceOf(go, setting.CrossPlatformPrefab));
        }

        /// <summary>
        /// プレハブの元をたどって、指定のプレハブが含まれているか。
        /// アバターごとプレハブ化(Apply・バリアント化)されていると、一番近い元はアバター側のプレハブになるため、
        /// 一番近い元だけでなく大元まで順にたどる。
        /// </summary>
        private static bool IsInstanceOf(GameObject go, GameObject prefab)
        {
            if (go == null || prefab == null) return false;

            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            // 循環は起きないが、念のため深さを制限する
            for (var depth = 0; src != null && depth < 32; depth++)
            {
                if (src == prefab) return true;
                src = PrefabUtility.GetCorrespondingObjectFromSource(src);
            }

            return false;
        }

        /// <summary>
        /// シーン上から削除できるか確認する。アバターのプレハブに含まれているギミックは
        /// Unityの仕様でシーンからは削除できないため、案内を出してそのオブジェクトを選択する。
        /// </summary>
        private static bool EnsureDeletable(System.Collections.Generic.IEnumerable<GameObject> instances, LocalizationTable localize)
        {
            var locked = instances.Where(go => go != null &&
                                               PrefabUtility.IsPartOfPrefabInstance(go) &&
                                               !PrefabUtility.IsOutermostPrefabInstanceRoot(go) &&
                                               !PrefabUtility.IsAddedGameObjectOverride(go))
                                  .ToArray();
            if (locked.Length == 0)
                return true;

            EditorUtility.DisplayDialog(localize.Get("DialogTitle"), localize.Get("CannotRemoveInPrefab"), localize.Get("DialogOK"));
            Selection.objects = locked;
            EditorGUIUtility.PingObject(locked[0]);
            return false;
        }

        private static GameObject ResolveInstallPrefab(GimmickSetting setting, GimmickMarker marker)
        {
            if (setting.CrossPlatformReady &&
                setting.CrossPlatformPrefab != null &&
                marker != null &&
                marker.UseCrossPlatformPrefab)
            {
                return setting.CrossPlatformPrefab;
            }

            return setting.Prefab;
        }
    }
}