using System.Collections.Generic;
using UnityEngine;

namespace ArtView
{
    [ExecuteInEditMode]
    public class StageInterElementRef : MonoBehaviour
    {
        [SerializeField] private List<GameObject> stages;

        public List<GameObject> Stages => stages;

#if UNITY_EDITOR
        private void Start()
        {
            Reset();
        }
#endif

        private void Reset()
        {
            stages ??= new List<GameObject>();
            stages.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                stages.Add(child.gameObject);
            }
        }
    }
}