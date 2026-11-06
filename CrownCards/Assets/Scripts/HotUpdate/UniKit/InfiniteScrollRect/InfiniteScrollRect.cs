using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.UniKit
{
    public class InfiniteScrollRect : ScrollRect
    {
        private IBehaviourHandler m_Handler;
        private RectTransform m_Dummy;
        private List<ScrollViewOptimizeItem> m_ManagedItem;

        public void Initialize(IBehaviourHandler _handler, RectTransform _dummy)
        {
            m_ManagedItem = new List<ScrollViewOptimizeItem>();
            m_Handler = _handler;
            m_Dummy = _dummy;
        }

        public void OnUpdate()
        {
            if (m_Handler.Count > m_ManagedItem.Count)
            {
                while (m_ManagedItem.Count < m_Handler.Count)
                {
                    var newItem = new ScrollViewOptimizeItem();
                    newItem.Dummy = Instantiate(m_Dummy, content);
                    m_ManagedItem.Add(newItem);
                }
            }
            else if (m_Handler.Count < m_ManagedItem.Count)
            {
                for (int i = m_ManagedItem.Count - 1; i >= m_Handler.Count; i--)
                {
                    m_Handler.Free(m_ManagedItem[i].Instance);
                    Destroy(m_ManagedItem[i].Dummy);
                    m_ManagedItem[i].Instance = null;
                    m_ManagedItem[i].Dummy = null;
                }
            }

            if (!CanvasUpdateRegistry.IsRebuildingLayout()) Canvas.ForceUpdateCanvases();
            for (int i = 0; i < m_ManagedItem.Count; i++)
            {
                var hasInstance = m_ManagedItem[i].Instance != null;
                var shouldDisplay = ShouldDisplay(i);
                if (hasInstance && !shouldDisplay)
                {
                    m_Handler.Free(m_ManagedItem[i].Instance);
                    m_ManagedItem[i].Instance = null;
                }
                else if (!hasInstance && shouldDisplay)
                {
                    var instance = m_Handler.Alloc(i);
                    m_Handler.OnUpdate(i, instance);
                    instance.SetParent(m_ManagedItem[i].Dummy, false);
                    instance.anchoredPosition = Vector2.zero;
                    m_ManagedItem[i].Instance = instance;
                }
            }
        }

        private bool ShouldDisplay(int index)
        {
            var rViewRect = new Rect(viewRect.rect.position + viewRect.anchoredPosition, viewRect.rect.size);
            return new Rect(rViewRect.position - content.anchoredPosition, rViewRect.size).Overlaps(m_ManagedItem[index]
                .Rect);
        }

        public interface IBehaviourHandler
        {
            public void OnUpdate(int idx, RectTransform rect);

            public RectTransform Alloc(int index);

            public void Free(RectTransform rect);

            public int Count { get; }
        }

        public class ScrollViewOptimizeItem
        {
            public RectTransform Instance;
            public RectTransform Dummy;

            public Rect Rect => new Rect(Dummy.rect.position + Dummy.anchoredPosition, Dummy.rect.size);
        }
    }
}