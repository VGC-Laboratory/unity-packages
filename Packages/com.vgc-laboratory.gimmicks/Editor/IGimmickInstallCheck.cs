using UnityEngine;

namespace VGC.Gimmicks
{
    /// <summary>
    /// ギミックを入れる先(アバターなど)を調べて、「ここには入れられない」「入れられるが注意が要る」を
    /// 導入 UI に伝える。ギミックの Editor アセンブリに、これを実装したクラスを置くだけでよい
    /// (登録は要らない。導入 UI が型から探して呼ぶ)。
    ///
    /// 引数なしのコンストラクタが要る。インスタンスは 1 つだけ作って使い回すので、状態は持たないこと。
    ///
    /// 例: 目の骨が無いアバターには入れられないギミック
    /// <code>
    /// public class EyesMovingInstallCheck : IGimmickInstallCheck
    /// {
    ///     public bool Applies(GimmickSetting setting) => setting.name == "EyesMovingSystem";
    ///
    ///     public GimmickInstallCheckResult Check(GimmickSetting setting, GameObject root)
    ///     {
    ///         var animator = root.GetComponent&lt;Animator&gt;();
    ///         if (animator == null || !animator.isHuman || animator.GetBoneTransform(HumanBodyBones.LeftEye) == null)
    ///             return GimmickInstallCheckResult.Error(setting.Table.Get("NoEyeBones"));
    ///         return GimmickInstallCheckResult.Ok;
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IGimmickInstallCheck
    {
        /// <summary>
        /// このチェックが、そのギミックのものか。一覧の行ごとに呼ばれるので、軽くすること
        /// (アセットの名前や参照を比べる程度)。
        /// </summary>
        bool Applies(GimmickSetting setting);

        /// <summary>
        /// 入れる先を調べる。入れる前にも、入れた後にも呼ばれる(後からアバターを変えた場合にも気づけるように)。
        /// 結果は少しの間使い回されるが、描くたびに呼ばれることもあるので、重い処理はしないこと。
        /// 文言は訳したものを返す(ギミックの LocalizationTable は <see cref="GimmickSetting.Table"/>)。
        /// </summary>
        /// <param name="setting">調べるギミック。</param>
        /// <param name="root">入れる先。アバターならアバターのルート。</param>
        GimmickInstallCheckResult Check(GimmickSetting setting, GameObject root);
    }

    public enum GimmickInstallCheckLevel
    {
        /// <summary>問題なし。何も出さない。</summary>
        None,

        /// <summary>知らせるだけ。導入できる。</summary>
        Info,

        /// <summary>注意。導入できる。</summary>
        Warning,

        /// <summary>入れられない。導入ボタンを押せなくする。</summary>
        Error,
    }

    /// <summary>
    /// <see cref="IGimmickInstallCheck.Check"/> の結果。
    /// </summary>
    public readonly struct GimmickInstallCheckResult
    {
        public readonly GimmickInstallCheckLevel Level;

        /// <summary>導入 UI に出す文言(訳したもの)。</summary>
        public readonly string Message;

        public GimmickInstallCheckResult(GimmickInstallCheckLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        /// <summary>問題なし。</summary>
        public static GimmickInstallCheckResult Ok => default;

        public static GimmickInstallCheckResult Info(string message) => new GimmickInstallCheckResult(GimmickInstallCheckLevel.Info, message);
        public static GimmickInstallCheckResult Warning(string message) => new GimmickInstallCheckResult(GimmickInstallCheckLevel.Warning, message);
        public static GimmickInstallCheckResult Error(string message) => new GimmickInstallCheckResult(GimmickInstallCheckLevel.Error, message);
    }
}
