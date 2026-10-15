using DG.Tweening;
using Exoa.Designer;
using Exoa.Events;
using Lean.Touch;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraTopDownOrtho : CameraOrthoBase
    {
        [Header("ROTATION")] private float initialRotationY;
        private float topDownRotation = 90;

        override protected void Init()
        {
            base.Init();
            initSize = size = cam.orthographicSize;
            initialRotationY = transform.rotation.eulerAngles.y;
            initOffset = transform.position.SetY(groundHeight);

            finalOffset = initOffset;
            finalPosition = transform.position.SetY(fixedDistance);
            finalRotation = CalculateInitialRotation();
        }

        void Update()
        {
            if (disableMoves)
                return;

            List<LeanFinger> twoFingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
            List<LeanFinger> oneFinger = Inputs.OneFingerFilter.UpdateAndGetFingers();

            finalPosition = transform.position.SetY(fixedDistance);

            Vector2 screenPoint = default(Vector2);
            float oldSize = size;
            float pinchRatio = Inputs.pinchRatio;
            float scrollRatio = Inputs.GetScroll();

            size = Mathf.Clamp(size * Inputs.pinchRatio * scrollRatio, sizeMinMax.x, sizeMinMax.y);

            if (isFocusingOrFollowing)
            {
                FollowGameObject();
            }

            if (IsInputMatching(InputMap.Translate) && LeanGesture.TryGetScreenCenter(twoFingers, ref screenPoint) == true)
            {
                // Derive actual pinchRatio from the zoom delta (it may differ with clamping)
                pinchRatio = size / oldSize;

                Vector3 worldPointTwoFingersCenter = ClampPointsXZ(HeightScreenDepth.Convert(screenPoint));
                Vector3 worldPointTwoFingersDelta =
                    HeightScreenDepth.ConvertDelta(Inputs.lastScreenPointTwoFingersCenter, Inputs.screenPointTwoFingersCenter, gameObject);

                if (disableTranslation)
                    worldPointTwoFingersDelta = Vector3.zero;

                Vector3 targetPosition = worldPointTwoFingersCenter + (transform.position - worldPointTwoFingersCenter) * pinchRatio;
                targetPosition.y = fixedDistance;
                targetPosition = ClampPointsXZ(targetPosition);

                finalOffset = worldPointTwoFingersCenter - worldPointTwoFingersDelta;
                finalOffset = ClampPointsXZ(finalOffset);

                Quaternion rot = Quaternion.AngleAxis(Inputs.twistDegrees, Vector3.up);
                finalPosition = rot * (targetPosition - worldPointTwoFingersCenter) + finalOffset;
                finalRotation = rot * finalRotation;
                finalPosition = ClampPointsXZ(finalPosition);

                finalOffset = finalPosition.SetY(groundHeight);

                currentPitch = Pitch = finalRotation.eulerAngles.x;
                currentYaw = Yaw = finalRotation.eulerAngles.y;

                CalculateInertia();
            }
            else if (IsInputMatching(InputMap.Translate))
            {
                var lastScreenPoint = LeanGesture.GetLastScreenCenter(oneFinger);
                screenPoint = LeanGesture.GetScreenCenter(oneFinger);

                // Get the world delta of them after conversion
                var worldDelta = HeightScreenDepth.ConvertDelta(lastScreenPoint, screenPoint, gameObject);
                worldDelta.y = 0;

                finalPosition -= worldDelta;
                finalPosition.y = fixedDistance;

                finalPosition = ClampPointsXZ(finalPosition);

                finalOffset = finalPosition.SetY(groundHeight);

                CalculateInertia();
            }
            else
            {
                ApplyInertia();
                finalPosition = finalOffset.SetY(fixedDistance);
            }

            // Apply Edge Boundaries
            if (IsUsingCameraEdgeBoundaries())
            {
                finalPosition = ClampCameraCorners(finalPosition, out bool clampApplied);
                finalOffset = finalPosition.SetY(groundHeight);
                if (clampApplied) CalculateInertia();
            }

            FinalDistance = finalPosition.y;

            if (!initDataSaved)
            {
                //SetResetValues(finalOffset, FinalRotation, size);
            }

            ApplyToCamera();
        }

        #region RESET

        override public void ResetCamera()
        {
            StopFollow();

            Quaternion currentRot = transform.rotation;
            Vector3 currentOffset = finalOffset;
            float currentSize = size;
            initRotation = finalRotation = CalculateInitialRotation();
            disableMoves = true;
            float lerp = 0;

            Tween t = DOTween.To(() => lerp, x => lerp = x, 1, focusTweenDuration).SetEase(focusEase);
            t.OnUpdate(() =>
            {
                size = Mathf.Lerp(currentSize, initSize, lerp);
                finalOffset = Vector3.Lerp(currentOffset, initOffset, lerp);
                finalPosition = finalOffset.SetY(fixedDistance);
                finalRotation = Quaternion.Lerp(currentRot, initRotation, lerp);
                ApplyToCamera();
            }).OnComplete(() => { disableMoves = false; });
        }

        #endregion

        #region UTILS

        override protected Quaternion CalculateInitialRotation()
        {
            initRotation = Quaternion.Euler(topDownRotation, initialRotationY, 0);
            return initRotation;
        }

        override public void SetResetValues(Vector3 offset, Quaternion rotation, float size)
        {
            initOffset = offset;

            currentPitch = Pitch = rotation.eulerAngles.x;
            currentYaw = Yaw = initialRotationY = rotation.eulerAngles.y;
            ;

            CalculateInitialRotation();
            initSize = size;
            initDataSaved = true;
        }

        override public void SetPositionByOffset()
        {
            finalPosition = finalOffset.SetY(fixedDistance);
        }

        #endregion

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

            Vector3 targetOffset = b.center.SetY(groundHeight);
            float targetSize = Mathf.Clamp(radius, sizeMinMax.x, sizeMinMax.y);

            // Disable follow
            StopFollow();

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

                    finalPosition = finalOffset.SetY(fixedDistance);
                    ApplyToCamera();
                }).OnComplete(() =>
                {
                    disableMoves = false;
                    CameraEvents.OnFocusComplete?.Invoke(go);
                });
            }
        }

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

            Vector3 targetOffset = b.center.SetY(groundHeight);
            float targetSize = Mathf.Clamp(radius, sizeMinMax.x, sizeMinMax.y);

            if (enableFocusOnGameObject)
            {
                size = targetSize;
            }

            finalOffset = targetOffset;
            finalPosition = finalOffset.SetY(fixedDistance);
        }

        #endregion
    }
}