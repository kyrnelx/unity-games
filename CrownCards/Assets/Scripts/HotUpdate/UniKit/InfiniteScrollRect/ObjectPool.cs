using System.Collections.Generic;
using UnityEngine;

namespace Kyrnel.UniKit
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        public T InstancePrefab;

        public ObjectPool(T instancePrefab) => InstancePrefab = instancePrefab;

        private Stack<T> m_Pool = new Stack<T>();

        public T Alloc()
        {
            T instance;
            if (m_Pool.Count > 0)
            {
                instance = m_Pool.Pop();
                instance.gameObject.SetActive(true);
            }
            else
                instance = GameObject.Instantiate(InstancePrefab);

            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            m_Pool.Push(instance);
        }
    }
}
