namespace VGC.GameFramework.Runtime
{
    /// <summary>
    /// エントリー枠の増減通知を受け取るインターフェース。
    /// 実装クラスは UdonSharpBehaviour を継承していれば基底クラスは問わない
    /// （UdonSharpBehaviour は単一継承なので、専用の基底クラスは用意しない）。
    ///
    /// 引数は Udon の制約で SendCustomEvent に乗せられないため、
    /// GameSystemMain が SetProgramVariable でフィールドへ直接書き込む。
    /// 実装クラスは以下と同名の int[] フィールドを必ず宣言すること。
    /// フィールド名は文字列キーで解決されるため、綴りがずれると静かに動かなくなる。
    ///
    /// <list type="bullet">
    /// <item><see cref="GameSystemMain.EntryArgsVariableName"/> （既定: _entryArgs）</item>
    /// <item><see cref="GameSystemMain.ExitArgsVariableName"/> （既定: _exitArgs）</item>
    /// </list>
    /// </summary>
    public interface IGamePlayerChanged
    {
#if !COMPILER_UDONSHARP
        /// <summary>
        /// Args(int gamePlayerIndex, int playerId)
        /// </summary>
        int[] EntryArgs { get; }

        /// <summary>
        /// Args(int gamePlayerIndex, int playerId)
        /// </summary>
        int[] ExitArgs { get; }
#endif
        void _OnEntry();
        void _OnExit();
        void _OnExitAll();
    }
}
