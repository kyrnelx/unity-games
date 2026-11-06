using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.UniKit
{
    public class GuideDialogView : ViewBase
    {
        public class Args : UIArgsBase
        {
            public GuideStep Step;
            public Args(GuideStep step) => Step = step;
        }

        [SerializeField] private Text messageText;
        
        private Args _args;

        public override void OnCreate()
        {
            base.OnCreate();
            _args = GetArgs<Args>();
            RefreshUI(_args.Step);
        }

        public void RefreshUI(GuideStep step)
        {
            messageText.text = step.message;
        }

        public void OnClick()
        {
            GlobalSignalBus.Instance.Fire<CompleteStepEvent>();
        }
    }
}