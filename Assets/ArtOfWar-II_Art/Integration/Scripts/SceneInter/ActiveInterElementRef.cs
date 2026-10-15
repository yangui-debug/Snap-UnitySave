using System.Collections.Generic;
using UnityEngine;

namespace ArtView
{
    public class ActiveInterElementRef : MonoBehaviour
    {
        [SerializeField] private List<GameObject> activeObjects;

        public void SetActive(bool value)
        {
            foreach (GameObject obj in activeObjects)
            {
                if (obj)
                {
                    obj.SetActive(value);
                }
            }
        }
    }
}