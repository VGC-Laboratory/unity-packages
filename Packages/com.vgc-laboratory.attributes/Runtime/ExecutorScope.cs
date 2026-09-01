namespace VGC.Attributes.Runtime
{
    /// <summary>
    /// Executor属性がコンポーネントを検索する範囲
    /// </summary>
    public enum ExecutorScope
    {
        /// <summary>
        /// シーン全体から検索します
        /// </summary>
        Scene,

        /// <summary>
        /// 自身のGameObjectに存在するコンポーネントのみ検索します
        /// </summary>
        Self,

        /// <summary>
        /// 自身を基準に子階層全体から検索します(自身含む)
        /// </summary>
        Children,

        /// <summary>
        /// 自身を基準に子階層全体から検索します(自身を含まない)
        /// </summary>
        ChildrenExcludeSelf,

        /// <summary>
        /// 自身を含む親階層全体から検索します
        /// </summary>
        Parents,

        /// <summary>
        /// 直接の親GameObjectのみ検索します
        /// </summary>
        Parent,

        /// <summary>
        /// 直接の親GameObjectを基準に子階層全体から検索します(直接の親GameObject含む)
        /// </summary>
        ParentHierarchy,

        /// <summary>
        /// 最も近い親階層でAnchorTypeを持つGameObjectを検索し、
        /// そのGameObjectのみを検索対象にします。
        /// 親探索時は非アクティブ状態を無視します。
        /// </summary>
        NearestParent,

        /// <summary>
        /// 最も近い親階層でAnchorTypeを持つGameObjectを検索し、
        /// そのGameObjectを基準に子階層全体を検索します。
        /// 親探索時は非アクティブ状態を無視します。
        /// </summary>
        NearestParentHierarchy,

        /// <summary>
        /// ルートGameObjectのみ検索します
        /// </summary>
        Root,

        /// <summary>
        /// ルートGameObjectを基準に子階層全体から検索します(ルートGameObject含む)
        /// </summary>
        RootHierarchy,
    }
}
