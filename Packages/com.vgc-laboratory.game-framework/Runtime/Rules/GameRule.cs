using UdonSharp;

namespace VGC.GameFramework.Runtime
{
    public abstract class GameRule : UdonSharpBehaviour
    {
        public virtual bool _CanStartGame(GameSystemMain gameSystemMain)
        {
            return gameSystemMain._HasAnyEntry();
        }
    }
}
