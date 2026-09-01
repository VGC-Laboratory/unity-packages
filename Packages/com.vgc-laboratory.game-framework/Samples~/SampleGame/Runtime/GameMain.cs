using TMPro;
using UdonSharp;
using UnityEngine;
using VGC.Attributes.Runtime;
using VGC.GameFramework.Runtime;
using VRC.SDKBase;

namespace VGC.SampleGame.Runtime
{
    public enum GamePhaseSample
    {
        IdlePhase,
        StartPhase,
        MainPhase,
        EndPhase
    }

    [AddComponentMenu("GameMainSample")]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public partial class GameMain : UdonSharpBehaviour
#if !COMPILER_UDONSHARP
        , IGameHostChanged
        , IGameStateChanged
        , IGamePlayerChanged
#endif
    {
        [SerializeField] private TextMeshProUGUI _showPhaseText;
        [SerializeField] private TextMeshProUGUI _countDownText;
        
        [Header("--- AutoReferences ---")]
        [SerializeField, AutoPopulateField(typeof(GameSystemMain), ExecutorScope.Parents)] private GameSystemMain _gameSystem;
        [SerializeField, AutoPopulateField(typeof(GamePlayerSample), ExecutorScope.ParentHierarchy)] private GamePlayerSample[] _gamePlayers;

        [UdonSynced] private double us_syncedStartTime;
        [UdonSynced] private GamePhaseSample us_phase;

        public GamePhaseSample US_Phase => us_phase;

        private GamePhaseSample _appliedPhase;
        private bool _phaseApplied;
        private bool _isHost;

        void Update()
        {
            // 毎フレームの処理が要るのはカウントダウン表示があるフェーズだけ。
            // IdlePhase / EndPhase では何もしない。
            //
            // Updateを専用コンポーネントに切り出して enabled で止める案は採らない。
            // UdonはBehaviourが増えるほど重く、別Behaviourへの呼び出しは
            // 同一Behaviour/partial分割の約1.5倍のコストになる。
            // 切り出すと毎フレームのカウントダウン処理がその高いほうに落ちるうえ、
            // enabledの管理を誤ると進行が止まる。
            // GameMainを単一のコーディネータ（partial分割）に保つ現状が適切。
            switch (us_phase)
            {
                case GamePhaseSample.StartPhase:
                    UpdateStartPhase();
                    break;
                case GamePhaseSample.MainPhase:
                    UpdateMainPhase();
                    break;
            }
        }

        public override void OnDeserialization()
        {
            // FieldChangeCallback は synced 変数を1個適用するたびに即発火するため、
            // us_phase のコールバック時点で us_syncedStartTime がまだ古い可能性がある。
            // スナップショットが揃ったここで適用する。
            ApplyPhase();
        }

        /// <summary>
        /// ホストのみ。フェーズを進めて同期する
        /// </summary>
        /// <param name="phase">遷移先フェーズ</param>
        /// <param name="resetCountDown">カウントダウン開始時刻を現在時刻で更新するか</param>
        private void SetPhase(GamePhaseSample phase, bool resetCountDown)
        {
            // フェーズより先に時刻を確定させる（Initialize<Phase>() が時刻を読む場合があるため）
            if (resetCountDown)
                us_syncedStartTime = Networking.GetServerTimeInSeconds();

            us_phase = phase;
            ApplyPhase();
            RequestSerialization();
        }

        /// <summary>
        /// 現在の us_phase を表示・Initialize に反映する（冪等）
        /// </summary>
        private void ApplyPhase()
        {
            if (_phaseApplied && _appliedPhase == us_phase)
                return;

            _appliedPhase = us_phase;
            _phaseApplied = true;

            if (_showPhaseText)
                _showPhaseText.text = GetGamePhaseName(us_phase);

            switch (us_phase)
            {
                case GamePhaseSample.IdlePhase:
                    InitializeIdlePhase();
                    break;
                case GamePhaseSample.StartPhase:
                    InitializeStartPhase();
                    break;
                case GamePhaseSample.MainPhase:
                    InitializeMainPhase();
                    break;
                case GamePhaseSample.EndPhase:
                    InitializeEndPhase();
                    break;
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            // OnPlayerRespawn はリスポーンした本人のクライアントでのみ発火するため、
            // _isHost で弾くと「ホストがリスポーンしたとき」しか動かない。
            // 自分の枠の退出をOwnerへリクエストする形にする。
            if (!player.isLocal)
                return;

            // リスポーンしたら強制退出をさせる
            _gameSystem._RequestExitLocalPlayer();
        }

        #region IGameHostChanged

        public void _OnLocalBecameHost()
        {
            _isHost = true;
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _OnLocalLostHost()
        {
            _isHost = false;
        }

        public void _OnRemoteBecameHost()
        {
        }

        public void _OnRemoteLostHost()
        {
        }

        #endregion

        #region IGameStateChanged

        public void _OnGameStart()
        {
            Debug.Log("[VGC.GameMainSample] ゲームを開始しました。");
            if (_isHost)
            {
                SetPhase(GamePhaseSample.StartPhase, true);
            }
        }

        public void _OnGameEnd()
        {
            Debug.Log("[VGC.GameMainSample] ゲームを終了しました。");
            // Entryしているプレイヤーは登録地点にテレポートなどさせる
            if (_gameSystem._IsEntryLocalPlayer())
            {
                // Networking.LocalPlayer.TeleportTo();
            }
            
            if(!_isHost)
                return;
            
            // 全てのプレイヤーを退出させてゲーム終了
            // IGamePlayerChanged._OnExitAll()内でIdlePhaseに戻す
            _gameSystem._RequestExitAll();
        }

        #endregion
        
        #region IGamePlayerChanged
        
        // GameSystemMain が SetProgramVariable で書き込む。
        // 名前は GameSystemMain.EntryArgsVariableName / ExitArgsVariableName と一致させること
        private int[] _entryArgs;
        private int[] _exitArgs;
        public int[] EntryArgs => _entryArgs;
        public int[] ExitArgs => _exitArgs;
        
        public void _OnEntry()
        {
            /*int gamePlayerIndex = EntryArgs[0];
            int playerId = EntryArgs[1];*/
        }

        public void _OnExit()
        {
            /*int gamePlayerIndex = ExitArgs[0];
            int playerId = ExitArgs[1];*/
        }

        public void _OnExitAll()
        {
            Debug.Log("[VGC.GameMainSample] 全てのプレイヤーが退出しました。");
            if(!_isHost)
                return;
            
            // Idle状態に戻る
            SetPhase(GamePhaseSample.IdlePhase, false);
        }

        #endregion
        
        private static string GetGamePhaseName(GamePhaseSample phase)
        {
            switch (phase)
            {
                case GamePhaseSample.IdlePhase:
                    return nameof(GamePhaseSample.IdlePhase);

                case GamePhaseSample.StartPhase:
                    return nameof(GamePhaseSample.StartPhase);

                case GamePhaseSample.MainPhase:
                    return nameof(GamePhaseSample.MainPhase);

                case GamePhaseSample.EndPhase:
                    return nameof(GamePhaseSample.EndPhase);

                default:
                    return "";
            }
        }
    }
}