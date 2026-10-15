using Exoa.Designer;
using UnityEngine;

namespace Exoa.Cameras
{
    public class TerrainModule : MonoBehaviour
    {
        public enum Mode { CameraCenter, FingersCenter };
        public Mode mode;

        private CameraBase camBase;
        private Camera cam;
        private RaycastHit hitInfo;
        private bool isHitting;
        public float maxDistance = 100f;
        public LayerMask layerMask;

        void Start()
        {
            cam = GetComponent<Camera>();
            camBase = GetComponent<CameraBase>();
        }


        void Update()
        {


            if (mode == Mode.CameraCenter)
            {
                Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));

                FindGround(screenCenter);
            }
            else if (mode == Mode.FingersCenter)
            {
                FindGround(Inputs.screenPointAnyFingerCountCenter);
            }
        }

        private void FindGround(Vector2 screenPoint)
        {
            Ray r = cam.ScreenPointToRay(screenPoint);
            isHitting = Physics.Raycast(r, out hitInfo, maxDistance, layerMask.value);
            if (isHitting)
            {
                camBase.SetGroundHeight(hitInfo.point.y);
            }
        }

    }
}