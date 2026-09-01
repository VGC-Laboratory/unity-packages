using TMPro;
using UnityEngine;
using UdonSharp;
using VGC.Attributes.Runtime;
using VRC.SDKBase;

namespace VGC.GameFramework.Runtime
{
    /// <summary>
    /// エントリーUIの基底クラス。
    /// これを継承してワールド固有のUIを作る想定なので、abstract ではなく
    /// そのままでも動く既定実装として提供する（"Base" はその意図）。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EntryPanelBase : UdonSharpBehaviour
#if !COMPILER_UDONSHARP
        , IGamePlayerChanged
        , IGameHostChanged
        , IGameStateChanged
#endif
    {
        [SerializeField, HideInInspector, AutoPopulateField(typeof(StartButton), ExecutorScope.Children)] protected StartButton _startButton;
        [SerializeField, HideInInspector, AutoPopulateField(typeof(EntryButton), ExecutorScope.Children, ExecutorOrder.Hierarchy)] protected EntryButton[] _entryButtons;
        [SerializeField, Header("対象のGameSystemを指定")] private GameSystemMain _gameSystemMain;
        
        [SerializeField] private TextMeshProUGUI _hostPlayerText;
        
        internal void _OnEntryButtonClick(int index)
        {
            Debug.Log($"{nameof(EntryPanelBase)}:{nameof(_OnEntryButtonClick)}({index})");

            // 参加か退出かはここで確定させる。
            // フレームワーク側にトグルを持たせると、Owner到達時の枠の状態次第で
            // 「退出したかったのに参加してしまう」反転が起きうる
            if (_gameSystemMain.LocalEntryIndex == index)
                _gameSystemMain._RequestExitLocalPlayer();
            else
                _gameSystemMain._RequestEntry(index);
        }

        public void _OnStartButtonClick()
        {
            _gameSystemMain._RequestStartGame();
        }
        
        private void UpdateButtons()
        {
            var localEntryIndex = _gameSystemMain.LocalEntryIndex;
            var gameStarted = _gameSystemMain.US_IsGameStarted;
            // どこにもエントリーしていない場合
            if (localEntryIndex == -1)
            {
                foreach (var entryButton in _entryButtons)
                    entryButton.Interactable = !gameStarted && entryButton.PlayerId == -1;
            }
            else
            {
                for (int i = 0; i < _entryButtons.Length; i++)
                {
                    _entryButtons[i].Interactable = !gameStarted && i == localEntryIndex;
                }
            }
        }
        
        #region IGamePlayerChanged

        // GameSystemMain が SetProgramVariable で書き込む。
        // 名前は GameSystemMain.EntryArgsVariableName / ExitArgsVariableName と一致させること
        private int[] _entryArgs;
        private int[] _exitArgs;
        public int[] EntryArgs => _entryArgs;
        public int[] ExitArgs => _exitArgs;

        public void _OnEntry()
        {
            _entryButtons[_entryArgs[0]]._OnEntry(_entryArgs[1]);
            UpdateButtons();
        }

        public void _OnExit()
        {
            _entryButtons[_exitArgs[0]]._OnExit();
            UpdateButtons();
        }

        public void _OnExitAll() { }
        
        #endregion

        #region IGameHostChanged
        
        public void _OnLocalBecameHost()
        {
            if (_hostPlayerText)
                _hostPlayerText.text = $"{Networking.LocalPlayer.displayName} : ({Networking.LocalPlayer.playerId})";
        }

        public void _OnLocalLostHost()
        {
            if (_hostPlayerText)
                _hostPlayerText.text = "None";
        }

        public void _OnRemoteBecameHost()
        {
            var player = VRCPlayerApi.GetPlayerById(_gameSystemMain.US_HostPlayerId);

            if (_hostPlayerText)
                _hostPlayerText.text = $"{player.displayName} : ({player.playerId})";
        }

        public void _OnRemoteLostHost()
        {
            if (_hostPlayerText)
                _hostPlayerText.text = "None";
        }

        #endregion
        
        #region IGameStateChanged

        public void _OnGameStart()
        {
            _startButton.Interactable = false;
            UpdateButtons();
        }

        public void _OnGameEnd()
        {
            _startButton.Interactable = true;
            UpdateButtons();
        }

        #endregion
    }
}
