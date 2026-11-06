using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.UniKit
{
    public class InfiniteScrollRectSampleView : MonoBehaviour, InfiniteScrollRect.IBehaviourHandler
    {
        [SerializeField] private InfiniteScrollRect m_InfiniteScrollRect;
        [SerializeField] private InfiniteScrollRectItemView m_InstancePrefab;
        [SerializeField] private RectTransform m_DummyPrefab;

        private ObjectPool<InfiniteScrollRectItemView> m_ObjectPool;
        private ScrollViewOptimizeData m_Data;

        private void Awake()
        {
            m_Data = new ScrollViewOptimizeData();
            m_ObjectPool = new ObjectPool<InfiniteScrollRectItemView>(m_InstancePrefab);
            m_InfiniteScrollRect.Initialize(this, m_DummyPrefab);
            m_InfiniteScrollRect.OnUpdate();
        }

        public void OnUpdate(int idx, RectTransform rect)
        {
            rect.GetComponent<InfiniteScrollRectItemView>().Refresh(m_Data.DataList[idx]);
        }

        public RectTransform Alloc(int index)
        {
            return m_ObjectPool.Alloc().GetComponent<RectTransform>();
        }

        public void Free(RectTransform rect)
        {
            m_ObjectPool.Release(rect.GetComponent<InfiniteScrollRectItemView>());
        }

        public int Count => m_Data.DataList.Count;
    }

    public class ScrollViewOptimizeData
    {
        public List<string> DataList = new List<string>()
        {
            "Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Item8", "Item9", "Item10",
            "Item11", "Item12", "Item13", "Item14", "Item15", "Item16", "Item17", "Item18", "Item19", "Item20",
            "Item21", "Item22", "Item23", "Item24", "Item25", "Item26",
        };
    }
}