using VGC.Core.Runtime;
using VRC.SDKBase;

namespace VGC.SampleGame.Runtime
{
    public partial class GameMain
    {
        private const double StartPhaseCountDownTime = 3;
        
        private void InitializeStartPhase()
        {
            // Entryしているプレイヤーはゲーム会場にテレポートなどさせる
            var entryIndex = _gameSystem._GetEntryIndex(Networking.LocalPlayer.playerId);
            if (entryIndex != -1)
            {
                // Networking.LocalPlayer.TeleportTo();
            }
        }

        private void UpdateStartPhase()
        {
            TimeHelper.ShowCountDownTime(us_syncedStartTime, StartPhaseCountDownTime, _countDownText, out var remainTime);
            if (remainTime <= 0)
            {
                if (_isHost)
                {
                    SetPhase(GamePhaseSample.MainPhase, true);
                }
            }
        }
    }
}