namespace VGC.GameFramework.Runtime
{
    public interface IGameHostChanged
    {
        /// <summary>
        /// LocalPlayerがHostになった場合に呼ばれる
        /// Lostの後に呼ばれる
        /// </summary>
        void _OnLocalBecameHost();
        /// <summary>
        /// LocalPlayerがHostじゃなくなった場合に呼ばれる
        /// Becameの前に呼ばれる
        /// </summary>
        void _OnLocalLostHost();

        /// <summary>
        /// 他のPlayerがHostになった場合に呼ばれる
        /// Lostの後に呼ばれる
        /// </summary>
        void _OnRemoteBecameHost();
        /// <summary>
        /// 他のPlayerがHostじゃなくなった場合に呼ばれる
        /// Becameの前に呼ばれる
        /// </summary>
        void _OnRemoteLostHost();
    }
}
