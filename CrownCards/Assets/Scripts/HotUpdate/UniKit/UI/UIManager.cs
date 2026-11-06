using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.UniKit
{
    public class UIArgsBase { }
    
    public interface IViewCloser
    {
        void CloseView(ViewBase instance);
    }

    public class UIManager : SingletonMono<UIManager>, IViewCloser
    {
        [SerializeField] private Transform _backgroundLayer;
        [SerializeField] private Transform _normalLayer;
        [SerializeField] private Transform _popupLayer;
        [SerializeField] private Transform _guideLayer;
        [SerializeField] private Transform _topmostLayer;

        private readonly List<ViewBase> _activeViews = new();
        private readonly List<ViewBase> _popupStack = new();

        public void ShowView(ViewBase prefab, ViewLayer layer, object args = null)
        {
            if (prefab == null) return;
            ViewBase instance = Instantiate(prefab, GetLayer(layer));
            instance.Initialize(this, false, args);
            instance.Show();
            _activeViews.Add(instance);
        }
        
        public void HideView(ViewBase instance)
        {
            if (instance == null) return;
            if (_activeViews.Contains(instance))
            {
                _activeViews.Remove(instance);
                Destroy(instance.gameObject);
            }
        }

        public void PushPopup(ViewBase prefab, ViewLayer layer, object args = null)
        {
            if (prefab == null) return;
            ViewBase instance = Instantiate(prefab, GetLayer(layer));
            instance.Initialize(this, true, args);
            instance.Show();
            _popupStack.Add(instance);
        }

        public void PopPopup()
        {
            if (_popupStack.Count == 0) return;
            int index = _popupStack.Count - 1;
            ViewBase instance = _popupStack[index];
            _popupStack.RemoveAt(index);
            Destroy(instance.gameObject);
        }
        
        public void CloseView(ViewBase instance)
        {
            if(instance.IsPopup)
                PopPopup();
            else
                HideView(instance);
        }

        private Transform GetLayer(ViewLayer layer)
        {
            return layer switch
            {
                ViewLayer.Background => _backgroundLayer,
                ViewLayer.Normal => _normalLayer,
                ViewLayer.Popup => _popupLayer,
                ViewLayer.Guide => _guideLayer,
                ViewLayer.Topmost => _topmostLayer,
                _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
            };
        }
    }

    public enum ViewLayer
    {
        Background = 0,
        Normal = 20,
        Popup = 30,
        Guide = 60,
        Topmost = 100
    }
}