using System;
using System.Collections.Generic;

namespace Kyrnel.UniKit
{
    [Serializable]
    public class GuideConfig
    {
        public List<Guide> guides = new();
    }

    [Serializable]
    public class Guide
    {
        public string id;
        public List<GuideStep> steps = new();
    }

    [Serializable]
    public class GuideStep
    {
        public string id;
        public GuideStepType type;
        public string message;
        public int requiredClicks;
    }

    public enum GuideStepType
    {
        None,
        Dialog,
        Finger
    }
}