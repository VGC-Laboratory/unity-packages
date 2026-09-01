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

        [PublicAPI]
        public void _RequestSetHost()
        {
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

            if (US_HostPlayerId != -1)
            {
                Debug.LogError("[VGC.GameFramework.Runtime.GameSystemMain._SetHostOwner] ホスト権限の移譲に失敗しました。");
                return;
            }

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
