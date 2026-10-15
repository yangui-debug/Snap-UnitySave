using UnityEngine;

namespace Functions.Components.UIComponents.Layout
{
    public class PlaneBgDisplay : MonoBehaviour
    {
        [SerializeField] private Transform plane = default;
        [SerializeField] private Transform planeBg = default;
        [SerializeField] private UnityEngine.Camera mainCamera = default;
        [SerializeField] private Vector2 designedImageSize = new Vector2(1440, 2560);
        [SerializeField] private bool hidePanel = default;
        [SerializeField] private bool hidePanelBg = default;

        private void Start()
        {
            Adjust();
        }

        private void Adjust()
        {
            RectTransform root = transform.root.GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            Vector2 canvasSize = root.rect.size;

            float scaleFactor = 1f / root.localScale.x / 10; // 10: default size of the 3d panel 
            float farClipPlane = mainCamera.farClipPlane;

            Vector3 v2p1 = new Vector3(0, 0, farClipPlane);
            Vector3 v2p2 = new Vector3(1, 1, farClipPlane);

            // 左下角世界坐标
            Vector3 lbp = mainCamera.ViewportToWorldPoint(v2p1);

            // 左上角世界坐标
            Vector3 ltp = mainCamera.ViewportToWorldPoint(new Vector3(v2p1.x, v2p2.y, farClipPlane));

            // 右上角世界坐标
            Vector3 rtp = mainCamera.ViewportToWorldPoint(v2p2);

            ScreenMatchType type = CalcScreenMatchType();
            if (type == ScreenMatchType.Width)
            {
                float x = Vector3.Distance(ltp, rtp);
                float xv = x * (designedImageSize.x / canvasSize.x);
                float yv = designedImageSize.y / designedImageSize.x * xv;
                plane.localScale = new Vector3(xv, 1, yv) * scaleFactor;
            }
            else if (type == ScreenMatchType.Height)
            {
                float y = Vector3.Distance(ltp, lbp);
                float yv = y * (designedImageSize.y / canvasSize.y);
                float xv = designedImageSize.x / designedImageSize.y * yv;
                plane.localScale = new Vector3(xv, 1, yv) * scaleFactor;
            }

            Vector3 p = (lbp + rtp) / 2;
            Transform cameraTrans = mainCamera.transform;
            Vector3 offset = (cameraTrans.position - p).normalized;

            plane.position = p + offset;
            plane.localEulerAngles = new Vector3(90, 180, 0);
            planeBg.localPosition = plane.localPosition + Vector3.forward;
            planeBg.localEulerAngles = new Vector3(0, 0, 180);
            transform.forward = cameraTrans.forward;

            plane.gameObject.SetActive(!hidePanel);
            planeBg.gameObject.SetActive(!hidePanelBg);
        }

        private static ScreenMatchType CalcScreenMatchType() => Screen.width / (float) Screen.height > 9.0f / 16.0f
            ? ScreenMatchType.Height
            : ScreenMatchType.Width;

        private enum ScreenMatchType
        {
            Width,
            Height,
        }
    }
}