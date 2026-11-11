namespace VGC.Gimmicks
{
    /// <summary>
    /// 設定ウィンドウ(<see cref="GimmickSettingsWindow"/>)での描き方を、エディタ自身が決めるときに付ける
    /// (UnityEditor.Editor の派生に付ける)。
    /// 付けていないエディタは、今までどおりウィンドウのスクロールの中で OnInspectorGUI が呼ばれる。
    ///
    /// 縦に長い設定で、上の一部を固定して残りだけをスクロールさせたいときに使う。
    /// ウィンドウの残りの高さはエディタからは分からないので、ウィンドウ側がスクロールをやめて高さを渡す。
    /// </summary>
    public interface IGimmickSettingsWindowGUI
    {
        /// <summary>
        /// false のとき、ウィンドウは自分のスクロールで囲まずに <see cref="OnSettingsWindowGUI"/> を呼ぶ。
        /// エディタは残りの高さいっぱいを使える(GUILayout.ExpandHeight(true) のスクロールビューなどで)。
        /// true のときは、今までどおりウィンドウのスクロールの中で OnInspectorGUI を呼ぶ。
        ///
        /// ウィンドウはレイアウトのたびに読むので、途中で値を変えてよい。
        /// 対象のコンポーネントが 2 つ以上あるときは、値によらずウィンドウがスクロールする。
        /// </summary>
        bool UseWindowScroll { get; }

        /// <summary>
        /// 設定ウィンドウで描くときに呼ばれる。インスペクタ(Unity の Inspector ウィンドウ)では呼ばれない。
        /// Inspector ウィンドウは全体をスクロールで囲むので、自分のスクロールはここでだけ作る。
        /// </summary>
        void OnSettingsWindowGUI();
    }
}
