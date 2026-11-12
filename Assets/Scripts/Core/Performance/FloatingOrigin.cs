// ============================================================================
// CosmoForge: Floating Origin Camera & Transform Translation System
// Conforms to PROJECT.md § Architecture & Interface Contracts
// ============================================================================

namespace CosmoForge.Core.Performance
{
    using System;
    using UnityEngine;
    using CosmoForge.Core.Math;

    /// <summary>
    /// Manages coordinate origin shifting to eliminate single-precision floating-point jitter
    /// when rendering distant celestial bodies in Unity 3D.
    /// Physics simulation coordinates remain strictly in absolute Vector3d double-precision space.
    /// </summary>
    public class FloatingOrigin : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Distance from Unity (0,0,0) before shifting the origin back to zero.")]
        [SerializeField] private float shiftThreshold = 2000f;

        [Tooltip("Target camera or focus transform to monitor.")]
        [SerializeField] private Transform targetCameraTransform;

        /// <summary>
        /// Current accumulated origin offset in absolute double-precision coordinates.
        /// </summary>
        public static Vector3d CurrentOrigin { get; private set; } = Vector3d.zero;

        /// <summary>
        /// Global event fired when an origin shift occurs.
        /// Receives the delta offset (in Vector3d) by which the world was shifted.
        /// </summary>
        public static event Action<Vector3d> OnOriginShift;

        private float _sqrThreshold;

        /// <summary>
        /// Public accessor for the shift threshold distance.
        /// </summary>
        public float ShiftThreshold
        {
            get => shiftThreshold;
            set
            {
                shiftThreshold = value;
                _sqrThreshold = value * value;
            }
        }

        /// <summary>
        /// Public accessor for the target camera transform.
        /// </summary>
        public Transform TargetCameraTransform
        {
            get => targetCameraTransform;
            set => targetCameraTransform = value;
        }

        private void Awake()
        {
            _sqrThreshold = shiftThreshold * shiftThreshold;
            if (targetCameraTransform == null && Camera.main != null)
            {
                targetCameraTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (targetCameraTransform == null)
            {
                if (Camera.main != null)
                {
                    targetCameraTransform = Camera.main.transform;
                }
                else
                {
                    return;
                }
            }

            if (_sqrThreshold <= 0f)
            {
                _sqrThreshold = shiftThreshold * shiftThreshold;
            }

            Vector3 camPos = targetCameraTransform.position;
            if (camPos.sqrMagnitude > _sqrThreshold)
            {
                Vector3d delta = new Vector3d(camPos.x, camPos.y, camPos.z);
                ShiftOrigin(delta);

                // Offset camera position by -delta to keep render position near (0,0,0)
                // and prevent infinite runaway shift loops on consecutive frames.
                if (targetCameraTransform.position == camPos)
                {
                    targetCameraTransform.position -= delta.ToVector3();
                }
            }
        }

        /// <summary>
        /// Shifts the floating origin by the specified delta offset.
        /// </summary>
        public static void ShiftOrigin(Vector3d deltaOffset)
        {
            CurrentOrigin += deltaOffset;
            OnOriginShift?.Invoke(deltaOffset);
        }

        /// <summary>
        /// Converts an absolute physics position to Unity render space.
        /// </summary>
        public static Vector3 AbsoluteToRender(Vector3d absolutePosition)
        {
            return (absolutePosition - CurrentOrigin).ToVector3();
        }

        /// <summary>
        /// Converts a Unity render space position back to absolute physics space.
        /// </summary>
        public static Vector3d RenderToAbsolute(Vector3 renderPosition)
        {
            return CurrentOrigin + Vector3d.FromVector3(renderPosition);
        }

        /// <summary>
        /// Resets the origin offset back to absolute zero (useful for scene restarts and unit tests).
        /// </summary>
        public static void ResetOrigin()
        {
            Vector3d delta = -CurrentOrigin;
            CurrentOrigin = Vector3d.zero;
            OnOriginShift?.Invoke(delta);
        }
    }
}
