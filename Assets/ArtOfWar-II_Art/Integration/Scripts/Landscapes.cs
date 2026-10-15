using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts
{
    public class Landscapes : MonoBehaviour
    {
        [SerializeField] private GameObject[] landscapes = default;

        /// <summary>
        /// sizeLv from 1
        /// </summary>
        /// <param name="sizeLv"></param>
        /// <param name="size"></param>
        public void Init(int sizeLv, Vector2Int size)
        {
            for (int i = 0; i < landscapes.Length; i++)
            {
                landscapes[i].gameObject.SetActive(i >= sizeLv - 1);
            }
        }
    }
}