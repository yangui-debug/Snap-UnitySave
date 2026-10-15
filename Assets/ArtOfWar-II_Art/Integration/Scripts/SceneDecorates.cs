using UnityEngine;

namespace Battle.NewVisuals.Buildings.Scripts
{
    public class SceneDecorates : MonoBehaviour
    {
        [SerializeField] private GameObject[] obstacles = default;

        /// <summary>
        /// sizeLv from 0
        /// </summary>
        /// <param name="sizeLv"></param>
        /// <param name="size"></param>
        public void Init(int sizeLv, Vector2Int size)
        {
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacles[i].gameObject.SetActive(i >= sizeLv);
            }
        }
    }
}