using VGC.Core.Runtime;

namespace VGC.SampleGame.Runtime
{
    public partial class GameMain
    {
        private const double MainPhaseCountDownTime = 10;
        
        private void InitializeMainPhase()
        {
        }

        private void UpdateMainPhase()
        {
            TimeHelper.ShowCountDownTime(us_syncedStartTime, MainPhaseCountDownTime, _countDownText, out var remainTime);
            if (remainTime <= 0)
            {
                if (_isHost)
                {
                    SetPhase(GamePhaseSample.EndPhase, false);
                }
            }
        }
    }
}