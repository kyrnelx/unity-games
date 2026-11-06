using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.UniKit
{
    public class StartGuideEvent : EventBase
    {
        public readonly GuideController GuideCtrl;
        public StartGuideEvent(GuideController guideCtrl) => GuideCtrl = guideCtrl;
    }

    public class GuideManager : MonoBehaviour, IGuideStepStateProvider
    {
        [SerializeField]
        private TextAsset _guideConfigTextAsset;
        
        private GuideData _guideData;
        private List<string> _completedIds;
        private GuideConfig _guideConfig;
        
        private int _currentGuideIndex;
        private GuideController _guideController;

        private void Awake()
        {
            LoadGuideConfig();
            LoadCompletedGuides();
            RefreshGuide();
        }

        private void Start()
        {
            var currentGuide = _guideConfig.guides[_currentGuideIndex];
            _guideController ??= new GuideController(this);
            _guideController.RefreshGuide(currentGuide);
            GlobalSignalBus.Instance.Fire(new StartGuideEvent(_guideController));
        }

        public bool IsGuideCompleted(string id)
        {
            return _completedIds.Contains(id);
        }

        public bool IsStepCompleted(string id)
        {
            return _completedIds.Contains(id);
        }

        private void RefreshGuide()
        {
            for (var i = 0; i < _guideConfig.guides.Count; i++)
            {
                var guideItem = _guideConfig.guides[i];
                if (!_completedIds.Contains(guideItem.id))
                {
                    _currentGuideIndex = i;
                    break;
                }
            }
        }

        private void LoadGuideConfig()
        {
            _guideConfig = JsonUtility.FromJson<GuideConfig>(_guideConfigTextAsset.text);
        }

        private void LoadCompletedGuides()
        {
            var json = PlayerPrefs.GetString("GuideData");
            _guideData = JsonUtility.FromJson<GuideData>(json);
            _completedIds = _guideData?.completedIds ?? new List<string>();
        }

        private void SaveCompletedGuides()
        {
            PlayerPrefs.SetString("GuideData", JsonUtility.ToJson(_guideData));
            PlayerPrefs.Save();
        }
    }
}