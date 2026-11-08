using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VGC.Attributes.Editor;

namespace VGC.Attributes.Udon.Editor
{
    /// <summary>
    /// UdonSharpBehaviour のフィールドを書き換えたあと、裏の UdonBehaviour へ写す処理を
    /// AttributeExecutor に登録する。
    /// このアセンブリは com.vrchat.worlds が入っている場合のみコンパイルされる
    /// （asmdef の versionDefines と defineConstraints による）ので、
    /// アバター用プロジェクトでは AttributeExecutor は EditorUtility.SetDirty だけを行う。
    /// </summary>
    [InitializeOnLoad]
    internal static class UdonProxyWriteBack
    {
        static UdonProxyWriteBack()
        {
            AttributeExecutor.AfterFieldsAssigned = WriteBack;
        }

        static bool WriteBack(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour is UdonSharpBehaviour udonSharpBehaviour)
            {
                UdonSharpEditorUtility.CopyProxyToUdon(udonSharpBehaviour);
                return true;
            }

            return false;
        }
    }
}
