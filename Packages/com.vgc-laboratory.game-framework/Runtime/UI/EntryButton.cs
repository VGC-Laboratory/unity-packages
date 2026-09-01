using TMPro;
using UnityEngine;
using VGC.Attributes.Runtime;
using VGC.UIExtension.Runtime;
using VRC.SDKBase;

namespace VGC.GameFramework.Runtime
{
    public class EntryButton : ButtonExtension
    {
        [SerializeField] private TextMeshProUGUI _entryPlayerText;
        
        [SerializeField, AutoAssignIndexField(typeof(EntryPanelBase))] internal int _index;
        [SerializeField, AutoPopulateField(typeof(EntryPanelBase), ExecutorScope.NearestParent, ExecutorOrder.Hierarchy, required:true)] internal EntryPanelBase _entryPanel;

        protected int _playerId = -1;
        public int PlayerId => _playerId;
        
        public override void _OnClick()
        {
            base._OnClick();
            _entryPanel._OnEntryButtonClick(_index);
        }

        public virtual void _OnEntry(int playerId)
        {
            _playerId = playerId;

            if (_entryPlayerText)
            {
                var player = VRCPlayerApi.GetPlayerById(playerId);
                _entryPlayerText.text = Utilities.IsValid(player)
                    ? $"{player.displayName} : ({playerId})"
                    : $"({playerId})";
            }

            _SetColor(playerId == Networking.LocalPlayer.playerId ? Color.green : Color.red);
        }

        public virtual void _OnExit()
        {
            _playerId = -1;
            if(_entryPlayerText)
                _entryPlayerText.text = "No Entry";
            _ResetColor();
        }
    }
}
