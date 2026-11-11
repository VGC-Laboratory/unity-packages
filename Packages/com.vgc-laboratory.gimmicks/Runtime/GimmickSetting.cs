using UnityEngine;
using VGC.Localization;

namespace VGC.Gimmicks
{
    /// <summary>
    /// ギミック 1 つの定義(版・プレハブ・依存)。
    ///
    /// 同じフォルダに LocalizationTable を置く。表の "GimmickName" の訳が表示名になる。
    /// 最新バージョンの確認のキーは <see cref="VersionKey"/>(未設定なら表のシート名)。
    ///
    /// <see cref="Publisher"/> に配布元の名前を入れると、導入 UI がその名前ごとに
    /// まとめて並べる。別々の配布元のギミックが同じプロジェクトに入っても混ざらない。
    /// </summary>
    [CreateAssetMenu(fileName = nameof(GimmickSetting), menuName = "VGC/Gimmicks/" + nameof(GimmickSetting))]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GimmickSetting : ScriptableObject
    {
        [Tooltip("配布元の名前。導入 UI はこの名前ごとにギミックをまとめて並べる。自分の名前やサークル名を入れる。空でも動くが、他の配布元のギミックと並んだときに見分けが付かない")]
        [SerializeField] private string _publisher;

        [SerializeField] private string _version = "1.0.0";

        [Tooltip("最新版の一覧を読む取得元。空なら最新バージョンの確認をしない。VGC のギミックなら com.vgc-laboratory.gimmicks の Config/VGCAvatarGimmicks か VGCWorldGimmicks を入れる")]
        [SerializeField] private VersionSource _versionSource;

        [Tooltip("版の一覧でのキー。空なら同じフォルダの LocalizationTable のシート名")]
        [SerializeField] private string _versionKey;

        [SerializeField] private GameObject _maPrefab;
        [SerializeField] private GameObject[] _dependencies;
        [SerializeField] private GameObject _crossPlatformPrefab;
        [SerializeField] private bool _crossPlatformReady;
        [SerializeField] private bool _useSetting;

        /// <summary>配布元の名前。導入 UI のまとめ方に使う。空でもよい。</summary>
        public string Publisher => _publisher;

        public string Version => _version;
        public VersionSource VersionSource => _versionSource;
        public GameObject Prefab => _maPrefab;
        public GameObject[] Dependencies => _dependencies;
        public GameObject CrossPlatformPrefab => _crossPlatformPrefab;
        public bool CrossPlatformReady => _crossPlatformReady;
        public bool UseSetting => _useSetting;

#if UNITY_EDITOR
        /// <summary>同じフォルダにある翻訳表。</summary>
        public LocalizationTable Table => LocalizationTableResolver.FindNextTo(this);

        public string SheetName => Table != null ? Table.SheetName : null;

        public string VersionKey => string.IsNullOrEmpty(_versionKey) ? SheetName : _versionKey;

        public string Name => Table != null ? Table.Get("GimmickName") : name;
#else
        public string Name => name;
#endif
    }
}
