using System;

namespace Kyrnel.UniKit
{
    [Serializable]
    public class GuideController
    {
        private Guide _guide;
        private IGuideStepStateProvider _stepStateProvider;
        private int _stepIndex;

        public GuideController(IGuideStepStateProvider stepStateProvider)
        {
            _stepStateProvider = stepStateProvider;
        }

        public void RefreshGuide(Guide guide)
        {
            _guide = guide;
            _stepIndex = 0;
        }

        public GuideStep GetCurrentStep()
        {
            return _guide.steps[_stepIndex];
        }

        public void RefreshStep()
        {
            for (int i = 0; i < _guide.steps.Count; i++)
            {
                if (!_stepStateProvider.IsStepCompleted(_guide.steps[i].id))
                {
                    _stepIndex = i;
                    break;
                }
            }
        }
    }
}