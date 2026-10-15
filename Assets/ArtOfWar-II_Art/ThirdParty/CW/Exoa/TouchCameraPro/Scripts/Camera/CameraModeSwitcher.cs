
using DG.Tweening;
using Exoa.Designer;
using Exoa.Events;
using System;
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraModeSwitcher : MonoBehaviour
    {
        public static CameraModeSwitcher Instance;
        public GameObject target;

        private CameraOrthoBase camOrtho;
        private CameraPerspBase camPersp;
        private Camera cam;

        private float matrixLerp; // 0 for ortho, 1 for perspective
        private bool orthoMode = true;
        private Matrix4x4 orthoMatrix, perspectiveMatrix;

        void OnDestroy()
        {
            CameraEvents.OnRequestButtonAction -= OnRequestButtonAction;
            CameraEvents.OnRequestObjectFocus -= FocusCameraOnGameObject;
            CameraEvents.OnRequestObjectFollow -= FollowGameObject;
            CameraEvents.OnRequestGroundHeightChange -= SetGroundHeightAnimated;
        }

        void Awake()
        {
            Instance = this;
            camOrtho = GetComponent<CameraOrthoBase>();
            camPersp = GetComponent<CameraPerspBase>();
            cam = GetComponent<Camera>();

            if (camOrtho.defaultMode && camPersp.defaultMode)
            {
                Debug.LogError("Error: only one camera mode can be marked as defaultMode!");
            }
            if (!camOrtho.defaultMode && !camPersp.defaultMode)
            {
                Debug.LogError("Error: no camera mode is marked as defaultMode!");
                camOrtho.defaultMode = true;
            }

            orthoMode = camOrtho.defaultMode;
            matrixLerp = orthoMode ? 0 : 1;

            CameraEvents.OnBeforeSwitchPerspective?.Invoke(orthoMode);
            CameraEvents.OnAfterSwitchPerspective?.Invoke(orthoMode);
            CameraEvents.OnRequestButtonAction += OnRequestButtonAction;
            CameraEvents.OnRequestObjectFocus += FocusCameraOnGameObject;
            CameraEvents.OnRequestObjectFollow += FollowGameObject;
            CameraEvents.OnRequestGroundHeightChange += SetGroundHeightAnimated;
        }


        void LateUpdate()
        {
            orthoMatrix = camOrtho.GetMatrix();
            perspectiveMatrix = camPersp.GetMatrix();

            if (Inputs.ChangePlanMode())
            {
                TogglePerspective();
            }

            Matrix4x4 mergedMatrix = MatrixLerp(orthoMatrix, perspectiveMatrix, matrixLerp);

            cam.projectionMatrix = mergedMatrix;


            try
            {
                Quaternion rotation = Quaternion.Lerp(camOrtho.FinalRotation, camPersp.FinalRotation, matrixLerp);
                //float rotationSmoothness = orthoMode ? camOrtho.moveSmoothness : camPersp.moveSmoothness;
                //transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 1 - rotationSmoothness);

                transform.rotation = rotation;

                // convert by two finger center pos to world pos
                // LeanScreenDepth leanScreenDepth = camPersp.GetLeanScreenDepth();
                // List<LeanFinger> twoFingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
                // Vector2 screenPoint = default(Vector2);
                // if(LeanGesture.TryGetScreenCenter(twoFingers, ref screenPoint))
                // {
                //     Vector3 worldPointTwoFingersCenter = camPersp.ClampPointsXZ(leanScreenDepth.Convert(screenPoint));
                //     Debug.LogError($"worldPointTwoFingersCenter{worldPointTwoFingersCenter}");
                //     Debug.LogError(transform.position);
                //     transform.RotateAround(new Vector3(worldPointTwoFingersCenter.x, 0, worldPointTwoFingersCenter.z), Vector3.up, 20 * Time.deltaTime);
                //     
                // }

            }
            catch (System.Exception) { };

            Vector3 position = Vector3.Lerp(camOrtho.FinalPosition, camPersp.FinalPosition, matrixLerp);
            if (!float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z))
            {
                //float positionSmoothness = orthoMode ? camOrtho.moveSmoothness : camPersp.moveSmoothness;
                //transform.position = Vector3.Lerp(transform.position, position, (1 - positionSmoothness) * 60 * Time.deltaTime);
                transform.position = position;
            }
        }


        public void DisableCameraMoves(bool active)
        {
            camOrtho.DisableMoves = active;
            camPersp.DisableMoves = active;
        }
        public void ResetCamera()
        {
            StopFollow();
            if (orthoMode) camOrtho.ResetCamera();
            else camPersp.ResetCamera();
        }

        public void SetResetValues(Vector3 offset, Quaternion rotation, float distance, float size)
        {
            camOrtho.SetResetValues(offset, rotation, size);
            camPersp.SetResetValues(offset, rotation, distance);
        }


        public void TogglePerspective()
        {
            orthoMode = !orthoMode;
            OnBeforeSwitch(orthoMode);
            DOTween.To(() => matrixLerp, x => matrixLerp = x, (orthoMode ? 0 : 1), 1).SetEase(Ease.InOutCubic).OnComplete(() => OnAfterSwitch(orthoMode));
        }
        #region EVENTS
        private void OnBeforeSwitch(bool orthoOn)
        {
            if (orthoOn)
            {
                camOrtho.FinalOffset = camPersp.FinalOffset;
                camOrtho.SetSizeByDistance(camPersp.GetDistance());
                camOrtho.SetPositionByOffset();
                camPersp.enabled = false;
            }
            else
            {
                camPersp.FinalOffset = camOrtho.FinalOffset;
                camPersp.SetPositionByDistance(camOrtho.GetDistanceFromSize());
                camOrtho.enabled = false;
            }
            CameraEvents.OnBeforeSwitchPerspective?.Invoke(orthoMode);
        }

        private void OnAfterSwitch(bool orthoOn)
        {
            if (!orthoOn)
            {
                camPersp.enabled = true;
                cam.orthographic = false;
            }
            else
            {
                camOrtho.enabled = true;
                cam.orthographic = true;
            }
            CameraEvents.OnAfterSwitchPerspective?.Invoke(orthoMode);
        }

        private void OnRequestButtonAction(CameraEvents.Action action, bool active)
        {
            if (action == CameraEvents.Action.ForcePerspectiveMode && orthoMode)
                TogglePerspective();
            else if (action == CameraEvents.Action.SwitchPerspective)
                TogglePerspective();
            else if (action == CameraEvents.Action.ResetCameraPositionRotation)
                ResetCamera();
            else if (action == CameraEvents.Action.DisableCameraMoves)
                DisableCameraMoves(active);
        }
        #endregion

        #region UTILS
        public static Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float time)
        {
            Matrix4x4 ret = new Matrix4x4();
            for (int i = 0; i < 16; i++)
                ret[i] = Mathf.Lerp(from[i], to[i], time);
            return ret;
        }
        #endregion

        #region FOLLOW

        private void StopFollow()
        {
            camOrtho.StopFollow();
            camPersp.StopFollow();
        }
        public void FollowGameObject(GameObject go, bool focusOnFollow)
        {
            camOrtho.FollowGameObject(go, focusOnFollow);
            camPersp.FollowGameObject(go, focusOnFollow);

        }
        #endregion

        #region FOCUS
        public void FocusCameraOnGameObject(GameObject go, bool allowYOffsetFromGround = false)
        {
            StopFollow();

            if (orthoMode)
            {
                camOrtho.FocusCameraOnGameObject(go, allowYOffsetFromGround);
            }
            else
            {
                camPersp.FocusCameraOnGameObject(go, allowYOffsetFromGround);
            }
        }
        #endregion

        private void SetGroundHeightAnimated(float newHeight, bool animate, float duration)
        {
            //print("SetGroundHeightAnimated newHeight:" + newHeight);
            camPersp.SetGroundHeightAnimated(newHeight, animate, duration);
        }
    }
}