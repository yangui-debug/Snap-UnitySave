using System;
using UnityEngine;

namespace ArtView
{
    public class FaceCameraItem : MonoBehaviour
    {
        public static Action<float> onCameraRotate;

        public void OnEnable()
        {
            onCameraRotate += OnRotateCamera;
        }

        public void OnDisable()
        {
            onCameraRotate -= OnRotateCamera;
        }

        public void OnRotateCamera(float ry)
        {
            Vector3 eulerAngle = transform.eulerAngles;
            transform.eulerAngles = new Vector3(eulerAngle.x, ry, eulerAngle.z);
        }
    }
}