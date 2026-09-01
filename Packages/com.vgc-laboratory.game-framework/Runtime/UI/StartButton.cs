using UnityEngine;
using VGC.Attributes.Runtime;
using VGC.UIExtension.Runtime;

namespace VGC.GameFramework.Runtime
{
    public class StartButton : ButtonExtension
    {
        [SerializeField, AutoPopulateField(typeof(EntryPanelBase), ExecutorScope.NearestParent, ExecutorOrder.Hierarchy, required:true)] internal EntryPanelBase _entryPanel;
        
        public override void _OnClick()
        {
            base._OnClick();
            _entryPanel._OnStartButtonClick();
        }
    }
}