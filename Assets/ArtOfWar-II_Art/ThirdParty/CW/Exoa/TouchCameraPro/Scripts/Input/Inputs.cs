using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Exoa.Designer
{
    public class Inputs : MonoBehaviour
    {
        public static bool ControlKey => (Event.current != null && Event.current.control && Event.current.type == EventType.KeyDown);
        public static bool IsOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        private static bool isFingerTap;
        private static bool isFingerUp;

        public static bool IsTranslating()
        {
            List<LeanFinger> fingers = TwoFingerFilter.UpdateAndGetFingers();
            if (fingers.Count == 2)
                return true;
            return Input.GetMouseButton(2);
        }

        public static bool IsRotating(bool allowLeftButton)
        {
            List<LeanFinger> fingers = OneFingerFilter.UpdateAndGetFingers();
            if (fingers.Count == 1 && !Input.GetMouseButton(2) && allowLeftButton)
                return true;

            return Input.GetMouseButton(1);
        }

        public static bool ResetCamera()
        {
            return Input.GetKeyDown(KeyCode.F) && !IsOverUI;
        }

        public static bool IsTap()
        {
            return isFingerTap && !Input.GetMouseButton(1) && !Input.GetMouseButtonUp(1)
                   && !Input.GetMouseButton(2) && !Input.GetMouseButtonUp(2);
        }

        public static bool IsUp()
        {
            return isFingerUp && !Input.GetMouseButton(1) && !Input.GetMouseButtonUp(1)
                   && !Input.GetMouseButton(2) && !Input.GetMouseButtonUp(2);
        }

        private const string Mouse_ScrollWheel = "Mouse ScrollWheel";

        public static float GetScroll()
        {
            if (IsOverUI) return 1;
            return 1 - Input.GetAxis(Mouse_ScrollWheel);
        }

        public static bool SavePressed()
        {
            return Input.GetKeyDown(KeyCode.S) && ControlKey;
        }

        public static bool OpenSaveFolderPressed()
        {
            return Input.GetKeyDown(KeyCode.D) && ControlKey;
        }

        public static bool ChangePlanMode()
        {
            return Input.GetKeyDown(KeyCode.Space) && !IsOverUI;
        }

        public static bool ToggleGizmo()
        {
            return Input.GetKeyDown(KeyCode.G) && !IsOverUI;
        }

        public static bool ToggleExteriorWalls()
        {
            return Input.GetKeyDown(KeyCode.E) && !IsOverUI;
        }

        public static bool ToggleRoof()
        {
            return Input.GetKeyDown(KeyCode.R) && !IsOverUI;
        }

        public static bool ReleaseDrag()
        {
            return Input.GetMouseButtonUp(0);
        }

        public static bool OptionPress()
        {
            return Input.GetMouseButtonDown(1);
        }

        public static bool EscapePressed()
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }

        public static bool AltPressed()
        {
            return Input.GetKey(KeyCode.LeftAlt);
        }

        public static LeanFingerFilter OneFingerFilter = new LeanFingerFilter(LeanFingerFilter.FilterType.AllFingers, true, 1, 0, null);
        public static LeanFingerFilter TwoFingerFilter = new LeanFingerFilter(LeanFingerFilter.FilterType.AllFingers, true, 2, 0, null);
        public static LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.HeightIntercept);

        public static float pinchScale;
        public static float pinchRatio = 1;
        public static float twistDegrees;
        public static Vector2 oneFingerScaledPixelDelta;
        public static Vector2 twoFingerScaledPixelDelta;
        public static Vector2 lastScreenPointTwoFingersCenter;
        public static Vector2 lastScreenPointOneFingerCenter;
        public static Vector2 lastScreenPointAnyFingerCountCenter;
        public static Vector2 screenPointTwoFingersCenter;
        public static Vector2 screenPointOneFingerCenter;
        public static Vector2 screenPointAnyFingerCountCenter;

        void OnDestroy()
        {
            LeanTouch.OnFingerTap -= OnFingerTap;
            LeanTouch.OnFingerUp -= OnFingerUp;
        }

        void Start()
        {
            if (LeanTouch.Instance != null)
            {
                LeanTouch.Instance.UseMouse = !Application.isMobilePlatform;
            }

            LeanTouch.OnFingerTap += OnFingerTap;
            LeanTouch.OnFingerUp += OnFingerUp;
        }

        void Update()
        {
            List<LeanFinger> twoFingers = TwoFingerFilter.UpdateAndGetFingers();
            List<LeanFinger> oneFinger = OneFingerFilter.UpdateAndGetFingers();

            // 2 Fingers
            pinchScale = LeanGesture.GetPinchScale(twoFingers);
            pinchRatio = LeanGesture.GetPinchRatio(twoFingers);
            twistDegrees = LeanGesture.GetTwistDegrees(twoFingers);
            lastScreenPointTwoFingersCenter = LeanGesture.GetLastScreenCenter(twoFingers);
            screenPointTwoFingersCenter = LeanGesture.GetScreenCenter(twoFingers);
            twoFingerScaledPixelDelta = screenPointTwoFingersCenter - lastScreenPointTwoFingersCenter;
            twoFingerScaledPixelDelta *= LeanTouch.ScalingFactor;

            // 1 Finger
            lastScreenPointOneFingerCenter = LeanGesture.GetLastScreenCenter(oneFinger);
            screenPointOneFingerCenter = LeanGesture.GetScreenCenter(oneFinger);
            oneFingerScaledPixelDelta = screenPointOneFingerCenter - lastScreenPointOneFingerCenter;
            oneFingerScaledPixelDelta *= LeanTouch.ScalingFactor;

            // 1 to X Fingers
            screenPointAnyFingerCountCenter = screenPointTwoFingersCenter == Vector2.zero ? screenPointOneFingerCenter : screenPointTwoFingersCenter;
            lastScreenPointAnyFingerCountCenter =
                lastScreenPointTwoFingersCenter == Vector2.zero ? lastScreenPointOneFingerCenter : lastScreenPointTwoFingersCenter;
        }

        private void OnFingerUp(LeanFinger obj)
        {
            isFingerUp = true;
        }

        private void OnFingerTap(LeanFinger obj)
        {
            isFingerTap = true;
        }

        void LateUpdate()
        {
            isFingerTap = false;
            isFingerUp = false;
        }
    }
}