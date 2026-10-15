using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Com.i89.InstanPool
{
    public class SimpleInstantiatePool<T> where T : Component
    {
        protected readonly List<T> loadedCells;
        private readonly T prefab;

        private int capacity;

        public Action<T> CustomPop;
        public Action<T> CustomRecycle;

        public SimpleInstantiatePool(T prefab)
        {
            this.prefab = prefab;
            loadedCells = new List<T>();
        }

        public List<T> GetAllActivationItem()
        {
            return loadedCells.GetRange(0, capacity);
        }

        public List<T> InstantiateAll(int capacity, Transform container)
        {
            List<T> list = new List<T>();
            for (var i = 0; i < capacity; i++)
            {
                T cell = Instantiate(i, container);
                list.Add(cell);
            }

            HideExcessCells(capacity);
            return list;
        }

        public T InstantiateOne(Transform container)
        {
            int index = capacity;
            capacity++;
            return Instantiate(index, container);
        }

        public T Instantiate(int index, Transform container)
        {
            capacity = Mathf.Max(capacity, index + 1);
            return GetCellByIndex(index, container);
        }

        public virtual void Hide(T cell)
        {
            if (CustomRecycle != null)
            {
                CustomRecycle.Invoke(cell);
            }
            else
            {
                cell.gameObject.SetActive(false);
            }

            // 挪到末尾
            loadedCells.Remove(cell);
            loadedCells.Add(cell);

            capacity--;
        }

        public void HideAll()
        {
            HideExcessCells(0);
        }

        public virtual void HideExcessCells(int capacity)
        {
            this.capacity = capacity;

            for (int i = capacity; i < loadedCells.Count; i++)
            {
                T cell = loadedCells[i];

                if (CustomRecycle != null)
                {
                    CustomRecycle.Invoke(cell);
                }
                else
                {
                    cell.gameObject.SetActive(false);
                }
            }
        }

        public void SetCapacity(int capacity)
        {
            this.capacity = capacity;
        }

        public void SyncCapacity()
        {
            HideExcessCells(capacity);
        }

        public void Destroy(T cell)
        {
            if (loadedCells.Remove(cell))
            {
                Object.Destroy(cell.gameObject);
                capacity--;
            }
        }

        public void Destroy()
        {
            for (int i = loadedCells.Count - 1; i >= 0; i--)
            {
                T cell = loadedCells[i];
                if (cell)
                {
                    Object.Destroy(cell.gameObject);
                }
            }

            capacity = 0;
            loadedCells.Clear();
        }

        protected virtual T GetCellByIndex(int index, Transform container)
        {
            if (index >= loadedCells.Count)
            {
                for (int i = loadedCells.Count; i <= index; i++)
                {
                    loadedCells.Add(Object.Instantiate(prefab, container));
                }
            }
            else
            {
                T item = loadedCells[index];
                if (!item)
                {
                    loadedCells.RemoveAt(index);
                    return GetCellByIndex(index, container);
                }

                item.transform.SetParent(container, false);
            }

            if (CustomPop != null)
            {
                CustomPop?.Invoke(loadedCells[index]);
            }
            else
            {
                loadedCells[index].gameObject.SetActive(true);
            }

            return loadedCells[index];
        }
    }
}