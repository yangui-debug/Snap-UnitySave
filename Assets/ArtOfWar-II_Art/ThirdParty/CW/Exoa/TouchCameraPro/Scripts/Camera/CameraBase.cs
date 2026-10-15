using DG.Tweening;
using Exoa.Designer;
using Exoa.Events;
using Lean.Touch;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraBase : MonoBehaviour
    {
        public bool defaultMode;
        protected bool standalone;
        protected LeanScreenDepth HeightScreenDepth;

        protected Camera cam;
        protected CameraBoundaries camBounds;

        protected bool initDataSaved;
        protected Vector3 initOffset;
        protected Quaternion initRotation;

        protected Vector3 finalOffset;
        protected Vector3 finalPosition;
        protected Quaternion finalRotation;
        protected float finalDistance;
        protected Vector3 lastTargetPosition;
        protected bool disableMoves;
        protected float currentPitch;
        protected float currentYaw;
        protected float Pitch;
        protected float Yaw;
        protected float deltaYaw;
        protected float deltaPitch;

        protected Quaternion twistRot;

        protected Vector3 worldPointCameraCenter;
        protected Vector3 worldPointFingersCenter;
        protected Vector3 worldPointFingersDelta;

        [Header("INPUTS")] public InputMap rightClickDrag = InputMap.Translate;
        public InputMap middleClickDrag = InputMap.Translate;
        public InputMap oneFingerDrag = InputMap.Rotate;
        public InputMap twoFingerDrag = InputMap.None;
        public float groundHeight = 0f;
        public bool lastDisableTranslation;
        public bool disableTranslation;

        public bool isAchievedMaxDistance;
        public bool isAchievedMinDistance;

        //[Header("SMOOTHNESS")]
        //[Range(0f, .999f)]
        //public float moveSmoothness;

        public enum InputMap
        {
            Translate,
            Rotate,
            None
        };

        public Quaternion FinalRotation
        {
            get { return finalRotation; }
        }

        public Vector3 FinalPosition
        {
            get { return finalPosition; }
        }

        public bool DisableMoves
        {
            get { return disableMoves; }

            set { disableMoves = value; }
        }

        public Vector3 FinalOffset
        {
            get { return finalOffset; }

            set { finalOffset = value; }
        }

        public virtual float FinalDistance
        {
            get => finalDistance;
            set => finalDistance = value;
        }

        public Vector2 PitchAndYaw
        {
            get => new Vector2(currentPitch, currentYaw);
        }

        public float GetDistance()
        {
            return FinalDistance;
        }

        virtual protected void OnDestroy()
        {
            CameraEvents.OnBeforeSwitchPerspective -= OnBeforeSwitchPerspective;
            CameraEvents.OnAfterSwitchPerspective -= OnAfterSwitchPerspective;
            CameraEvents.OnRequestButtonAction -= OnRequestButtonAction;
            CameraEvents.OnRequestObjectFocus -= FocusCameraOnGameObject;
            CameraEvents.OnRequestObjectFollow -= FollowGameObject;
            CameraEvents.OnRequestGroundHeightChange -= SetGroundHeightAnimated;
        }

        virtual protected void Awake()
        {
            cam = GetComponent<Camera>();
            camBounds = GetComponent<CameraBoundaries>();
            standalone = GetComponent<CameraModeSwitcher>() == null;
            Init();
            CameraEvents.OnBeforeSwitchPerspective += OnBeforeSwitchPerspective;
            CameraEvents.OnAfterSwitchPerspective += OnAfterSwitchPerspective;

            if (standalone)
            {
                CameraEvents.OnRequestButtonAction += OnRequestButtonAction;
                CameraEvents.OnRequestObjectFocus += FocusCameraOnGameObject;
                CameraEvents.OnRequestObjectFollow += FollowGameObject;
                CameraEvents.OnRequestGroundHeightChange += SetGroundHeightAnimated;
            }
        }

        private bool firstUpdateDone;

        virtual protected void LateUpdate()
        {
            if (!firstUpdateDone)
            {
                enabled = defaultMode || standalone;
                firstUpdateDone = true;
            }
        }

        virtual protected void Init()
        {
            CreateConverter();
        }

        virtual protected void CreateConverter()
        {
            HeightScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.HeightIntercept, -5, groundHeight);
        }

        virtual protected void ApplyToCamera()
        {
            if (standalone)
            {
                //transform.position = Vector3.Lerp(transform.position, FinalPosition, (1 - moveSmoothness) * 60 * Time.deltaTime);
                //transform.rotation = Quaternion.Lerp(transform.rotation, FinalRotation, (1 - moveSmoothness) * 60 * Time.deltaTime);
                transform.position = FinalPosition;
                transform.rotation = FinalRotation;
            }
        }

        public void SetGroundHeightAnimated(float newHeight, bool animate, float duration)
        {
            if (animate)
            {
                DOTween.To(() => groundHeight, x => groundHeight = x, newHeight, duration).SetEase(Ease.OutCubic).OnUpdate(() =>
                {
                    SetGroundHeight(groundHeight);
                });
            }
            else
            {
                SetGroundHeight(newHeight);
            }
        }

        public void SetGroundHeight(float v)
        {
            groundHeight = v;
            HeightScreenDepth.Distance = groundHeight;
            finalOffset.y = groundHeight;
        }

        protected bool IsInputMatching(InputMap action)
        {
            if (middleClickDrag == action && Input.GetMouseButton(2))
            {
                return true;
            }

            if (rightClickDrag == action && Input.GetMouseButton(1) && !Input.GetMouseButton(0))
            {
                return true;
            }

            if (twoFingerDrag == action && Inputs.TwoFingerFilter.UpdateAndGetFingers().Count == 2)
            {
                return true;
            }

            if (oneFingerDrag == action && Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
            {
                return true;
            }

            return false;
        }

        protected bool IsUsingCameraEdgeBoundaries()
        {
            return camBounds != null && camBounds.mode == CameraBoundaries.Mode.CameraEdges;
        }

        protected Vector3 ClampCameraCorners(Vector3 finalPosition, out bool clampApplied, bool bottomEdges = true, bool topEdges = true)
        {
            if (!IsUsingCameraEdgeBoundaries())
            {
                clampApplied = false;
                return finalPosition;
            }

            Vector3 diffBL = Vector3.zero,
                diffBR = Vector3.zero,
                diffTR = Vector3.zero,
                diffTL = Vector3.zero;

            if (bottomEdges)
            {
                Vector3 worldPointBottomLeft = HeightScreenDepth.Convert(cam.ViewportToScreenPoint(new Vector3(0, 0, 0)));
                Vector3 worldPointBottomLeftClamped = ClampPointsXZ(worldPointBottomLeft);
                diffBL = worldPointBottomLeftClamped - worldPointBottomLeft;

                Vector3 worldPointBottomRight = HeightScreenDepth.Convert(cam.ViewportToScreenPoint(new Vector3(1, 0, 0)));
                Vector3 worldPointBottomRightClamped = ClampPointsXZ(worldPointBottomRight);
                diffBR = worldPointBottomRightClamped - worldPointBottomRight;
            }

            if (topEdges)
            {
                Vector3 worldPointTopRight = HeightScreenDepth.Convert(cam.ViewportToScreenPoint(new Vector3(1, 1, 0)));
                Vector3 worldPointTopRightClamped = ClampPointsXZ(worldPointTopRight);
                diffTR = worldPointTopRightClamped - worldPointTopRight;

                Vector3 worldPointTopLeft = HeightScreenDepth.Convert(cam.ViewportToScreenPoint(new Vector3(0, 1, 0)));
                Vector3 worldPointTopLeftClamped = ClampPointsXZ(worldPointTopLeft);
                diffTL = worldPointTopLeftClamped - worldPointTopLeft;
            }

            float m = Mathf.Abs((diffTR + diffBR + diffBL + diffTL).magnitude);
            clampApplied = m > 0;

            // print("m:" + m + " apply:" + clampApplied);
            return Vector3.Lerp(finalPosition, finalPosition + diffTR + diffBR + diffBL + diffTL, (1 - camBounds.edgeElasticity) * 60 * Time.deltaTime);
        }

        public Vector3 ClampPointsXZ(Vector3 targetPosition)
        {
            if (camBounds != null)
                return camBounds.ClampPointsXZ(targetPosition, out bool isInBoundaries, groundHeight);
            return targetPosition;
        }

        virtual protected void OnBeforeSwitchPerspective(bool orthoMode)
        {
        }

        virtual protected void OnAfterSwitchPerspective(bool orthoMode)
        {
        }

        protected void OnRequestButtonAction(CameraEvents.Action action, bool active)
        {
            if (action == CameraEvents.Action.ResetCameraPositionRotation)
                ResetCamera();
            else if (action == CameraEvents.Action.DisableCameraMoves)
                DisableCameraMoves(active);
        }

        virtual public Matrix4x4 GetMatrix()
        {
            return new Matrix4x4();
        }

        public LeanScreenDepth GetLeanScreenDepth()
        {
            return HeightScreenDepth;
        }

        protected Vector3 CalculateNewCenter(Vector3 pos, Quaternion rot)
        {
            float adj = (pos.y - groundHeight) / Mathf.Tan(Mathf.Deg2Rad * rot.eulerAngles.x);
            Vector3 camForward = Quaternion.Euler(0, rot.eulerAngles.y, 0) * Vector3.forward;
            Vector3 camOffset = pos.SetY(groundHeight) + camForward.normalized * adj;
            return camOffset;
        }

        protected Vector3 CalculateNewPosition(Vector3 center, Quaternion rot, float distance)
        {
            return rot * (Vector3.back * distance) + center;
        }

        public float CalculateDistanceFromPositionAndRotation(Vector3 pos, Quaternion rot)
        {
            float distance = Mathf.Abs((pos.y - groundHeight) / Mathf.Cos(Mathf.Deg2Rad * (90 - rot.eulerAngles.x)));
            return distance;
        }

        protected Vector3 CalculateOffsetFromPosition(Vector3 pos, Quaternion rot, float distance, float groundHeight)
        {
            Vector3 offset = pos - rot * (Vector3.back * distance);
            return offset.SetY(groundHeight);
        }

        protected float CalculateClampedDistance(Vector3 camPos, Vector3 worldPoint, Vector2 minMaxDistance, float multiplier = 1)
        {
            Vector3 vecWorldCenterToCamera = (camPos - worldPoint);
            float rawValue = vecWorldCenterToCamera.magnitude * multiplier;
            isAchievedMinDistance = Mathf.Abs(rawValue - minMaxDistance.x) < 50;
            isAchievedMaxDistance = Mathf.Abs(rawValue - minMaxDistance.y) < 50;
            return Mathf.Clamp(vecWorldCenterToCamera.magnitude * multiplier, minMaxDistance.x, minMaxDistance.y);
        }

        protected float CalculateClampedDistance(Vector3 camPos, Vector3 worldPoint, float minMaxDistance, float multiplier = 1)
        {
            Vector3 vecWorldCenterToCamera = (camPos - worldPoint);
            return Mathf.Clamp(vecWorldCenterToCamera.magnitude * multiplier, minMaxDistance, minMaxDistance);
        }

        virtual public void DisableCameraMoves(bool active)
        {
            DisableMoves = active;
        }

        virtual protected Quaternion CalculateInitialRotation()
        {
            return initRotation;
        }

        protected float GetRotationSensitivity()
        {
            // Adjust sensitivity by FOV?
            if (cam.orthographic == false)
            {
                return cam.fieldOfView / 90.0f;
            }

            return 1.0f;
        }

        virtual protected void ClampRotation()
        {
        }

        static public float ModularClamp(float val, float min, float max, float rangemin = -180f, float rangemax = 180f)
        {
            var modulus = Mathf.Abs(rangemax - rangemin);
            if ((val %= modulus) < 0f) val += modulus;
            return Mathf.Clamp(val + Mathf.Min(rangemin, rangemax), min, max);
        }

        /// returns angle in range -180 to 180
        protected float NormalizeAngle(float a)
        {
            if (a > 180) a -= 360;
            if (a < -180) a += 360;
            return a;
        }

        #region RESET

        virtual public void ResetCamera()
        {
        }

        #endregion

        #region FOCUS

        [Header("FOCUS")] public float focusTweenDuration = 1f;
        public Ease focusEase = Ease.InOutCubic;
        public float focusDistanceMultiplier = 1f;
        public float focusRadiusMultiplier = 1f;
        public float focusLerp = .1f;

        virtual public void FocusCameraOnGameObject(GameObject go, bool allowYOffsetFromGround = false)
        {
            targetGo = go;
            isFocusingOrFollowing = targetGo != null;
            enableFollowGameObject = true;
            enableFocusOnGameObject = true;
        }

        #endregion

        #region FOLLOW

        [Header("FOLLOW")] public float followRadiusMultiplier = 1f;
        public float followLerp = .1f;
        protected GameObject targetGo;
        protected bool enableFocusOnGameObject;
        protected bool enableFollowGameObject;
        protected bool isFocusingOrFollowing;
        protected Vector3 followOffset;
        protected float followReachAutoStopThreshold;

        public void FollowGameObject(GameObject go, bool andFocus)
        {
            FollowGameObject(go, Vector3.zero, andFocus);
        }

        public void FollowGameObject(GameObject go, Vector3 offset, bool andFocus, float autoStopThreshold = 0)
        {
            followOffset = offset;
            targetGo = go;
            followReachAutoStopThreshold = autoStopThreshold;

            isFocusingOrFollowing = targetGo != null;
            enableFollowGameObject = true;
            enableFocusOnGameObject = andFocus;
        }

        public void StopFollow()
        {
            FollowGameObject(null, Vector3.zero, false);
        }

        #endregion

        #region INERTIA

        [Header("INERTIA")] public bool enableTranslationInertia;
        [Range(0.01f, 5)] public float translationInertiaDuration = 2f;
        [Range(0.01f, 50)] public float translationInertiaMultiplier = 40f;
        protected float translationInertiaForce = 40f;
        protected float translationMaxIntertia = 1f;
        protected Vector3 translationVec;
        protected Vector3 lastOffset;

        public bool enableRotationInertia;
        [Range(0.01f, 5)] public float rotationInertiaDuration = 3f;
        [Range(0.01f, 100)] public float rotationInertiaMultiplier = 60f;
        protected float rotationInertiaForce = 40f;
        protected float rotationMaxIntertia = 1f;
        protected Vector2 rotationVec;
        protected Vector2 lastRotation;

        protected float inertiaTime;
        protected float inertiaThreshold = 0.1f;

        public void ResetInertia()
        {
            translationVec = Vector3.zero;
            rotationVec = Vector2.zero;
        }

        virtual protected void CalculateInertia()
        {
            Vector3 offset = finalOffset - lastOffset;
            if (offset.sqrMagnitude < inertiaThreshold)
            {
                translationVec = Vector3.zero;
                rotationVec = Vector2.zero;
                return;
            }

            translationVec = offset / (Time.deltaTime * translationInertiaForce);
            rotationVec = new Vector2(currentPitch - lastRotation.x, currentYaw - lastRotation.y) / (Time.deltaTime * rotationInertiaForce);

            lastOffset = finalOffset;
            lastRotation = new Vector2(currentPitch, currentYaw);

            inertiaTime = 0;
        }

        virtual protected void ApplyInertia()
        {
            if (enableTranslationInertia)
            {
                translationVec = translationVec.magnitude > translationMaxIntertia ? translationVec.normalized * translationMaxIntertia : translationVec;

                float period = translationVec.magnitude * translationInertiaDuration;
                float easedTime = EaseOutCubic(0, 1, inertiaTime / period);
                Vector3 finalTranslationVec = translationVec * translationInertiaMultiplier * Mathf.Max(0, easedTime);
                if (easedTime > 0 && easedTime <= 1)
                {
                    finalOffset = lastOffset + finalTranslationVec;
                    finalOffset = ClampPointsXZ(finalOffset);
                }
            }

            if (enableRotationInertia)
            {
                rotationVec = rotationVec.magnitude > rotationMaxIntertia ? rotationVec.normalized * rotationMaxIntertia : rotationVec;

                float period = rotationVec.magnitude * translationInertiaDuration;
                float easedTime = EaseOutCubic(0, 1, inertiaTime / period);
                Vector2 finalRotationVec = rotationVec * rotationInertiaMultiplier * Mathf.Max(0, easedTime);
                if (easedTime > 0 && easedTime <= 1)
                {
                    Vector2 r = lastRotation + finalRotationVec;
                    currentPitch = Pitch = r.x;
                    currentYaw = Yaw = r.y;
                    ClampRotation();
                    finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0.0f);
                }
            }

            inertiaTime += Time.deltaTime;
        }

        public static float EaseOutCubic(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value + 1) + start;
        }

        #endregion
    }
}