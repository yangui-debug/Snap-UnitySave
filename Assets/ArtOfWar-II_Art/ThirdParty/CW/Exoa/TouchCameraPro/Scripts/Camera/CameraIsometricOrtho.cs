using DG.Tweening;
using Exoa.Designer;
using Exoa.Events;
using Lean.Touch;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraIsometricOrtho : CameraOrthoBase
    {
        [Header("ROTATION")] public bool allowPitchRotation = false;
        private float PitchSensitivity = 0.25f;
        private bool PitchClamp = true;
        public Vector2 PitchMinMax = new Vector2(0.0f, 90.0f);
        public bool allowYawRotation = true;
        public bool YawClamp = false;
        public Vector2 YawMinMax = new Vector2(0.0f, 360.0f);
        public float YawSensitivity = 0.25f;
        private Vector2 initialRotation = new Vector2(45, 45);
        private float maxTranslationSpeed = 10f;

        override protected void Init()
        {
            base.Init();
            initSize = size = cam.orthographicSize;
            initialRotation.y = transform.rotation.eulerAngles.y;
            currentPitch = Pitch = initialRotation.x;
            currentYaw = Yaw = initialRotation.y;
            initDistance = CalculateDistanceFromPositionAndRotation(transform.position, transform.rotation);
            finalOffset = CalculateOffsetFromPosition(transform.position, transform.rotation, initDistance, groundHeight);
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            FinalDistance = initDistance;
        }

        void Update()
        {
            if (disableMoves)
                return;

            List<LeanFinger> twoFingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
            List<LeanFinger> oneFinger = Inputs.OneFingerFilter.UpdateAndGetFingers();
            float oldSize = size;
            Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));

            worldPointCameraCenter = ClampPointsXZ(HeightScreenDepth.Convert(screenCenter));
            float pinchRatio = Inputs.pinchRatio;
            float scrollRatio = Inputs.GetScroll();
            size = Mathf.Clamp(size * Inputs.pinchRatio * scrollRatio, sizeMinMax.x, sizeMinMax.y);

            if (isFocusingOrFollowing)
                FollowGameObject();

            if (IsInputMatching(InputMap.Translate))
            {
                pinchRatio = size / oldSize;

                worldPointFingersCenter = ClampPointsXZ(HeightScreenDepth.Convert(Inputs.screenPointAnyFingerCountCenter));
                worldPointFingersDelta =
                    Vector3.ClampMagnitude(
                        HeightScreenDepth.ConvertDelta(Inputs.lastScreenPointAnyFingerCountCenter, Inputs.screenPointAnyFingerCountCenter, gameObject),
                        maxTranslationSpeed);

                if (disableTranslation)
                    worldPointFingersDelta = Vector3.zero;

                Vector3 vecFingersCenterToCamera = (finalPosition - worldPointFingersCenter);
                float vecFingersCenterToCameraDistance = vecFingersCenterToCamera.magnitude * pinchRatio;
                vecFingersCenterToCamera = vecFingersCenterToCamera.normalized * vecFingersCenterToCameraDistance;

                Vector3 targetPosition = worldPointFingersCenter + vecFingersCenterToCamera;

                twistRot = Quaternion.AngleAxis(allowYawRotation ? Inputs.twistDegrees : 0, Vector3.up);

                Vector3 offsetFromFingerCenter = worldPointFingersCenter - worldPointFingersDelta;

                finalPosition = twistRot * (targetPosition - worldPointFingersCenter) + offsetFromFingerCenter;
                finalRotation = twistRot * finalRotation;

                currentPitch = Pitch = finalRotation.eulerAngles.x;
                currentYaw = Yaw = finalRotation.eulerAngles.y;
                ClampRotation();
                finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);

                Vector3 newWorldPointCameraCenter = CalculateNewCenter(finalPosition, finalRotation);
                Vector3 newWorldPointCameraCenterClamped = ClampPointsXZ(newWorldPointCameraCenter);

                finalOffset = newWorldPointCameraCenter;
                FinalDistance = CalculateClampedDistance(finalPosition, newWorldPointCameraCenter, fixedDistance);
                finalPosition = CalculateNewPosition(newWorldPointCameraCenterClamped, finalRotation, FinalDistance);

                CalculateInertia();

                //print("twistRot:" + twistRot);
            }
            else if (scrollRatio != 1)
            {
                finalOffset = worldPointCameraCenter;
                FinalDistance = CalculateClampedDistance(finalPosition, worldPointCameraCenter, fixedDistance, scrollRatio);
                finalPosition = CalculateNewPosition(worldPointCameraCenter, finalRotation, FinalDistance);
            }
            else
            {
                if (IsInputMatching(InputMap.Rotate))
                {
                    Rotate(Inputs.oneFingerScaledPixelDelta);
                    CalculateInertia();
                }
                else ApplyInertia();

                FinalDistance = CalculateClampedDistance(finalPosition, worldPointCameraCenter, fixedDistance);
                finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
                finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            }

            // Apply Edge Boundaries
            if (IsUsingCameraEdgeBoundaries())
            {
                finalPosition = ClampCameraCorners(finalPosition, out bool clampApplied);
                finalOffset = CalculateOffsetFromPosition(finalPosition, finalRotation, FinalDistance, groundHeight);
                if (clampApplied) CalculateInertia();
            }

            if (!initDataSaved)
            {
                SetResetValues(finalOffset, FinalRotation, size);
            }

            ApplyToCamera();
        }

        override public void ResetCamera()
        {
            StopFollow();

            DOTween.To(() => FinalDistance, x => FinalDistance = x, initDistance, focusTweenDuration).SetEase(focusEase);
            Quaternion currentRot = finalRotation;
            Vector3 currentOffset = finalOffset;
            float currentSize = size;
            disableMoves = true;
            float lerp = 0;
            Tween t = DOTween.To(() => lerp, x => lerp = x, 1, focusTweenDuration).SetEase(focusEase);
            t.OnUpdate(() =>
                {
                    size = Mathf.Lerp(currentSize, initSize, lerp);
                    finalOffset = Vector3.Lerp(currentOffset, initOffset, lerp);
                    finalRotation = Quaternion.Lerp(currentRot, initRotation, lerp);
                    finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
                    ApplyToCamera();
                })
                .OnComplete(() =>
                {
                    currentPitch = Pitch = initialRotation.x;
                    currentYaw = Yaw = initialRotation.y;
                    disableMoves = false;
                });
        }

        override public void SetResetValues(Vector3 offset, Quaternion rotation, float size)
        {
            initOffset = offset;

            initialRotation = rotation.eulerAngles;
            CalculateInitialRotation();
            initSize = size;
            initDataSaved = true;
        }

        override protected Quaternion CalculateInitialRotation()
        {
            initRotation = Quaternion.Euler(initialRotation.x, initialRotation.y, 0);
            return initRotation;
        }

        override protected void ClampRotation()
        {
            if (PitchClamp)
                currentPitch = Mathf.Clamp(NormalizeAngle(currentPitch), PitchMinMax.x, PitchMinMax.y);
            if (YawClamp)
                currentYaw = Mathf.Clamp(NormalizeAngle(currentYaw), YawMinMax.x, YawMinMax.y);
        }

        #region EVENTS

        override protected void OnBeforeSwitchPerspective(bool orthoMode)
        {
            if (!orthoMode)
            {
                currentPitch = Pitch = initialRotation.x;
                currentYaw = Yaw = initialRotation.y;
                finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
                finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            }
        }

        #endregion

        #region UTILS

        public void Rotate(Vector2 delta)
        {
            var sensitivity = GetRotationSensitivity();
            if (allowYawRotation)
            {
                deltaYaw = delta.x * YawSensitivity * sensitivity;
                Yaw += deltaYaw;
            }

            if (allowPitchRotation)
            {
                deltaPitch = -delta.y * PitchSensitivity * sensitivity;
                Pitch += deltaPitch;
            }

            currentPitch = Pitch;
            currentYaw = Yaw;
            ClampRotation();
        }

        #endregion

        #region FOCUS

        override public void FocusCameraOnGameObject(GameObject go, bool allowYOffsetFromGround = false)
        {
            Bounds b = go.GetBoundsRecursive(followOffset);

            if (b.size == Vector3.zero && b.center == Vector3.zero)
                return;

            // offseting the bounding box
            float yOffset = b.center.y;
            b.extents = b.extents.SetY(b.extents.y + yOffset);
            b.center = b.center.SetY(groundHeight);

            Vector3 max = b.size;

            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude * focusRadiusMultiplier;

            Vector3 targetOffset = b.center;

            // Disable follow mode
            StopFollow();

            float targetSize = Mathf.Clamp(radius, sizeMinMax.x, sizeMinMax.y);

            if (targetOffset != finalOffset || size != targetSize)
            {
                disableMoves = true;

                float currentSize = size;
                Vector3 currentOffset = finalOffset;
                float lerp = 0;

                Tween t = DOTween.To(() => lerp, x => lerp = x, 1, focusTweenDuration).SetEase(focusEase);
                t.OnUpdate(() =>
                {
                    b = go.GetBoundsRecursive(followOffset);
                    targetOffset = b.center.SetY(groundHeight);

                    finalOffset = Vector3.Lerp(currentOffset, targetOffset, lerp);
                    size = Mathf.Lerp(currentSize, targetSize, lerp);
                    finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
                    ApplyToCamera();
                }).OnComplete(() =>
                {
                    disableMoves = false;
                    CameraEvents.OnFocusComplete?.Invoke(go);
                });
            }
        }

        #endregion

        #region FOLLOW

        public void FollowGameObject()
        {
            if (!isFocusingOrFollowing)
                return;

            Bounds b = targetGo.GetBoundsRecursive(followOffset);

            if (b.size == Vector3.zero && b.center == Vector3.zero)
                return;

            // offseting the bounding box
            float yOffset = b.center.y;
            b.extents = b.extents.SetY(b.extents.y + yOffset);
            b.center = b.center.SetY(groundHeight);

            Vector3 max = b.size;

            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude * followRadiusMultiplier;
            float targetSize = Mathf.Clamp(radius, sizeMinMax.x, sizeMinMax.y);
            Vector3 targetOffset = b.center;

            if (enableFocusOnGameObject)
            {
                size = targetSize;
            }

            finalOffset = worldPointCameraCenter = targetOffset;
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
        }

        #endregion
    }
}