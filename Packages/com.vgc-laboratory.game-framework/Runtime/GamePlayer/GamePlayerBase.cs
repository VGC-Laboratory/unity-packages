using UdonSharp;
using VRC.SDKBase;

namespace VGC.GameFramework.Runtime
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public abstract class GamePlayerBase : UdonSharpBehaviour
    {
        protected VRCPlayerApi _player;
        
        public virtual void _OnChangePlayer(int playerId)
        {
            if (playerId == -1)
            {
                _player = null;
            }
            else
            {
                _player = VRCPlayerApi.GetPlayerById(playerId);
            }
        }
    }
}