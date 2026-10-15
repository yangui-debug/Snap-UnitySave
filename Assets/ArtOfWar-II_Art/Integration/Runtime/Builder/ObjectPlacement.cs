using UnityEngine;

namespace ArtOfWar_II_Art.Integration.Scripts.Builder
{
    public class ObjectPlacement : MonoBehaviour
    {
        public const string layerName = "SceneObject";

        public int type;
        public int areaId;
        public Vector3Int size = Vector3Int.one;
        public Vector3Int position;

        [SerializeField] private GameObject stateContainer = default;

        public bool active { get; private set; }

        private void Reset()
        {
            Bounds bound = new Bounds(transform.position, Vector3.one);
            foreach (Renderer child in GetComponentsInChildren<Renderer>())
            {
                if (child.name.Contains("AO"))
                {
                    continue;
                }

                bound.Encapsulate(child.bounds);
            }

            if (!gameObject.TryGetComponent(out BoxCollider box))
            {
                box = gameObject.AddComponent<BoxCollider>();
            }

            box.size = bound.size;
            box.center = bound.center;
        }

        public void SetActive(bool active)
        {
            this.active = active;
            if (stateContainer)
            {
                stateContainer.SetActive(active);
            }
        }
    }
}