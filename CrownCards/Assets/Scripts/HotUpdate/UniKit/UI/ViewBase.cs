using UnityEngine;

namespace Kyrnel.UniKit
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(RectTransform))]
    public class ViewBase : MonoBehaviour
    {
        public bool IsPopup { get; private set; }
        protected CanvasGroup CanvasGroup { get; private set; }
        protected RectTransform RectTransform { get; private set; }
        private object _args;
        private IViewCloser _closer;

        public virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            RectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(IViewCloser closer, bool isPopup, object args)
        {
            _closer = closer;
            IsPopup = isPopup;
            _args = args;
            OnCreate();
        }

        public virtual void OnCreate()
        {
        }

        public void Show()
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public void Hide()
        {
            CanvasGroup.alpha = 0;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            OnHide();
        }

        public void Close()
        {
            OnClose();
            _closer.CloseView(this);
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        public virtual void OnClose()
        {
        }

        protected T GetArgs<T>()
        {
            return (T)_args;
        }
    }
}