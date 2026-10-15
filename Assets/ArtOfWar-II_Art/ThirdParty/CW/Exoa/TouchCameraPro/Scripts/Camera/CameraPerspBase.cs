using System;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraPerspBase : CameraBase
    {
        [Header("DISTANCE")] public Vector2 minMaxDistance = new Vector2(3, 30);
        protected float initDistance = 10f;
        protected float fov = 55.0f;

        public event Action<float> distanceScaleAction;

        public float Fov
        {
            get { return fov; }
        }

        public override float FinalDistance
        {
            get => finalDistance;
            set
            {
                finalDistance = value;
                float distanceScale = CalcDistanceScale();
                distanceScaleAction?.Invoke(distanceScale);
            }
        }

        private float CalcDistanceScale()
        {
            float distanceScale;
            float offset = finalDistance - initDistance;
            if (offset > 0)
            {
                distanceScale = -offset / (minMaxDistance.y - initDistance);
            }
            else
            {
                distanceScale = -offset / (initDistance - minMaxDistance.x);
            }

            return distanceScale;
        }

        override protected void Init()
        {
            fov = cam.fieldOfView;
            FinalDistance = initDistance;
            finalOffset = transform.position.SetY(groundHeight);

            //print("Init finalOffset:" + finalOffset);

            base.Init();
        }

        override public Matrix4x4 GetMatrix()
        {
            float aspect = cam.aspect;
            float near = cam.nearClipPlane, far = cam.farClipPlane;
            return Matrix4x4.Perspective(fov, aspect, near, far);
        }

        public void SetPositionByDistance(float v)
        {
            FinalDistance = Mathf.Clamp(v, minMaxDistance.x, minMaxDistance.y);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
        }

        #region FOLLOW

        [Header("FOLLOW")] public float followDistanceMultiplier = 1f;

        #endregion

        public void MoveCameraTo(Vector3 targetPos, Vector3 targetRotation)
        {
            finalPosition = targetPos;
            finalRotation = Quaternion.Euler(targetRotation);
        }

        virtual public void SetResetValues(Vector3 offset, Quaternion rotation, float distance)
        {
            initOffset = offset;
            initDistance = distance;
            initRotation = rotation;
            initDataSaved = true;
        }
    }
}