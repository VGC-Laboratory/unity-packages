namespace VGC.SampleGame.Runtime
{
    public partial class GameMain
    {
        private void InitializeIdlePhase()
        {
            if (_countDownText)
                _countDownText.text = "";
        }
    }
}