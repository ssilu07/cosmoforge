// ============================================================================
// CosmoForge: Slingshot Placement & Launch Controller (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F25
// ============================================================================

namespace CosmoForge.UI.Workbench
{
    using System;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Core.Performance;

    /// <summary>
    /// Implements tactile slingshot body placement:
    /// Click/tap to position new celestial body, drag to define initial velocity vector,
    /// and release to launch into the gravitational simulation.
    /// </summary>
    public class SlingshotController : MonoBehaviour
    {
        [Header("Slingshot Configuration")]
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private float velocityScale = 0.15f;
        [SerializeField] private float maxDragDistance = 50f;

        [Header("Target Body Preset")]
        [SerializeField] private BodyType pendingBodyType = BodyType.Terrestrial;
        [SerializeField] private double pendingMass = 1.0;
        [SerializeField] private double pendingRadius = 1.0;

        private bool _isDragging = false;
        private Vector3 _startWorldPos;
        private Vector3 _currentWorldPos;

        public event Action<CelestialBodyState> OnBodyLaunched;

        public bool IsDragging => _isDragging;

        public void SetPendingBody(BodyType type, double mass, double radius)
        {
            pendingBodyType = type;
            pendingMass = mass;
            pendingRadius = radius;
        }

        public void BeginDrag(Vector3 worldPos)
        {
            _isDragging = true;
            _startWorldPos = worldPos;
            _currentWorldPos = worldPos;

            if (aimLine != null)
            {
                aimLine.enabled = true;
                aimLine.positionCount = 2;
                aimLine.SetPosition(0, _startWorldPos);
                aimLine.SetPosition(1, _startWorldPos);
            }
        }

        public void UpdateDrag(Vector3 worldPos)
        {
            if (!_isDragging) return;

            Vector3 dragDelta = worldPos - _startWorldPos;
            if (dragDelta.magnitude > maxDragDistance)
            {
                dragDelta = dragDelta.normalized * maxDragDistance;
            }
            _currentWorldPos = _startWorldPos + dragDelta;

            if (aimLine != null)
            {
                aimLine.SetPosition(0, _startWorldPos);
                aimLine.SetPosition(1, _currentWorldPos);
            }
        }

        public CelestialBodyState ReleaseAndLaunch()
        {
            if (!_isDragging) return null;
            _isDragging = false;

            if (aimLine != null)
            {
                aimLine.enabled = false;
            }

            // Slingshot vector: points in opposite direction of pull
            Vector3 pullVector = _startWorldPos - _currentWorldPos;
            Vector3 launchVelocity = pullVector * velocityScale;

            Vector3d absolutePos = (_startWorldPos).ToVector3d() + FloatingOrigin.CurrentOrigin;
            Vector3d velocity = launchVelocity.ToVector3d();

            int id = (int)(DateTime.UtcNow.Ticks % 1000000);
            var launchedBody = new CelestialBodyState(id, $"{pendingBodyType}_{id}", pendingBodyType, pendingMass, pendingRadius, absolutePos, velocity)
            {
                SurfaceTemperature = 288.0,
                CoreFuel = pendingBodyType == BodyType.Star ? 1.0 : 0.0
            };

            OnBodyLaunched?.Invoke(launchedBody);
            return launchedBody;
        }
    }
}
