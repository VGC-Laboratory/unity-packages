using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VGC.Attributes.Runtime;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

namespace VGC.GameFramework.Runtime
{
    public sealed partial class GameSystemMain
    {
        [Header("--- GameStateManager ---")]
        [SerializeField, AutoPopulateField(typeof(IGameStateChanged), ExecutorScope.Children)] private UdonSharpBehaviour[] _gameStateChangedCallbacks;
        
        [UdonSynced, FieldChangeCallback(nameof(US_IsGameStarted))] private bool us_isGameStarted;

        private const float StartGameRequestInterval = 0.4f;
        private float _nextStartGameRequestTime;

        public bool US_IsGameStarted
        {
            get => us_isGameStarted;
            set
            {
                us_isGameStarted = value;
                if (us_isGameStarted)
                {
                    foreach (var callback in _gameStateChangedCallbacks)
                    {
                        callback.SendCustomEvent(nameof(IGameStateChanged._OnGameStart));
                    }
                }
                else
                {
                    foreach (var callback in _gameStateChangedCallbacks)
                    {
                        callback.SendCustomEvent(nameof(IGameStateChanged._OnGameEnd));
                    }
                }
            }
        }
        
        /// <summary>
        /// ゲーム開始時に呼び出す
        /// 基本的にはStartボタンなどから呼ぶ想定
        /// </summary>
        [PublicAPI]
        public void _RequestStartGame()
        {
            if(US_IsGameStarted)
                return;

            // Startボタン直結の入口。連打分が送信側にキューされないよう間引く
            // （_RequestToggleEntry とはタイマーを分けて、Entry直後のStartを潰さないようにする）
            if (Time.time < _nextStartGameRequestTime)
                return;
            _nextStartGameRequestTime = Time.time + StartGameRequestInterval;

            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_StartGameOwner));
        }
        
        /// <summary>
        /// ゲーム終了時に呼び出す
        /// 基本的にはゲーム終了フェイズなどで呼ぶ
        /// </summary>
        [PublicAPI]
        public void _RequestEndGame()
        {
            if(!US_IsGameStarted)
                return;
            
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_EndGameOwner));
        }

        [NetworkCallable]
        public void _StartGameOwner()
        {
            if (!ValidateOwnerNetworkCall())
                return;

            StartGame();
        }

        [NetworkCallable]
        public void _EndGameOwner()
        {
            if (!ValidateOwnerNetworkCall())
                return;

            EndGame();
        }

        private void StartGame()
        {
            if(US_IsGameStarted)
                return;

            if (_gameRule != null)
            {
                if (!_gameRule._CanStartGame(this))
                {
                    return;
                }
            }
            else
            {
                // 誰もエントリーしていない場合
                if (!_HasAnyEntry())
                {
                    return;
                }
            }

            US_IsGameStarted = true;
            RequestSerialization();
        }

        /// <summary>
        /// Owner専用。ローカルから直接呼ぶ場合はIsOwnerを確認してから呼ぶこと
        /// </summary>
        private void EndGame()
        {
            if(!US_IsGameStarted)
                return;

            US_IsGameStarted = false;
            RequestSerialization();
        }
    }
}
