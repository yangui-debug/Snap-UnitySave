using UnityEngine;

namespace ArtOfWar_II_Art.ZoomEffect
{
    public class MagnifyingEffect : MonoBehaviour
    {
        // shader
        // 放大强度
        [Range(-2.0f, 2.0f), Tooltip("放大强度")] [SerializeField]
        private float zoomFactor = 0.4f;

        // 放大镜大小
        [Range(0.0f, 0.2f), Tooltip("放大镜大小")] [SerializeField]
        private float size = 0.15f;

        // 凸镜边缘强度
        [Range(0.0001f, 0.1f), Tooltip("凸镜边缘强度")] [SerializeField]
        private float edgeFactor = 0.05f;

        [SerializeField] private Material material = default;

        [SerializeField, Space] private RectTransform followTarget = default;

        private static readonly int PosX = Shader.PropertyToID("_PosX");
        private static readonly int PosY = Shader.PropertyToID("_PosY");
        private static readonly int ZoomFactor = Shader.PropertyToID("_ZoomFactor");
        private static readonly int EdgeFactor = Shader.PropertyToID("_EdgeFactor");
        private static readonly int Size = Shader.PropertyToID("_Size");

        private Vector2 canvasSize;
        private Vector2 halfSize;

        private void Start()
        {
            canvasSize = transform.root.GetComponent<RectTransform>().sizeDelta;
            halfSize = canvasSize / 2;
        }

        void Update()
        {
            if (!material)
            {
                return;
            }

            material.SetFloat(ZoomFactor, zoomFactor);
            material.SetFloat(EdgeFactor, edgeFactor);
            material.SetFloat(Size, size);

            if (followTarget)
            {
                Vector2 screenPos = followTarget.anchoredPosition;
                material.SetFloat(PosX, (screenPos.x + halfSize.x) / canvasSize.x);
                material.SetFloat(PosY, (screenPos.y + halfSize.y) / canvasSize.y);
            }
        }
    }
}