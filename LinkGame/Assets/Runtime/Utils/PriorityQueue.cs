using System;
using System.Collections.Generic;

namespace Kyrnel
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        public int Count => _heapItems.Count;

        private readonly List<T> _heapItems = new();

        public void Enqueue(T item)
        {
            _heapItems.Add(item);
            var child = _heapItems.Count - 1;
            while (child > 0)
            {
                var parent = (child - 1) / 2;
                if (_heapItems[child].CompareTo(_heapItems[parent]) >= 0) break;
                this.Swap(child, parent);
                child = parent;
            }
        }

        public T Dequeue()
        {
            var lastIndex = _heapItems.Count - 1;
            var front = _heapItems[0];
            _heapItems[0] = _heapItems[lastIndex];
            _heapItems.RemoveAt(lastIndex);
            var nParent = 0;
            while (true)
            {
                var left = nParent * 2 + 1;
                if (left >= _heapItems.Count) break;
                var right = left + 1;
                var min = (right < _heapItems.Count && _heapItems[right].CompareTo(_heapItems[left]) < 0)
                    ? right
                    : left;
                if (_heapItems[nParent].CompareTo(_heapItems[min]) <= 0) break;
                this.Swap(nParent, min);
                nParent = min;
            }

            return front;
        }

        public void Clear() => _heapItems.Clear();

        private void Swap(int a, int b) => (_heapItems[a], _heapItems[b]) = (_heapItems[b], _heapItems[a]);
    }
}