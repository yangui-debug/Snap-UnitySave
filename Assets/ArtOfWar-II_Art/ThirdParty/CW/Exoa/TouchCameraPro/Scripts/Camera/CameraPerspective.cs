using System;
using System.Collections.Generic;
using DG.Tweening;
using Exoa.Designer;
using Exoa.Events;
using Lean.Touch;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraPerspective : CameraPerspBase
    {
        [Header("ROTATION")] public bool allowPitchRotation = true;
        public float PitchSensitivity = 0.25f;
        public bool PitchClamp = true;
        public Vector2 PitchMinMax = new Vector2(5.0f, 90.0f);
        private Vector2 initialRotation = new Vector2(35, 0);
        public bool allowYawRotation = true;
        public float YawSensitivity = 0.25f;

        public bool forceDisableRotation = false;
        [Header("TRANSLATION")] public float maxTranslationSpeed = 40f;

        private bool translating;

        private bool opEnable = true;

        public bool OpEnable
        {
            get => opEnable;
            set => opEnable = value;
        }

        public bool Translating => translating;

        override protected void Init()
        {
            base.Init();

            // Calculating the initial parameters based on camera's transform
            initialRotation = transform.rotation.eulerAngles;
            CalculateInitialRotation();
            initDistance = CalculateDistanceFromPositionAndRotation(transform.position, transform.rotation);
            initOffset =
                CalculateOffsetFromPosition(transform.position, transform.rotation, initDistance, groundHeight);

            currentPitch = Pitch = initialRotation.x;
            currentYaw = Yaw = initialRotation.y;

            finalOffset = initOffset;
            FinalDistance = initDistance;
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            ApplyToCamera();
        }

        public void ForceApplyRotation()
        {
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
        }

        void Update()
        {
            if (disableMoves)
                return;

            float pinchRatio = Inputs.pinchRatio;
            float scrollRatio = Inputs.GetScroll();

            if (isFocusingOrFollowing)
            {
                HandleFocusAndFollow();
            }

            if (GameObjectExtensions.IsPointerOverGameObject() || !opEnable)
            {
                return;
            }

            if (IsInputMatching(InputMap.Translate))
            {
                List<LeanFinger> fingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
                disableTranslation = fingers.Count == 2;

                worldPointFingersCenter =
                    ClampPointsXZ(HeightScreenDepth.Convert(Inputs.screenPointAnyFingerCountCenter));

                worldPointFingersDelta =
                    Vector3.ClampMagnitude(
                        HeightScreenDepth.ConvertDelta(Inputs.lastScreenPointAnyFingerCountCenter,
                            Inputs.screenPointAnyFingerCountCenter, gameObject), maxTranslationSpeed);

                if (disableTranslation)
                {
                    worldPointFingersDelta = Vector3.zero;
                }
                else if (!translating)
                {
                    if (worldPointFingersDelta.sqrMagnitude < 0.05f)
                    {
                        return;
                    }

                    translating = true;
                }

                Vector3 vecFingersCenterToCamera = (finalPosition - worldPointFingersCenter);
                float vecFingersCenterToCameraDistance = vecFingersCenterToCamera.magnitude * pinchRatio;
                vecFingersCenterToCamera = vecFingersCenterToCamera.normalized * vecFingersCenterToCameraDistance;

                Vector3 targetPosition = worldPointFingersCenter + vecFingersCenterToCamera;

                if (allowYawRotation && Mathf.Abs(Inputs.twistDegrees) > 1)
                {
                    twistRot = Quaternion.AngleAxis(Inputs.twistDegrees, Vector3.up);
                }
                else
                {
                    twistRot = Quaternion.AngleAxis(0, Vector3.up);
                }

                Vector3 offsetFromFingerCenter = worldPointFingersCenter - worldPointFingersDelta;

                finalPosition = twistRot * (targetPosition - worldPointFingersCenter) + offsetFromFingerCenter;

                if (!forceDisableRotation)
                {
                    finalRotation = twistRot * finalRotation;
                    currentPitch = Pitch = NormalizeAngle(finalRotation.eulerAngles.x);
                    currentYaw = Yaw = finalRotation.eulerAngles.y;
                }

                Vector3 newWorldPointCameraCenter = CalculateNewCenter(finalPosition, finalRotation);
                Vector3 newWorldPointCameraCenterClamped = ClampPointsXZ(newWorldPointCameraCenter);

                finalOffset = newWorldPointCameraCenter;
                FinalDistance = CalculateClampedDistance(finalPosition, newWorldPointCameraCenter, minMaxDistance);
                finalPosition = CalculateNewPosition(newWorldPointCameraCenterClamped, finalRotation, finalDistance);

                CalculateInertia();
            }
            else if (Math.Abs(scrollRatio - 1) > 0.001f)
            {
                Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));
                worldPointCameraCenter = ClampPointsXZ(HeightScreenDepth.Convert(screenCenter));
                finalOffset = worldPointCameraCenter;
                FinalDistance =
                    CalculateClampedDistance(finalPosition, worldPointCameraCenter, minMaxDistance, scrollRatio);
                finalPosition = CalculateNewPosition(worldPointCameraCenter, finalRotation, FinalDistance);
            }
            else if (oneFingerDrag == InputMap.Rotate && IsInputMatching(InputMap.Rotate)) // 单指操作
            {
                Vector2 offset = Inputs.oneFingerScaledPixelDelta;
                if (Math.Abs(offset.y) > 0.001f || Math.Abs(offset.x) > 0.001f)
                {
                    Rotate(offset);
                    ForceApplyRotation();
                    CalculateInertia();
                }
            }
            else
            {
                if (IsInputMatching(InputMap.Rotate))
                {
                    Rotate(Inputs.oneFingerScaledPixelDelta);
                    CalculateInertia();
                }
                else
                {
                    ApplyInertia();
                }

                UpdatePosRotDis();

                translating = false;
            }

            // Apply Edge Boundaries
            if (IsUsingCameraEdgeBoundaries())
            {
                finalPosition = ClampCameraCorners(finalPosition, out bool clampApplied, true, currentPitch > 60);
                finalOffset = CalculateOffsetFromPosition(finalPosition, finalRotation, FinalDistance, groundHeight);
                if (clampApplied)
                {
                    CalculateInertia();
                }
            }

            if (!initDataSaved)
            {
                //SetResetValues(finalOffset, FinalRotation, finalDistance);
            }

            ApplyToCamera();
        }

        public void RefreshImmediate()
        {
            UpdatePosRotDis();
            ApplyToCamera();
        }

        private void UpdatePosRotDis()
        {
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, finalDistance);

            Vector3 newWorldPointCameraCenter = CalculateNewCenter(finalPosition, finalRotation);
            Vector3 newWorldPointCameraCenterClamped = ClampPointsXZ(newWorldPointCameraCenter);

            FinalDistance = CalculateClampedDistance(finalPosition, newWorldPointCameraCenter, minMaxDistance);
            finalPosition = CalculateNewPosition(newWorldPointCameraCenterClamped, finalRotation, finalDistance);
        }

        override protected Quaternion CalculateInitialRotation()
        {
            initRotation = Quaternion.Euler(initialRotation.x, initialRotation.y, 0);
            return initRotation;
        }

        override public void SetResetValues(Vector3 offset, Quaternion rotation, float distance)
        {
            initOffset = offset;
            initDistance = distance;
            initialRotation = rotation.eulerAngles;
            CalculateInitialRotation();
            initDataSaved = true;

            //print("SetResetValues initOffset:" + initOffset);
        }

        override public void ResetCamera()
        {
            finalOffset = initOffset;
            finalRotation = initRotation;
            FinalDistance = initDistance;
            translationVec = Vector3.zero;
            rotationVec = Vector2.zero;

            DOTween.To(() => FinalDistance, x => FinalDistance = x, initDistance, focusTweenDuration)
                .SetEase(focusEase);
            ApplyToCamera();

            disableMoves = true;
            float currentDist = FinalDistance;
            float lerp = 0;
            Tween t = DOTween.To(() => lerp, x => lerp = x, 1, focusTweenDuration).SetEase(focusEase);
            t.OnUpdate(() =>
                {
                    FinalDistance = Mathf.Lerp(currentDist, initDistance, lerp);
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

        private Sequence cameraResetAnim;

        public void ResetCameraWithAnimation()
        {
            StopFollow();

            cameraResetAnim = DOTween.Sequence();
            cameraResetAnim.Join(DOTween.To(() => FinalDistance, x => FinalDistance = x, initDistance, focusTweenDuration).SetEase(focusEase));
            Quaternion currentRot = finalRotation;
            float currentDist = FinalDistance;
            Vector3 currentOffset = finalOffset;
            disableMoves = true;
            float lerp = 0;
            cameraResetAnim.Join(DOTween.To(() => lerp, x => lerp = x, 1, focusTweenDuration).SetEase(focusEase));
            cameraResetAnim.OnUpdate(() =>
                {
                    finalOffset = Vector3.Lerp(currentOffset, initOffset, lerp);
                    finalRotation = Quaternion.Lerp(currentRot, initRotation, lerp);
                    FinalDistance = Mathf.Lerp(currentDist, initDistance, lerp);
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

        protected override void ClampRotation()
        {
            if (PitchClamp)
            {
                currentPitch = Pitch = Mathf.Clamp(NormalizeAngle(currentPitch), PitchMinMax.x, PitchMinMax.y);
            }
        }

        public static float ClampAngle(float current, float min = -90f, float max = 90f)
        {
            float dtAngle = Mathf.Abs(((min - max) + 180) % 360 - 180);
            float hdtAngle = dtAngle * 0.5f;
            float midAngle = min + hdtAngle;

            float offset = Mathf.Abs(Mathf.DeltaAngle(current, midAngle)) - hdtAngle;
            if (offset > 0)
                current = Mathf.MoveTowardsAngle(current, midAngle, offset);
            return current;
        }

        #endregion

        #region FOCUS

        private Tween focusTween;

        public override void FocusCameraOnGameObject(GameObject go, bool allowYOffsetFromGround = false)
        {
            Bounds b = go.GetBoundsRecursive(followOffset);

            FocusCameraOnGameObject(b, () => CameraEvents.OnFocusComplete?.Invoke(go), allowYOffsetFromGround);
        }

        public void Focus(Vector3 targetPos, float targetDis, float animDuration = 1f, Action onComplete = null, Ease ease = Ease.Unset)
        {
            // Disable follow mode
            StopFollow();

            if (focusTween != null)
            {
                DOTween.Kill(focusTween);
                focusTween = null;
            }

            disableMoves = true;
            float lerp = 0;
            Vector3 currentOffset = finalOffset;
            float currentDistance = finalDistance;
            var animEase = ease == Ease.Unset ? focusEase : ease;

            focusTween = DOTween.To(() => lerp, x => lerp = x, 1, animDuration).SetEase(animEase);
            focusTween.OnUpdate(() =>
            {
                finalOffset = Vector3.Lerp(currentOffset, targetPos, lerp);
                finalDistance = Mathf.Lerp(currentDistance, targetDis, lerp);
                finalPosition = CalculateNewPosition(finalOffset, finalRotation, finalDistance);
                ApplyToCamera();
            }).OnComplete(() =>
            {
                disableMoves = false;
                onComplete?.Invoke();
            });
        }

        public void StopAnim()
        {
            if (focusTween != null)
            {
                focusTween.Kill();
                focusTween = null;
            }

            if (cameraAnima != null)
            {
                cameraAnima.Kill(true);
                cameraAnima = null;
            }

            if (cameraResetAnim != null)
            {
                cameraResetAnim.Kill(true);
                cameraResetAnim = null;
            }

            if (shakeAnima != null)
            {
                shakeAnima.Complete(true);
                shakeAnima.Kill();
                shakeAnima = null;
            }
        }

        public Tween FocusCameraOnGameObject(Bounds b, Action onComplete = null, bool allowYOffsetFromGround = false, float animDuration = 1f,
            bool ignoreTimescale = false)
        {
            if (b.size == Vector3.zero && b.center == Vector3.zero)
                return null;

            b.center = ClampPointsXZ(b.center);

            // offseting the bounding box
            if (!allowYOffsetFromGround)
            {
                float yOffset = b.center.y;
                b.extents = b.extents.SetY(b.extents.y + yOffset);
                b.center = b.center.SetY(groundHeight);
            }

            Vector3 max = b.size;

            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude * focusRadiusMultiplier;

            // Debug.Log("FocusCameraOnGameObject targetGo position:" + go.transform.position + " b.center:" + b.center);

            float aspect = cam.aspect;
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad / 2f) * aspect) * Mathf.Rad2Deg;

            // Use the smaller FOV as it limits what would get cut off by the frustum        
            float fovMin = Mathf.Min(fov, horizontalFOV);
            float dist = radius / (Mathf.Sin(fovMin * Mathf.Deg2Rad / 2f));

            Vector3 targetOffset = b.center;
            float targetDistance = Mathf.Clamp((dist * focusDistanceMultiplier), minMaxDistance.x, minMaxDistance.y);

            // Disable follow mode
            StopFollow();

            if (targetOffset != finalOffset || FinalDistance != targetDistance)
            {
                disableMoves = true;

                float currentDist = FinalDistance;
                Vector3 currentOffset = finalOffset;
                float lerp = 0;

                if (focusTween != null)
                {
                    DOTween.Kill(focusTween, true);
                    focusTween = null;
                }

                focusTween = DOTween.To(() => lerp, x => lerp = x, 1, animDuration).SetEase(focusEase).SetAutoKill(true).SetRecyclable(false);
                focusTween.SetUpdate(ignoreTimescale);
                focusTween.OnUpdate(() =>
                {
                    targetOffset = b.center.SetY(groundHeight);

                    finalOffset = Vector3.Lerp(currentOffset, targetOffset, lerp);
                    FinalDistance = Mathf.Lerp(currentDist, targetDistance, lerp);

                    finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
                    ApplyToCamera();
                }).OnComplete(() =>
                {
                    disableMoves = false;
                    onComplete?.Invoke();
                });

                return focusTween;
            }

            return null;
        }

        #endregion

        public void MoveCameraTo(Vector3 targetOffset, float targetDistance, float lerp = .1f)
        {
            finalOffset = Vector3.Lerp(finalOffset, targetOffset, lerp);
            FinalDistance = Mathf.Lerp(FinalDistance, targetDistance, lerp);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            ApplyToCamera();
        }

        public void MoveCameraToWithEdge(Vector3 targetOffset, float targetDistance, float lerp = .1f)
        {
            finalOffset = Vector3.Lerp(finalOffset, targetOffset, lerp);
            FinalDistance = Mathf.Lerp(FinalDistance, targetDistance, lerp);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);

            if (IsUsingCameraEdgeBoundaries())
            {
                finalPosition = ClampCameraCorners(finalPosition, out bool clampApplied, true, currentPitch > 60);
                finalOffset = CalculateOffsetFromPosition(finalPosition, finalRotation, FinalDistance, groundHeight);
                if (clampApplied)
                {
                    CalculateInertia();
                }
            }

            ApplyToCamera();
        }

        private void HandleFocusAndFollow()
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

            //Debug.Log("HandleFocusAndFollow targetGo position:" + targetGo.transform.position + " b.center:" + b.center);

            Vector3 max = b.size;

            // Get the radius of a sphere circumscribing the bounds
            float radius = max.magnitude * followRadiusMultiplier;

            float aspect = cam.aspect;
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad / 2f) * aspect) * Mathf.Rad2Deg;

            // Use the smaller FOV as it limits what would get cut off by the frustum        
            float fovMin = Mathf.Min(fov, horizontalFOV);
            float dist = radius / (Mathf.Sin(fovMin * Mathf.Deg2Rad / 2f));

            Vector3 targetOffset = b.center;
            float targetDistance = Mathf.Clamp((dist * followDistanceMultiplier), minMaxDistance.x, minMaxDistance.y);

            if (enableFocusOnGameObject)
            {
                FinalDistance = Mathf.Lerp(FinalDistance, targetDistance, focusLerp);
            }

            finalOffset = worldPointCameraCenter = Vector3.Lerp(finalOffset, targetOffset, followLerp);
            Vector3 targetPos = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            finalPosition = Vector3.Lerp(finalPosition, targetPos, followLerp);

            ApplyToCamera();

            if (followReachAutoStopThreshold > 0)
            {
                bool focusAndFollowReached = !enableFocusOnGameObject || Mathf.Abs(FinalDistance - targetDistance) < followReachAutoStopThreshold;
                focusAndFollowReached &= Vector3.Distance(finalPosition, targetPos) < followReachAutoStopThreshold;
                if (focusAndFollowReached)
                {
                    StopFollow();
                }
            }
        }

        public void ChangeOneFingersInputMap(InputMap inputMap)
        {
            oneFingerDrag = inputMap;
        }

        public void ChangeTwoFingersInputMap(InputMap inputMap)
        {
            twoFingerDrag = inputMap;
            allowYawRotation = inputMap == InputMap.Translate;
        }

        public void SetRotationSwitch(bool value)
        {
            allowYawRotation = value;
            allowPitchRotation = value;
            forceDisableRotation = !value;
        }

        private void TryPlayReboundAnimation()
        {
            List<LeanFinger> fingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
            if (isAchievedMaxDistance && fingers.Count != 2)
            {
                var targetDistance = FinalDistance - 200;
                DOTween.To(() => FinalDistance, x => FinalDistance = x, targetDistance, focusTweenDuration)
                    .SetEase(focusEase);
                ApplyToCamera();

                float currentDist = FinalDistance;
                float lerp = 0;
                Tween t = DOTween.To(() => lerp, x => lerp = x, 0.05f, focusTweenDuration).SetEase(focusEase);
                t.OnUpdate(() =>
                {
                    FinalDistance = Mathf.Lerp(currentDist, targetDistance, lerp);
                    finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
                    ApplyToCamera();
                });
            }

            if (isAchievedMinDistance && fingers.Count != 2)
            {
                var targetDistance = FinalDistance + 200;
                DOTween.To(() => FinalDistance, x => FinalDistance = x, targetDistance, focusTweenDuration)
                    .SetEase(focusEase);
                ApplyToCamera();

                float currentDist = FinalDistance;
                float lerp = 0;
                Tween t = DOTween.To(() => lerp, x => lerp = x, 0.05f, focusTweenDuration).SetEase(focusEase);
                t.OnUpdate(() =>
                {
                    FinalDistance = Mathf.Lerp(currentDist, targetDistance, lerp);
                    finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
                    ApplyToCamera();
                });
                isAchievedMinDistance = false;
            }
        }

        public void ApplyCurrentState()
        {
            Transform trans = transform;
            ApplyTo(trans.position, trans.rotation);
        }

        private Sequence cameraAnima;
        public bool InAnimation => cameraAnima != null && cameraAnima.IsPlaying();

        public Sequence AnimationApplyTo(Vector3 p, Quaternion r, float duration = 1, Ease ease = Ease.Linear, bool autoEnable = true, Action onComplete = null,
            float delay = 0)
        {
            enabled = false;
            ResetInertia();

            Transform trans = transform;
            cameraAnima?.Kill();
            cameraAnima = DOTween.Sequence();
            if (delay > 0)
            {
                cameraAnima.SetDelay(delay);
            }

            cameraAnima.Append(trans.DOLocalMove(p, duration).SetEase(ease));
            cameraAnima.Join(trans.DOLocalRotateQuaternion(r, duration).SetEase(ease));
            cameraAnima.OnUpdate(ApplyCurrentState);
            cameraAnima.OnComplete(() =>
            {
                if (autoEnable)
                {
                    enabled = true;
                }

                ApplyCurrentState();
                onComplete?.Invoke();
            });

            return cameraAnima;
        }

        public void ApplyTo(Vector3 p, Quaternion r)
        {
            float dis = CalculateDistanceFromPositionAndRotation(p, r);
            Vector3 offset = CalculateOffsetFromPosition(p, r, dis, groundHeight);

            Vector3 rotation = r.eulerAngles;
            currentPitch = Pitch = rotation.x;
            currentYaw = Yaw = rotation.y;

            finalOffset = offset;
            FinalDistance = dis;
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
        }

        public void ApplyRotation(Quaternion r)
        {
            Vector3 rotation = r.eulerAngles;
            currentPitch = Pitch = rotation.x;
            currentYaw = Yaw = rotation.y;
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
            finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
        }

        public Sequence Shake(float duration = 0.1f, float strength = 1, int vibrato = 10, float randomness = 90f, Action onComplete = null)
        {
            cameraAnima?.Kill();
            cameraAnima = DOTween.Sequence();
            cameraAnima.Append(transform.DOShakePosition(duration, strength, vibrato, randomness, false, false));
            cameraAnima.SetUpdate(true); // 默认是false, 不受到time scale影响
            cameraAnima.OnComplete(() => { onComplete?.Invoke(); });
            return cameraAnima;
        }

        private Sequence shakeAnima;

        public void InOutShake()
        {
            const float fps = 30f;
            shakeAnima?.Complete(true);
            shakeAnima?.Kill();
            shakeAnima = DOTween.Sequence();

            // 随机震动 (-1 到 1)
            Vector3 xyStrength = new Vector3(0.2f, 0.2f, 0.2f);

            Transform t = transform;
            Vector3 originPos = t.position;
            shakeAnima.Append(t.DOShakePosition(10 / fps, strength: xyStrength, vibrato: 10, randomness: 90, fadeOut: true));
            shakeAnima.OnComplete(() => t.position = originPos);
        }
    }
}