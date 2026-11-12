// ============================================================================
// CosmoForge: Android Tactile Touch Gesture Handler (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F27
// ============================================================================

namespace CosmoForge.UI.Input
{
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Processes multi-touch gestures on Android (pan, log-scale pinch zoom, twist)
    /// and mouse fallbacks in editor, plus single-tap body selection and double-tap focus tracking.
    /// </summary>
    public class TouchGestureHandler : MonoBehaviour, ITouchInputHandler
    {
        [Header("Camera Control")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float panSensitivity = 0.05f;
        [SerializeField] private float zoomSensitivity = 0.02f;
        [SerializeField] private float minDistance = 5.0f;
        [SerializeField] private float maxDistance = 2000.0f;

        [Header("Selection Settings")]
        [SerializeField] private double maxSelectionDistance = 25.0;

        public TouchGestureType LastGesture { get; private set; } = TouchGestureType.None;
        public Vector3 CameraPosition { get; set; } = new Vector3(0f, 100f, 0f);
        public float CameraDistance { get; set; } = 100f;
        public float CameraYaw { get; set; } = 0f;
        public int FocusedBodyId { get; set; } = -1;
        public bool IsTrackingFocus { get; set; } = false;

        private Vector2 _lastMousePos;
        private float _lastPinchDist;
        private float _lastTapTime;
        private const float DoubleTapThreshold = 0.35f;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                CameraPosition = targetCamera.transform.position;
                CameraDistance = targetCamera.transform.position.y;
            }
        }

        public void ProcessPan(Vector2 deltaScreen, float sensitivity)
        {
            CameraPosition += new Vector3(deltaScreen.x * sensitivity, 0f, deltaScreen.y * sensitivity);
            LastGesture = TouchGestureType.Pan;
            IsTrackingFocus = false;
        }

        public void ProcessPinch(float initialDistance, float currentDistance, float minZoom, float maxZoom)
        {
            float safeCurrent = Mathf.Max(1e-5f, currentDistance);
            float ratio = initialDistance / safeCurrent;
            CameraDistance = Mathf.Clamp(CameraDistance * ratio, minZoom, maxZoom);
            LastGesture = TouchGestureType.PinchZoom;
        }

        public void ProcessTwist(float angleDeltaDegrees)
        {
            CameraYaw += angleDeltaDegrees;
            LastGesture = TouchGestureType.Twist;
        }

        public int ProcessSingleTap(Vector3 tapWorldPos, IList<CelestialBodyState> bodies, double maxDist)
        {
            LastGesture = TouchGestureType.SingleTap;
            int bestId = -1;
            double minDist = maxDist;

            if (bodies != null)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    var b = bodies[i];
                    if (b == null || b.IsDestroyed) continue;
                    double d = (b.Position.ToVector3() - tapWorldPos).magnitude;
                    if (d < minDist)
                    {
                        minDist = d;
                        bestId = b.Id;
                    }
                }
            }

            FocusedBodyId = bestId;
            return bestId;
        }

        public void ProcessDoubleTap(int bodyId)
        {
            LastGesture = TouchGestureType.DoubleTap;
            FocusedBodyId = bodyId;
            IsTrackingFocus = true;
        }

        private void Update()
        {
            HandleInput();
            ApplyCameraTransform();
        }

        private void HandleInput()
        {
            // 1. Android Touch Input
            if (UnityEngine.Input.touchCount == 1)
            {
                Touch t = UnityEngine.Input.GetTouch(0);
                if (t.phase == TouchPhase.Moved)
                {
                    ProcessPan(-t.deltaPosition, panSensitivity);
                }
                else if (t.phase == TouchPhase.Began)
                {
                    Ray ray = targetCamera != null ? targetCamera.ScreenPointToRay(t.position) : new Ray();
                    ProcessSingleTap(ray.origin, null, maxSelectionDistance);
                }
                else if (t.phase == TouchPhase.Ended)
                {
                    if (Time.time - _lastTapTime < DoubleTapThreshold)
                    {
                        ProcessDoubleTap(FocusedBodyId);
                    }
                    else
                    {
                        _lastTapTime = Time.time;
                    }
                }
            }
            else if (UnityEngine.Input.touchCount >= 2)
            {
                Touch t0 = UnityEngine.Input.GetTouch(0);
                Touch t1 = UnityEngine.Input.GetTouch(1);

                float currentDist = Vector2.Distance(t0.position, t1.position);
                if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
                {
                    _lastPinchDist = currentDist;
                }
                else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
                {
                    float adjustedCurrent = Mathf.Lerp(_lastPinchDist, currentDist, 1.0f + zoomSensitivity);
                    ProcessPinch(_lastPinchDist, adjustedCurrent, minDistance, maxDistance);
                    _lastPinchDist = currentDist;
                }
            }
            // 2. Mouse Drag & Scrollwheel Fallback for Editor / PC
            else if (UnityEngine.Input.GetMouseButton(0))
            {
                Vector2 mouseDelta = (Vector2)UnityEngine.Input.mousePosition - _lastMousePos;
                if (mouseDelta.sqrMagnitude > 1f)
                {
                    ProcessPan(-mouseDelta, panSensitivity);
                }
            }

            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                CameraDistance = Mathf.Clamp(CameraDistance * (1f - scroll * 1.5f), minDistance, maxDistance);
            }

            _lastMousePos = UnityEngine.Input.mousePosition;
        }

        private void ApplyCameraTransform()
        {
            if (targetCamera == null) return;

            targetCamera.transform.position = new Vector3(CameraPosition.x, CameraDistance, CameraPosition.z);
            targetCamera.transform.rotation = Quaternion.Euler(90f, CameraYaw, 0f); // Top-down cosmic view
        }
    }
}
