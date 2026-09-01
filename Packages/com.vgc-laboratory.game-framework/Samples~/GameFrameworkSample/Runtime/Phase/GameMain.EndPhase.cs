namespace VGC.GameFrameworkSample.Runtime
{
    public partial class GameMain
    {
        private void InitializeEndPhase()
        {
            if (!_isHost)
                return;

            // ApplyPhase() から同期的に呼ぶと
            // _RequestEndGame -> _OnGameEnd -> _RequestExitAll -> _OnExitAll -> SetPhase(IdlePhase)
            // が同じコールスタックでネストし、外側の SetPhase の RequestSerialization() が
            // IdlePhase を送ってしまうため1フレーム遅らせる
            SendCustomEventDelayedFrames(nameof(_RequestEndGameDelayed), 1);
        }

        public void _RequestEndGameDelayed()
        {
            if (!_isHost)
                return;

            // 遅延中にフェーズが進んでいたら何もしない
            if (US_Phase != GamePhaseSample.EndPhase)
                return;

            _gameSystem._RequestEndGame();
        }
    }
}