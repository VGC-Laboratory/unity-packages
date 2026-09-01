using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VGC.Attributes.Runtime;

#if UDONSHARP
using UdonSharp;
using UdonSharpEditor;
#endif

namespace VGC.Attributes.Editor
{
    [FilePath("ScriptableSingleton/AttributeExecutorSetting.dat", FilePathAttribute.Location.ProjectFolder)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class AttributeExecutorSetting : ScriptableSingleton<AttributeExecutorSetting>
    {
        [SerializeField]
        private bool _isExecuteUnityEditor;

        public bool IsExecuteInUnityEditor
        {
            get => _isExecuteUnityEditor;
            private set
            {
                if (_isExecuteUnityEditor != value)
                {
                    _isExecuteUnityEditor = value;
                    Save(true);
                }
            }
        }
#if !VGC_DEVELOP
        [MenuItem("VGC/Attribute/Executor/TurnOFF ExecuteUnityEditor(now ON)")]
        private static void TurnOffExecuteUnityEditor()
        {
            instance.IsExecuteInUnityEditor = !instance.IsExecuteInUnityEditor;
        }

        [MenuItem("VGC/Attribute/Executor/TurnOFF ExecuteUnityEditor(now ON)", validate = true)]
        private static bool TurnOffExecuteUnityEditorValidation()
        {
            return instance.IsExecuteInUnityEditor;
        }

        [MenuItem("VGC/Attribute/Executor/TurnON ExecuteUnityEditor(now OFF)")]
        private static void TurnOnExecuteUnityEditor()
        {
            instance.IsExecuteInUnityEditor = !instance.IsExecuteInUnityEditor;
        }

        [MenuItem("VGC/Attribute/Executor/TurnON ExecuteUnityEditor(now OFF)", true)]
        private static bool TurnOnExecuteUnityEditorValidation()
        {
            return !instance.IsExecuteInUnityEditor;
        }
#endif
    }
    
    public class AttributeExecutorBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => -10000;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            AttributeExecutor.Execute(false);
        }
    }
    
    public static class AttributeExecutor
    {
       [DidReloadScripts]
       static void OnDidReloadScripts()
       {
            EditorSceneManager.sceneOpened += OnSceneOpend;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabSaved += OnPrefabStageSaved;
            ObjectFactory.componentWasAdded += OnComponentWasAdded;

            if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
                return;
            
            Execute();
       }

       static void OnSceneOpend(Scene scene, OpenSceneMode mode)
       {
           if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
               return;
           
           Execute();
       }
       
       static void OnSceneSaving(Scene scene, string path)
       {
           if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
               return;
           
           Execute(false);
       }
       
       static void OnPrefabStageOpened(PrefabStage stage)
       {
           if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
               return;
           
           Execute();
       }

       static void OnPrefabStageSaved(GameObject prefab)
       {
           if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
               return;
           
           Execute(false);
       }

       static void OnComponentWasAdded(Component component)
       {
           if (!AttributeExecutorSetting.instance.IsExecuteInUnityEditor || EditorApplication.isPlaying)
               return;
           
           Execute();   
       }
       
       public static void Execute(bool registerUndo = true)
       {
           Debug.Log("<color=#FF9900>[VGC.AttributeExecutor] Start Execute</color>");
           System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
           sw.Start();
           
           ExecutorSharedCache.Clear();
           AutoAssignIndexCache.Clear();
           foreach (var monoBehaviour in ExecutorSharedCache.GetTargets<MonoBehaviour>())
           {
               bool updateFields = false;
               var type = monoBehaviour.GetType();
               var fieldInfos = GetAllFields(type);
               
               foreach (var fieldInfo in fieldInfos)
               {
                   foreach (var attribute in fieldInfo.GetCustomAttributes().OfType<IExecutorFieldAttribute>())
                   {
                       // 戻り値を捨てて無条件にtrueを立てると、
                       // 値が変わっていなくても CopyProxyToUdon が走る
                       updateFields |= attribute.Execute(monoBehaviour, fieldInfo, registerUndo);
                   }
               }
               
               foreach (var attribute in type.GetCustomAttributes().OfType<IExecutorClassAttribute>())
               {
                   updateFields |= attribute.Execute(monoBehaviour, registerUndo);
               }
               
               if (updateFields)
               {
#if UDONSHARP
                   if(monoBehaviour is UdonSharpBehaviour udonSharpBehaviour)
                   {
                       UdonSharpEditorUtility.CopyProxyToUdon(udonSharpBehaviour);
                   }
                   else
#endif
                   {
                       EditorUtility.SetDirty(monoBehaviour);
                   }
               }
           }
           
           sw.Stop();
           Debug.Log($"<color=#FF9900>[VGC.AttributeExecutor] Complete Execute 経過時間:{sw.ElapsedMilliseconds} ms</color>");
       }
       
       static IEnumerable<FieldInfo> GetAllFields(Type type)
       {
           var fields = new List<FieldInfo>();
           while (type != null && type != typeof(object))
           {
               fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
               type = type.BaseType;
           }
           return fields;
       }
    }
}