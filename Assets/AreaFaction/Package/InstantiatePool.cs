using System.Collections.Generic;
using UnityEngine;

namespace Com.i89.InstanPool
{
    public class InstantiatePool<T> : SimpleInstantiatePool<T> where T : MonoBehaviour, IInstantiatedItem
    {
        public InstantiatePool(T prefab) : base(prefab)
        {
        }

        public new List<T> GetAllActivationItem()
        {
            return loadedCells.FindAll(x => x.Active);
        }

        public override void Hide(T cell)
        {
            base.Hide(cell);
            cell.Active = false;
        }

        public override void HideExcessCells(int capacity)
        {
            for (int i = capacity; i < loadedCells.Count; i++)
            {
                T cell = loadedCells[i];
                cell.Active = false;
            }

            base.HideExcessCells(capacity);
        }

        protected override T GetCellByIndex(int index, Transform container)
        {
            T cell = base.GetCellByIndex(index, container);
            cell.Active = true;
            return cell;
        }

        public int Count => GetAllActivationItem().Count;
    }
}