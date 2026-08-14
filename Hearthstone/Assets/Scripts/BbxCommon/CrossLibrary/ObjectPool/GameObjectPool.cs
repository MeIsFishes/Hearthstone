using System;
using System.Collections.Generic;
using UnityEngine;

namespace BbxCommon
{
    /// <summary>
    /// Instance-scoped pool for reusable prefab components.
    /// The owner controls the pool lifetime and supplies its hierarchy root.
    /// </summary>
    public sealed class GameObjectPool<TComponent> : IDisposable where TComponent : Component
    {
        private readonly TComponent m_Prefab;
        private readonly Transform m_Root;
        private readonly Stack<TComponent> m_Available;
        private readonly HashSet<TComponent> m_AvailableSet = new();
        private readonly HashSet<TComponent> m_Instances = new();
        private readonly bool m_OwnsRoot;
        private bool m_Disposed;

        public int Count => m_Instances.Count;
        public int AvailableCount => m_Available.Count;

        public GameObjectPool(TComponent prefab, Transform root, int initialCapacity = 0)
            : this(prefab, root, initialCapacity, false)
        {
        }

        public GameObjectPool(TComponent prefab, string rootName, int initialCapacity = 0)
            : this(prefab, new GameObject(rootName).transform, initialCapacity, true)
        {
        }

        private GameObjectPool(TComponent prefab, Transform root, int initialCapacity, bool ownsRoot)
        {
            m_Prefab = prefab;
            m_Root = root;
            m_OwnsRoot = ownsRoot;
            m_Available = new Stack<TComponent>(Mathf.Max(0, initialCapacity));

            for (int i = 0; i < initialCapacity; i++)
                Collect(CreateInstance());
        }

        public TComponent Alloc()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(GetType().Name);

            TComponent instance;
            if (m_Available.Count > 0)
            {
                instance = m_Available.Pop();
                m_AvailableSet.Remove(instance);
            }
            else
            {
                instance = CreateInstance();
            }

            instance.transform.SetParent(null, true);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Collect(TComponent instance)
        {
            if (instance == null)
                return;

            if (m_Disposed || !m_Instances.Contains(instance))
            {
                UnityEngine.Object.Destroy(instance.gameObject);
                return;
            }

            if (!m_AvailableSet.Add(instance))
            {
                DebugApi.LogError($"The GameObject {instance.name} has already been collected.");
                return;
            }

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(m_Root, false);
            m_Available.Push(instance);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            foreach (var instance in m_Instances)
            {
                if (instance != null)
                    UnityEngine.Object.Destroy(instance.gameObject);
            }
            m_Available.Clear();
            m_AvailableSet.Clear();
            m_Instances.Clear();
            if (m_OwnsRoot && m_Root != null)
                UnityEngine.Object.Destroy(m_Root.gameObject);
        }

        private TComponent CreateInstance()
        {
            if (m_Prefab == null)
                throw new InvalidOperationException($"{GetType().Name} requires a prefab component.");

            var instance = UnityEngine.Object.Instantiate(m_Prefab, m_Root);
            m_Instances.Add(instance);
            return instance;
        }
    }
}
