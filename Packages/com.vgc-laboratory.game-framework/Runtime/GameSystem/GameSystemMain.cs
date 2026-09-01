using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

namespace VGC.GameFramework.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed partial class GameSystemMain : UdonSharpBehaviour
    {
        private bool _initialized;
        
        private void Start()
        {
            if (_autoHostSetup)
            {
                if (Networking.IsOwner(gameObject))
                {
                    US_HostPlayerId = Networking.LocalPlayer.playerId;
                    RequestSerialization();
                }
            }
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if(!player.isLocal)
                return;
            
            if (Networking.IsOwner(gameObject))
            {
                InitializeIfNecessary();
            }
        }

        private void InitializeIfNecessary()
        {
            if (_initialized)
                return;
            
            if (Networking.IsOwner(gameObject))
            {
                us_entryPlayerIds = new int[_gamePlayers.Length];
                for (int i = 0; i < us_entryPlayerIds.Length; i++)
                {
                    us_entryPlayerIds[i] = -1;
                }
                RequestSerialization();
            }
            else
            {
                // 非Ownerは同期データを受け取るまで初期化しない
                // （ここで _initialized を立てると us_entryPlayerIds が null のまま参照される）
                if (us_entryPlayerIds == null)
                    return;

                // 枠数が食い違うとUpdateEntryPlayersでIndexOutOfRangeになりUdonが停止する
                if (us_entryPlayerIds.Length != _gamePlayers.Length)
                {
                    Debug.LogError($"[VGC.GameFramework.Runtime.GameSystemMain.InitializeIfNecessary] エントリー枠数が一致しません。synced:{us_entryPlayerIds.Length}, _gamePlayers:{_gamePlayers.Length}");
                    return;
                }
            }

            _prevEntryPlayerIds = new int[_gamePlayers.Length];
            for (int i = 0; i < _prevEntryPlayerIds.Length; i++)
            {
                _prevEntryPlayerIds[i] = -1;
            }
            _initialized = true;
        }
        
        public override void OnDeserialization()
        {
            InitializeIfNecessary();
            UpdateEntryPlayers();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if(!player.isLocal)
                return;
            
            // 安全のため複数回実行する
            SendCustomEventDelayedSeconds(nameof(_LeftPlayerCheck), 0.5f);
            SendCustomEventDelayedSeconds(nameof(_LeftPlayerCheck), 15f);
        }
        
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if(!Networking.IsOwner(gameObject))
                return;
            
            // 退出プレイヤーがOwnerの場合はOnOwnershipTransferredに任せる
            if(player.isLocal)
                return;
            
            if(!_initialized)
                return;

            ExitPlayer(player.playerId);
        }

        #region NetworkCallable共通検証

        /// <summary>
        /// Owner宛ネットワークイベントの入口で共通に行う検証。
        /// [NetworkCallable] メソッドの先頭で必ず呼ぶこと。
        /// </summary>
        private bool ValidateOwnerNetworkCall()
        {
            // ネットワーク経由の場合、呼び出し元が取得できない/無効なら破棄する
            if (NetworkCalling.InNetworkCall && !Utilities.IsValid(NetworkCalling.CallingPlayer))
                return false;

            // synced変数を書けるのはOwnerのみ。
            // 転送中にOwnerが移ると非Ownerに届くことがあるため必ず弾く
            return Networking.IsOwner(gameObject);
        }

        /// <summary>
        /// 引数で渡されたplayerIdが呼び出し元本人のものか検証する。
        /// ネットワーク引数は呼び出し元が自由に詐称できるため、
        /// 本人性が要る操作では必ずこれを通す。
        /// </summary>
        private bool IsCallingPlayer(int playerId)
        {
            // ローカル呼び出しは検証不要
            if (!NetworkCalling.InNetworkCall)
                return true;

            var caller = NetworkCalling.CallingPlayer;
            return Utilities.IsValid(caller) && caller.playerId == playerId;
        }

        /// <summary>
        /// ホスト限定操作の呼び出し元がホスト本人か検証する
        /// </summary>
        private bool IsCallingHost()
        {
            // ローカル呼び出しはローカルのホスト判定で足りる
            if (!NetworkCalling.InNetworkCall)
                return _isHost;

            var caller = NetworkCalling.CallingPlayer;
            return Utilities.IsValid(caller) && caller.playerId == US_HostPlayerId;
        }

        #endregion
    }
}
