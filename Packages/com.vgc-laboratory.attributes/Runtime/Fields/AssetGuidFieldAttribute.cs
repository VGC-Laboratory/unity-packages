using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;
#endif

namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// GUID で指定した Project 内のアセットを、エディタ／ビルド時にフィールドへ焼き込みます。
    /// Inspectorでの編集不可能状態で表示することができます。
    /// </summary>
    /// <remarks>
    /// GUID がフォルダの場合、配列／List フィールドにはそのフォルダ配下（サブフォルダ含む）の
    /// 該当型アセットが path 順で全件入ります。
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class AssetGuidFieldAttribute : PropertyAttribute, IExecutorFieldAttribute
    {
        /// <summary>
        /// 対象アセット（またはフォルダ）の GUID。.meta ファイルの guid と同じ値。
        /// </summary>
        protected string Guid { get; }

        /// <summary>
        /// サブアセット（アトラス内 Sprite、fbx 内 Mesh / AnimationClip など）を名前で指定します。
        /// nullの場合はメインアセットを使用します。
        /// </summary>
        protected string SubAssetName { get; }

        /// <summary>
        /// 値が見つからなかった場合に警告を表示します。
        /// </summary>
        protected bool Required { get; }

        public AssetGuidFieldAttribute(string guid, string subAssetName = null, bool required = true)
        {
            Guid = guid;
            SubAssetName = subAssetName;
            Required = required;
        }

#if UNITY_EDITOR
        public bool Execute(MonoBehaviour monoBehaviour, FieldInfo field, bool registerUndo)
        {
            if (monoBehaviour == null || field == null)
                return false;

            if (registerUndo)
            {
                Undo.RecordObject(monoBehaviour, $"AssetGuid:{field.Name}, {monoBehaviour.name}");
            }

            var elementType = ObjectFieldAssignUtility.ResolveElementType(field.FieldType);
            var assets = LoadAssets(monoBehaviour, field, elementType);

            // 見つからなかった場合も null（配列なら要素数0）を書き込む。
            // Inspector から編集できない以上、古い参照が残り続ける方が危険なため
            ObjectFieldAssignUtility.Apply(monoBehaviour, field, assets, out _, out var changed);

            // 常にtrueを返すと、値が変わっていなくても全UdonSharpBehaviourに
            // CopyProxyToUdon が走る。実際に書き換えたときだけ返す
            return changed;
        }

        private Object[] LoadAssets(MonoBehaviour monoBehaviour, FieldInfo field, Type elementType)
        {
            if (elementType == null || !typeof(Object).IsAssignableFrom(elementType))
            {
                Debug.LogError($"<color=#FF9900>[VGC.AssetGuid] Field type is not a UnityEngine.Object.\n" +
                               $"Component: {monoBehaviour.GetType().Name}\n" +
                               $"Field: {field.Name}\n" +
                               $"Type: {field.FieldType.FullName}</color>"
                    , monoBehaviour);
                return Array.Empty<Object>();
            }

            var path = AssetDatabase.GUIDToAssetPath(Guid);

            Object[] assets;
            if (string.IsNullOrEmpty(path))
            {
                assets = Array.Empty<Object>();
            }
            else if (AssetDatabase.IsValidFolder(path))
            {
                // FindAssets はサブフォルダも辿る。順序が不定なので path で並べ直す
                assets = AssetDatabase.FindAssets($"t:{elementType.Name}", new[] { path })
                                      .Select(AssetDatabase.GUIDToAssetPath)
                                      .Where(p => !string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p))
                                      .Distinct()
                                      .OrderBy(p => p, StringComparer.Ordinal)
                                      .SelectMany(p => LoadFromPath(p, elementType))
                                      .ToArray();
            }
            else
            {
                assets = LoadFromPath(path, elementType).ToArray();
            }

            if (Required && assets.Length == 0)
            {
                Debug.LogWarning($"<color=#FF9900>[VGC.AssetGuid] Asset was not found.\n" +
                                 $"Component: {monoBehaviour.GetType().Name}\n" +
                                 $"Field: {field.Name}\n" +
                                 $"Guid: {Guid}\n" +
                                 $"Path: {(string.IsNullOrEmpty(path) ? "(unresolved)" : path)}\n" +
                                 $"SubAsset: {SubAssetName ?? "(main)"}\n" +
                                 $"Type: {elementType.FullName}</color>"
                    , monoBehaviour);
            }

            return assets;
        }

        private IEnumerable<Object> LoadFromPath(string path, Type elementType)
        {
            if (string.IsNullOrEmpty(SubAssetName))
            {
                var main = AssetDatabase.LoadAssetAtPath(path, elementType);
                return main == null ? Enumerable.Empty<Object>() : new[] { main };
            }

            return AssetDatabase.LoadAllAssetsAtPath(path)
                                .Where(o => o != null && o.name == SubAssetName && elementType.IsInstanceOfType(o));
        }
#endif
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AssetGuidFieldAttribute))]
    public class AssetGuidDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            IExecutorFieldAttribute.ReadOnlyTagGUI(position, property, label, "AssetGuid");
        }
    }
#endif
}
