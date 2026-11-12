// ============================================================================
// CosmoForge: Trajectory & Orbit Conic Visualizer (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F05
// ============================================================================

namespace CosmoForge.Physics.Trajectory
{
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Renders dynamic forward ghost trajectories and osculating conic sections
    /// with periapsis/apoapsis indicators in Unity 3D.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryRenderer : MonoBehaviour
    {
        [Header("Prediction Settings")]
        [Tooltip("Number of forward integration steps to compute (up to 500).")]
        [Range(50, 500)]
        [SerializeField] private int predictionSteps = 300;

        [Tooltip("Time delta per prediction step in seconds.")]
        [SerializeField] private double stepDeltaTime = 0.05;

        [Tooltip("Softening factor for prediction.")]
        [SerializeField] private double softening = 0.05;

        [Header("Visual Markers")]
        [Tooltip("Optional prefab or marker GameObject for Periapsis (closest approach).")]
        [SerializeField] private GameObject periapsisMarker;

        [Tooltip("Optional prefab or marker GameObject for Apoapsis (farthest point).")]
        [SerializeField] private GameObject apoapsisMarker;

        private LineRenderer _lineRenderer;
        private ITrajectoryPredictor _predictor;
        private Vector3[] _linePositions;

        public ITrajectoryPredictor Predictor
        {
            get => _predictor;
            set => _predictor = value;
        }

        public int PredictionSteps
        {
            get => predictionSteps;
            set => predictionSteps = Mathf.Clamp(value, 10, 500);
        }

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _predictor = new KeplerianPredictor();
            _lineRenderer.useWorldSpace = true;

            // Configure default glowing line visuals if not set
            if (_lineRenderer.startWidth <= 0.01f && _lineRenderer.endWidth <= 0.01f)
            {
                _lineRenderer.startWidth = 0.4f;
                _lineRenderer.endWidth = 0.05f;
            }
        }

        /// <summary>
        /// Updates the trajectory line and periapsis/apoapsis markers for the specified body.
        /// </summary>
        public void UpdateTrajectory(
            CelestialBodyState target,
            List<CelestialBodyState> allBodies,
            double G,
            CelestialBodyState primaryAttractor = null)
        {
            if (target == null || allBodies == null || _lineRenderer == null)
            {
                if (_lineRenderer != null) _lineRenderer.positionCount = 0;
                SetMarkerActive(periapsisMarker, false);
                SetMarkerActive(apoapsisMarker, false);
                return;
            }

            Vector3d[] predictedPoints = _predictor.PredictForwardGhostTrajectory(
                target,
                allBodies,
                predictionSteps,
                stepDeltaTime,
                G,
                softening
            );

            if (predictedPoints == null || predictedPoints.Length == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            if (_linePositions == null || _linePositions.Length != predictedPoints.Length)
            {
                _linePositions = new Vector3[predictedPoints.Length];
            }

            Vector3d currentOrigin = FloatingOrigin.CurrentOrigin;
            double minDistSqr = double.MaxValue;
            double maxDistSqr = double.MinValue;
            Vector3 periPos = Vector3.zero;
            Vector3 apoPos = Vector3.zero;
            Vector3d attractorPos = primaryAttractor != null ? primaryAttractor.Position : Vector3d.zero;

            for (int i = 0; i < predictedPoints.Length; i++)
            {
                Vector3d localDouble = predictedPoints[i] - currentOrigin;
                _linePositions[i] = localDouble.ToVector3();

                if (primaryAttractor != null)
                {
                    double dSqr = (predictedPoints[i] - attractorPos).sqrMagnitude;
                    if (dSqr < minDistSqr)
                    {
                        minDistSqr = dSqr;
                        periPos = _linePositions[i];
                    }
                    if (dSqr > maxDistSqr)
                    {
                        maxDistSqr = dSqr;
                        apoPos = _linePositions[i];
                    }
                }
            }

            _lineRenderer.positionCount = _linePositions.Length;
            _lineRenderer.SetPositions(_linePositions);

            // Update Periapsis & Apoapsis Marker positions
            if (primaryAttractor != null && periapsisMarker != null)
            {
                periapsisMarker.SetActive(true);
                periapsisMarker.transform.position = periPos;
            }
            if (primaryAttractor != null && apoapsisMarker != null)
            {
                apoapsisMarker.SetActive(true);
                apoapsisMarker.transform.position = apoPos;
            }
        }

        public void Clear()
        {
            if (_lineRenderer != null) _lineRenderer.positionCount = 0;
            SetMarkerActive(periapsisMarker, false);
            SetMarkerActive(apoapsisMarker, false);
        }

        private static void SetMarkerActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
