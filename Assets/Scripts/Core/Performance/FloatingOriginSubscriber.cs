// ============================================================================
// CosmoForge: Floating Origin Vertex, Particle & Transform Subscriber
// Conforms to PROJECT.md § Architecture & Interface Contracts
// Remediates BUG-02 & PERF-01 for Milestone 1 Iteration 2
// ============================================================================

namespace CosmoForge.Core.Performance
{
    using UnityEngine;
    using CosmoForge.Core.Math;

    /// <summary>
    /// Subscribes to FloatingOrigin shifts and translates the host Transform,
    /// world-space particle systems, and world-space line renderers to prevent
    /// visual discontinuities and positional drift across origin shifts.
    /// </summary>
    public class FloatingOriginSubscriber : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("If true, offsets this GameObject's world transform by -shift when an origin shift occurs.")]
        [SerializeField] private bool shiftTransform = true;

        private ParticleSystem _particleSystem;
        private LineRenderer _lineRenderer;
        private ParticleSystem.Particle[] _particles;
        private Vector3[] _linePoints;

        /// <summary>
        /// Gets or sets whether this subscriber translates the host transform on origin shifts.
        /// </summary>
        public bool ShiftTransform
        {
            get => shiftTransform;
            set => shiftTransform = value;
        }

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void OnEnable()
        {
            FloatingOrigin.OnOriginShift += HandleOriginShift;
        }

        private void OnDisable()
        {
            FloatingOrigin.OnOriginShift -= HandleOriginShift;
        }

        private void HandleOriginShift(Vector3d offset)
        {
            Vector3 shift = offset.ToVector3();

            // 1. Offset Host Transform
            // If the host has a parent with an ancestor FloatingOriginSubscriber,
            // the parent's shift already translates this child transform in world space.
            if (shiftTransform && (transform.parent == null || transform.parent.GetComponentInParent<FloatingOriginSubscriber>() == null))
            {
                transform.position -= shift;
            }

            // 2. Adjust World-Space Particle Systems
            if (_particleSystem != null && _particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                int count = _particleSystem.particleCount;
                if (_particles == null || _particles.Length < count)
                {
                    _particles = new ParticleSystem.Particle[count + 64];
                }

                int activeCount = _particleSystem.GetParticles(_particles);
                for (int i = 0; i < activeCount; i++)
                {
                    _particles[i].position -= shift;
                }
                _particleSystem.SetParticles(_particles, activeCount);
            }

            // 3. Adjust World-Space LineRenderers (e.g. Orbit paths, ghost trajectories)
            if (_lineRenderer != null && _lineRenderer.useWorldSpace)
            {
                int pointCount = _lineRenderer.positionCount;
                if (pointCount > 0)
                {
                    // Reusable buffer to eliminate per-shift heap allocations (PERF-01)
                    if (_linePoints == null || _linePoints.Length != pointCount)
                    {
                        _linePoints = new Vector3[pointCount];
                    }

                    _lineRenderer.GetPositions(_linePoints);
                    for (int i = 0; i < pointCount; i++)
                    {
                        _linePoints[i] -= shift;
                    }
                    _lineRenderer.SetPositions(_linePoints);
                }
            }
        }
    }
}
