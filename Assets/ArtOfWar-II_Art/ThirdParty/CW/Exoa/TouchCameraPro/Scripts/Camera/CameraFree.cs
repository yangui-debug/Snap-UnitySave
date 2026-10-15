using DG.Tweening;
using DG.Tweening.Core;
using Exoa.Designer;
using Exoa.Events;
using Lean.Common;
using Lean.Touch;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraFree : CameraPerspective
    {
        private RaycastHit hitInfo;
        private bool isHitting;
        public float maxDistance = 100f;
        public LayerMask layerMask;
        public LeanPlane plane;
        public Transform sphere;



        override protected void CreateConverter()
        {
            HeightScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.PlaneIntercept, -5, groundHeight);
            Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));
            FindGround(screenCenter);
        }

        void Update()
        {

            //return;
            if (disableMoves)
                return;

            List<LeanFinger> twoFingers = Inputs.TwoFingerFilter.UpdateAndGetFingers();
            List<LeanFinger> oneFinger = Inputs.OneFingerFilter.UpdateAndGetFingers();

            Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));

            worldPointCameraCenter = ClampPointsXZ(HeightScreenDepth.Convert(screenCenter));
            float pinchRatio = Inputs.pinchRatio;
            float scrollRatio = Inputs.GetScroll();

            //if (isFocusingOrFollowing)
            //HandleFocusAndFollow();

            if (IsInputMatching(InputMap.Translate))
            {
                if (!isHitting)
                {
                    FindGround(Inputs.screenPointAnyFingerCountCenter);
                }
                if (isHitting)
                {

                    worldPointCameraCenter = ClampPointsXZ(HeightScreenDepth.Convert(screenCenter));
                    worldPointFingersCenter = ClampPointsXZ(HeightScreenDepth.Convert(Inputs.screenPointAnyFingerCountCenter));

                    worldPointFingersDelta = Vector3.ClampMagnitude(HeightScreenDepth.ConvertDelta(Inputs.lastScreenPointAnyFingerCountCenter,
                        Inputs.screenPointAnyFingerCountCenter, gameObject), maxTranslationSpeed);

                    //Debug.Log("worldPointFingersCenter:" + worldPointFingersCenter + " worldPointFingersDelta:" + worldPointFingersDelta);

                    if (disableTranslation)
                        worldPointFingersDelta = Vector3.zero;

                    // pinch scale
                    Vector3 vecFingersCenterToCamera = (finalPosition - worldPointFingersCenter);
                    float vecFingersCenterToCameraDistance = vecFingersCenterToCamera.magnitude * pinchRatio;
                    vecFingersCenterToCamera = vecFingersCenterToCamera.normalized * vecFingersCenterToCameraDistance;

                    Vector3 targetPosition = worldPointFingersCenter + vecFingersCenterToCamera;

                    //Debug.Log("vecFingersCenterToCamera:" + vecFingersCenterToCamera + " targetPosition:" + targetPosition);

                    float belowGroundMultiplier = NormalizeAngle(finalRotation.eulerAngles.x) < 0 ? -1 : 1;
                    twistRot = Quaternion.AngleAxis(allowYawRotation ? Inputs.twistDegrees : 0, Vector3.up * belowGroundMultiplier);

                    Vector3 offsetFromFingerCenter = worldPointFingersCenter - worldPointFingersDelta;
                    //sphere.position = offsetFromFingerCenter;

                    finalPosition = twistRot * (targetPosition - worldPointFingersCenter) + offsetFromFingerCenter;
                    finalRotation = twistRot * finalRotation;

                    currentPitch = Pitch = NormalizeAngle(finalRotation.eulerAngles.x);
                    currentYaw = Yaw = (finalRotation.eulerAngles.y);

                    //Vector3 newWorldPointCameraCenter = CalculateOffsetFromPosition(finalPosition, finalRotation, vecFingersCenterToCamera.magnitude, groundHeight);// CalculateNewCenter(finalPosition, finalRotation);
                    //Vector3 newWorldPointCameraCenter = CalculateNewCenter(finalPosition, finalRotation);
                    Vector3 newWorldPointCameraCenter = worldPointCameraCenter - worldPointFingersDelta * 1;
                    Vector3 newWorldPointCameraCenterClamped = ClampPointsXZ(newWorldPointCameraCenter);

                    finalOffset = newWorldPointCameraCenter;
                    FinalDistance = CalculateClampedDistance(finalPosition, newWorldPointCameraCenter, minMaxDistance);
                    finalPosition = CalculateNewPosition(newWorldPointCameraCenterClamped, finalRotation, FinalDistance);


                    CalculateInertia();
                }
            }
            else if (scrollRatio != 1)
            {
                finalOffset = worldPointCameraCenter;
                FinalDistance = CalculateClampedDistance(finalPosition, worldPointCameraCenter, minMaxDistance, scrollRatio);
                finalPosition = CalculateNewPosition(worldPointCameraCenter, finalRotation, FinalDistance);
            }
            else
            {
                isHitting = false;

                if (IsInputMatching(InputMap.Rotate))
                {
                    Rotate(Inputs.oneFingerScaledPixelDelta);
                    CalculateInertia();
                }
                else ApplyInertia();
                finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
                finalPosition = CalculateNewPosition(finalOffset, finalRotation, FinalDistance);
            }




            // Apply Edge Boundaries
            //if (IsUsingCameraEdgeBoundaries())
            //{
            //finalPosition = ClampCameraCorners(finalPosition, out bool clampApplied, true, currentPitch > 60);
            //finalOffset = CalculateOffsetFromPosition(finalPosition, finalRotation, finalDistance);
            //if (clampApplied) CalculateInertia();
            //}



            ApplyToCamera();




        }

        private void FindGround(Vector2 screenPoint)
        {
            Ray r = cam.ScreenPointToRay(screenPoint);
            isHitting = Physics.Raycast(r, out hitInfo, maxDistance, layerMask.value);
            if (isHitting)
            {
                plane.transform.rotation = Quaternion.LookRotation(hitInfo.normal);
                plane.transform.position = hitInfo.point;
                groundHeight = hitInfo.point.y;
                HeightScreenDepth.Object = plane;
            }
            //print("isHitting:" + isHitting + " groundHeight:" + groundHeight);
        }

        private Vector3 FindHitPoint(Vector2 screenPoint)
        {
            Ray r = cam.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(r, out hitInfo, maxDistance, layerMask.value))
            {
                return hitInfo.point;
            }
            return cam.transform.position + cam.transform.forward * 10;
        }
    }
}