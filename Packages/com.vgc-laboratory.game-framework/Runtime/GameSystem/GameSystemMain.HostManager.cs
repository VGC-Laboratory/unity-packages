using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VGC.Attributes.Runtime;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VGC.GameFramework.Runtime
{
    public sealed partial class GameSystemMain
    {
        [Header("--- GameHostManager ---")]
        [SerializeField, Header("自動Host振り分け")] private bool _autoHostSetup = true;
        [SerializeField, AutoPopulateField(typeof(IGameHostChanged), ExecutorScope.Children)] private UdonSharpBehaviour[] _gameHostChangedCallbacks;
        [UdonSynced, FieldChangeCallback(nameof(US_HostPlayerId))] private int us_hostPlayerId = -1;
        private bool _isHost;
        
        public int US_HostPlayerId
        {
            get => us_hostPlayerId;
            set
            {
                us_hostPlayerId = value;
                OnChangeHost();
            }
        }
        
        private int _prevHostPlayerId = -1;

        private const float SetHostRequestInterval = 0.4f;
        private float _nextSetHostRequestTime;

        /// <summary>
        /// ローカルプレイヤーがホストを取得する。
        /// 既に他プレイヤーがホストでも奪い取れるが、ゲーム中は受理されない。
        /// 高性能なPCの参加者が開始前に自主的にホストを引き受ける、といった用途を想定する
        /// </summary>
        [PublicAPI]
        public void _RequestSetHost()
        {
            // ゲーム中の移譲は不可。進行中にホストが変わるとフェーズ管理が破綻する
            if (US_IsGameStarted)
                return;

            if (_isHost)
                return;

            if (Time.time < _nextSetHostRequestTime)
                return;
            _nextSetHostRequestTime = Time.time + SetHostRequestInterval;

            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_SetHostOwner), Networking.LocalPlayer.playerId);
        }

        [NetworkCallable]
        public void _SetHostOwner(int playerId)
        {
            if (!ValidateOwnerNetworkCall())
                return;

            // 他人になりすましてホストを取られないよう本人か確認する
            if (!IsCallingPlayer(playerId))
                return;

            if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId)))
                return;

            // ゲーム中は拒否する。UI側でボタンを無効化していても [NetworkCallable] は
            // 直接呼べるため、判定はOwner側に置く必要がある
            if (US_IsGameStarted)
                return;

            if (US_HostPlayerId == playerId)
                return;

            US_HostPlayerId = playerId;
            RequestSerialization();
        }
        
        private void OnChangeHost()
        {
            int localId = Networking.LocalPlayer.playerId;
            int prev = _prevHostPlayerId;
            int now  = US_HostPlayerId;
            Debug.Log($"[VGC.GameFramework.Runtime.GameSystemMain.OnChangeHost] ChangeGameHost: {now}");

            // 状態確定
            _isHost = now == localId;
            if (_isHost && !Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            
            // Callback実行
            foreach (var callback in _gameHostChangedCallbacks)
            {
                // Lost（prev 基準）
                if (prev != -1 && prev != now)
                    callback.SendCustomEvent(prev == localId ? nameof(IGameHostChanged._OnLocalLostHost) : nameof(IGameHostChanged._OnRemoteLostHost));
                
                // Became（now 基準）
                if (now != -1 && prev != now)
                    callback.SendCustomEvent(now == localId ? nameof(IGameHostChanged._OnLocalBecameHost) : nameof(IGameHostChanged._OnRemoteBecameHost));
            }

            _prevHostPlayerId = now;
        }
    }
}
