using System;
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
        [Header("--- GamePlayerManager ---")]
        [SerializeField, AutoPopulateField(typeof(GamePlayerBase), ExecutorScope.Children)] private GamePlayerBase[] _gamePlayers;
        [SerializeField, AutoPopulateField(typeof(IGamePlayerChanged), ExecutorScope.Children)] private UdonSharpBehaviour[] _gamePlayerChangedCallbacks;
        [SerializeField, AutoPopulateField(typeof(GameRule), ExecutorScope.Children)] private GameRule _gameRule;
        
        /// <summary>
        /// IGamePlayerChanged 実装クラスが宣言すべき int[] フィールド名。
        /// SetProgramVariable の文字列キーなので、実装側のフィールド名と必ず一致させること
        /// </summary>
        public const string EntryArgsVariableName = "_entryArgs";

        /// <inheritdoc cref="EntryArgsVariableName"/>
        public const string ExitArgsVariableName = "_exitArgs";

        [UdonSynced] private int[] us_entryPlayerIds;

        private readonly int[] _entryBuffer = new int[2];
        private readonly int[] _exitBuffer = new int[2];
        private int[] _prevEntryPlayerIds;
        private int _localEntryIndex = -1;
        
        [PublicAPI]
        public int LocalEntryIndex => _localEntryIndex;

        /// <summary>
        /// エントリー枠の数。_RequestEntryAll に渡す配列はこの長さちょうどにする
        /// </summary>
        [PublicAPI]
        public int EntryCapacity => _gamePlayers.Length;

        /// <summary>
        /// 指定枠に参加しているplayerIdを返す。空き枠と範囲外は -1
        /// </summary>
        [PublicAPI]
        public int _GetEntryPlayerId(int index)
        {
            if(!_initialized)
                return -1;

            if (index < 0 || index >= us_entryPlayerIds.Length)
                return -1;

            return us_entryPlayerIds[index];
        }

        [PublicAPI]
        public bool _HasAnyEntry()
        {
            foreach (var playerId in us_entryPlayerIds)
            {
                if(playerId != -1)
                    return true;
            }

            return false;
        }

        [PublicAPI]
        public bool _IsEntryLocalPlayer() => _IsEntry(Networking.LocalPlayer.playerId);

        [PublicAPI]
        public bool _IsEntry(int playerId) => _GetEntryIndex(playerId) != -1;

        [PublicAPI]
        public int _GetEntryIndex(int playerId)
        {
            if(!_initialized)
                return -1;
            
            for (var index = 0; index < us_entryPlayerIds.Length; index++)
            {
                if (us_entryPlayerIds[index] == playerId)
                    return index;
            }
            return -1;
        }
        
        // [NetworkCallable] の既定レートは5回/秒。超過分は破棄されず送信側でキューされるため、
        // ボタン連打をそのまま流すと遅れて連続適用される。UI直結の入口だけローカルで間引く。
        // EntryとExitでタイマーを分けて、参加直後の退出が潰れないようにする
        private const float EntryRequestInterval = 0.4f;
        private float _nextEntryRequestTime;
        private float _nextExitRequestTime;

        /// <summary>
        /// ローカルプレイヤーを指定枠に参加させる。
        /// 既にどこかに参加している場合や、枠が埋まっている場合は何もしない
        /// </summary>
        /// <param name="index">参加する枠番号</param>
        [PublicAPI]
        public void _RequestEntry(int index)
        {
            if(!_initialized)
                return;

            // publicAPIなので範囲外indexで us_entryPlayerIds[index] を踏まないようにする
            if (index < 0 || index >= us_entryPlayerIds.Length)
                return;

            var playerId = Networking.LocalPlayer.playerId;

            // 既にどこかに参加している場合は何もしない（枠移動は一度退出してから）
            if (_IsEntry(playerId))
                return;

            if (us_entryPlayerIds[index] != -1)
                return;

            if (Time.time < _nextEntryRequestTime)
                return;
            _nextEntryRequestTime = Time.time + EntryRequestInterval;

            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_EntryOwner), index, playerId);
        }

        /// <summary>
        /// まとめて開始するゲームの場合はこちらを使用する
        /// </summary>
        /// <param name="playerIds">上書きするId群</param>
        [PublicAPI]
        public void _RequestEntryAll(int[] playerIds)
        {
            if(!_initialized)
                return;

            if (playerIds.Length != us_entryPlayerIds.Length)
            {
                Debug.LogError("[VGC.GameFramework.Runtime.GameSystemMain._RequestEntryAll] 配列サイズが合いません");
                return;
            }
            
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_EntryAllOwner), playerIds);
        }
        
        /// <summary>
        /// ホストのみ実行可能
        /// 指定プレイヤーを退出させる
        /// </summary>
        /// <param name="playerId">退出させるプレイヤーID</param>
        [PublicAPI]
        public void _RequestExit(int playerId)
        {
            if (!_isHost)
            {
                Debug.LogError("[VGC.GameFramework.Runtime.GameSystemMain._RequestExit] ホストではないので実行できません。");
                return;
            }

            if(!_initialized)
                return;

            // ホストとOwnerは一致させ続ける運用だが、ずれた場合に
            // 非Ownerがsynced変数を書いても黙って捨てられるためOwner経由に統一する
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_ExitPlayerOwner), playerId);
        }
        
        /// <summary>
        /// ローカルプレイヤーを退出させる。
        /// エントリーしていない場合は何もしない
        /// </summary>
        [PublicAPI]
        public void _RequestExitLocalPlayer()
        {
            if(!_initialized)
                return;

            var playerId = Networking.LocalPlayer.playerId;
            if (!_IsEntry(playerId))
                return;

            if (Time.time < _nextExitRequestTime)
                return;
            _nextExitRequestTime = Time.time + EntryRequestInterval;

            // 枠番号は渡さない。Owner側で現在の在席枠を引き直すことで、
            // 転送中に枠が変わっていた場合の取り違えを避ける
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_ExitLocalPlayerOwner), playerId);
        }

        /// <summary>
        /// 全てのプレイヤーを強制的に退出させる
        /// ゲーム終了時であるIGameStateChanged._OnGameEndで呼ぶ想定
        /// </summary>
        [PublicAPI]
        public void _RequestExitAll()
        {
            if(!_initialized)
                return;
            
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_ExitAllOwner));
        }

        /// <summary>
        /// 呼び出し元本人を指定枠に参加させる
        /// </summary>
        [NetworkCallable]
        public void _EntryOwner(int index, int playerId)
        {
            if (!ValidateOwnerNetworkCall())
                return;

            if(!_initialized)
                return;

            // 範囲外のindexは us_entryPlayerIds[index] で IndexOutOfRange になりUdonが停止する
            if (index < 0 || index >= us_entryPlayerIds.Length)
                return;

            // 他人を勝手にエントリーさせられないよう本人か確認する
            if (!IsCallingPlayer(playerId))
                return;

            // 空き枠でなければ何もしない。Exitに転じることは無い
            if (us_entryPlayerIds[index] != -1)
                return;

            if(_IsEntry(playerId))
                return;

            if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId)))
                return;

            SetEntry(index, playerId);
        }
        
        /// <summary>
        /// ホストのみ実行可能。指定プレイヤーを退出させる
        /// </summary>
        [NetworkCallable]
        public void _ExitPlayerOwner(int playerId)
        {
            if (!ValidateOwnerNetworkCall())
                return;

            if(!_initialized)
                return;

            // ホスト限定APIなので、呼び出し元がホスト本人かOwner側で検証する
            if (!IsCallingHost())
                return;

            ExitPlayer(playerId);
        }

        /// <summary>
        /// 呼び出し元本人を退出させる
        /// </summary>
        [NetworkCallable]
        public void _ExitLocalPlayerOwner(int playerId)
        {
            if (!ValidateOwnerNetworkCall())
                return;

            if(!_initialized)
                return;

            // 他人を勝手に退出させられないよう本人か確認する
            if (!IsCallingPlayer(playerId))
                return;

            ExitPlayer(playerId);
        }

        [NetworkCallable]
        public void _EntryAllOwner(int[] playerIds)
        {
            if (!ValidateOwnerNetworkCall())
                return;

            if(!_initialized)
                return;

            // ホスト以外からの呼び出しも許可しているぶん、配列は必ず検証する。
            // 呼び出し元は任意の長さを送れるため、_RequestEntryAll 側のチェックは信用できない
            if (playerIds == null || playerIds.Length != us_entryPlayerIds.Length)
            {
                Debug.LogError("[VGC.GameFramework.Runtime.GameSystemMain._EntryAllOwner] 配列サイズが合いません");
                return;
            }

            for (int i = 0; i < us_entryPlayerIds.Length; i++)
            {
                var playerId = playerIds[i];
                // 無効なIdは空き枠(-1)として扱う。前の値を残さず必ず上書きする
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(playerId)))
                {
                    us_entryPlayerIds[i] = -1;
                    continue;
                }

                // 同じプレイヤーが複数枠に入らないようにする(先に現れた枠を優先)
                bool duplicated = false;
                for (int j = 0; j < i; j++)
                {
                    if (us_entryPlayerIds[j] == playerId)
                    {
                        duplicated = true;
                        break;
                    }
                }
                us_entryPlayerIds[i] = duplicated ? -1 : playerId;
            }

            UpdateEntryPlayers();
            RequestSerialization();
        }

        [NetworkCallable]
        public void _ExitAllOwner()
        {
            if (!ValidateOwnerNetworkCall())
                return;

            if(!_initialized)
                return;

            for (int i = 0; i < us_entryPlayerIds.Length; i++)
            {
                us_entryPlayerIds[i] = -1;
            }
            UpdateEntryPlayers();
            RequestSerialization();
        }

        private void SetEntry(int index, int playerId)
        {
            us_entryPlayerIds[index] = playerId;
            UpdateEntryPlayers();
            RequestSerialization();
        }

        /// <summary>
        /// Owner専用。指定プレイヤーが在席している枠を探して退出させる。
        /// 在席していなければ何もしない
        /// </summary>
        private void ExitPlayer(int playerId)
        {
            for (var index = 0; index < us_entryPlayerIds.Length; index++)
            {
                if (us_entryPlayerIds[index] == playerId)
                {
                    ClearEntry(index);
                    return;
                }
            }
        }

        private void ClearEntry(int index)
        {
            us_entryPlayerIds[index] = -1;
            UpdateEntryPlayers();
            RequestSerialization();
        }
        
        public void _LeftPlayerCheck()
        {
            if(!Networking.IsOwner(gameObject))
                return;
            
            InitializeIfNecessary();
            bool isAnyExitPlayer = false;
            for (int index = 0; index < us_entryPlayerIds.Length; index++)
            {
                if(us_entryPlayerIds[index] == -1)
                    continue;
                
                if (!Utilities.IsValid(VRCPlayerApi.GetPlayerById(us_entryPlayerIds[index])))
                {
                    us_entryPlayerIds[index] = -1;
                    isAnyExitPlayer = true;
                }
            }
            
            if(isAnyExitPlayer)
                UpdateEntryPlayers();
            
            bool isAnyUdonSyncedChanged = isAnyExitPlayer;
            // Hostの移譲が発生
            if (US_HostPlayerId != -1 && !Utilities.IsValid(VRCPlayerApi.GetPlayerById(US_HostPlayerId)))
            {
                Debug.Log($"[VGC.GameFramework.Runtime.GameSystemMain._LeftPlayerCheck] LostHost: {US_HostPlayerId}");
                if (_autoHostSetup)
                {
                    int lostPlayerId = US_HostPlayerId;
                    // ゲーム参加者から適当に振り分け
                    foreach (var entryPlayerId in us_entryPlayerIds)
                    {
                        if(entryPlayerId == -1)
                            continue;
                        
                        US_HostPlayerId = entryPlayerId;
                        break;
                    }

                    if (US_HostPlayerId == -1 || US_HostPlayerId == lostPlayerId)
                    {
                        US_HostPlayerId = Networking.LocalPlayer.playerId;
                    }
                    Debug.Log($"[VGC.GameFramework.Runtime.GameSystemMain._LeftPlayerCheck] BecameHost: {US_HostPlayerId}");
                }
                else
                {
                    US_HostPlayerId = -1;
                }
                isAnyUdonSyncedChanged = true;
            }
            
            if (isAnyUdonSyncedChanged)
                RequestSerialization();
        }
        
        private void UpdateEntryPlayers()
        {
            if (!_initialized)
                return;
            
            for (int gamePlayerIndex = 0; gamePlayerIndex < us_entryPlayerIds.Length; gamePlayerIndex++)
            {
                var playerId = us_entryPlayerIds[gamePlayerIndex];
                if (_prevEntryPlayerIds[gamePlayerIndex] != playerId)
                {
                    _gamePlayers[gamePlayerIndex]._OnChangePlayer(playerId);

                    if (playerId == -1)
                    {
                        Debug.Log($"[VGC.GameFramework.Runtime.GameSystemMain.UpdateEntryPlayers] 退出しました。GamePlayerIndex:{gamePlayerIndex}");
                        if (_localEntryIndex == gamePlayerIndex)
                            _localEntryIndex = -1;

                        bool exitAll = !_HasAnyEntry();

                        _exitBuffer[0] = gamePlayerIndex;
                        _exitBuffer[1] = _prevEntryPlayerIds[gamePlayerIndex];
                        foreach (var gamePlayerChangedCallback in _gamePlayerChangedCallbacks)
                        {
                            gamePlayerChangedCallback.SetProgramVariable(ExitArgsVariableName, _exitBuffer);
                            gamePlayerChangedCallback.SendCustomEvent(nameof(IGamePlayerChanged._OnExit));
                        }

                        if (exitAll)
                        {
                            Debug.Log("[VGC.GameFramework.Runtime.GameSystemMain.UpdateEntryPlayers] 全てのプレイヤーが退出しました。");
                            foreach (var gamePlayerChangedCallback in _gamePlayerChangedCallbacks)
                            {
                                gamePlayerChangedCallback.SendCustomEvent(nameof(IGamePlayerChanged._OnExitAll));
                            }

                            // UpdateEntryPlayers は OnDeserialization からも呼ばれる（＝全クライアントで走る）ため、
                            // synced 変数を書く EndGame は Owner でのみ実行する
                            if (Networking.IsOwner(gameObject))
                                EndGame();
                        }
                    }
                    else
                    {
                        Debug.Log($"[VGC.GameFramework.Runtime.GameSystemMain.UpdateEntryPlayers] 参加しました。GamePlayerIndex:{gamePlayerIndex}, PlayerId :{playerId}");
                        if (Networking.LocalPlayer.playerId == playerId)
                        {
                            _localEntryIndex = gamePlayerIndex;
                        }
                        else if(_localEntryIndex == gamePlayerIndex)
                        {
                            _localEntryIndex = -1;
                        }
                        
                        _entryBuffer[0] = gamePlayerIndex;
                        _entryBuffer[1] = playerId;
                        foreach (var gamePlayerChangedCallback in _gamePlayerChangedCallbacks)
                        {
                            gamePlayerChangedCallback.SetProgramVariable(EntryArgsVariableName, _entryBuffer);
                            gamePlayerChangedCallback.SendCustomEvent(nameof(IGamePlayerChanged._OnEntry));
                        }
                    }
                }
            }

            Array.Copy(us_entryPlayerIds, _prevEntryPlayerIds, us_entryPlayerIds.Length);
        }
    }
}
