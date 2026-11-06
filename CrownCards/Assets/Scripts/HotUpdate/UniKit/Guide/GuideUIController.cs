using UnityEngine;

namespace Kyrnel.UniKit
{
    public class CompleteStepEvent : EventBase { }

    public class GuideUIController : MonoBehaviour
    {
        [SerializeField] private GuideDialogView _dialogView;
        
        private void Awake()
        {
            GlobalSignalBus.Instance.Subscribe<StartGuideEvent>(StartGuide);
        }

        private void StartGuide(StartGuideEvent args)
        {
            args.GuideCtrl.RefreshStep();
            var currentStep = args.GuideCtrl.GetCurrentStep();
            if (currentStep.type == GuideStepType.Dialog)
            {
                UIManager.Instance.ShowView(_dialogView, ViewLayer.Guide, new GuideDialogView.Args(currentStep));
            }
        }

        private void CompleteStep()
        {
        }
    }
}