// ============================================================================
// CosmoForge: 2.5D / 3D Spacetime Curvature Grid Visualizer (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F06
// ============================================================================

namespace CosmoForge.Physics.Grid
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Renders and deforms a 2.5D/3D coordinate grid that dynamically bends and
    /// depresses downward in the gravity wells of massive bodies according to:
    /// z_warp(x, z) = - sum_i [ G * m_i / (dist_i + c^2) ]
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SpacetimeCurvatureGrid : MonoBehaviour, ISpacetimeGridVisualizer
    {
        [Header("Grid Geometry")]
        [Tooltip("Number of cells along X and Z axes.")]
        [Range(20, 100)]
        [SerializeField] private int resolution = 40;

        [Tooltip("Total world width of the spacetime grid.")]
        [SerializeField] private float gridExtent = 400f;

        [Tooltip("Visual vertical depression scale factor.")]
        [SerializeField] private float warpVisualScale = 1.0f;

        [Header("Astrophysical Parameters")]
        [SerializeField] private double gravitationalConstant = 1.0;
        [SerializeField] private double speedOfLight = 50.0;

        [Header("Appearance")]
        [SerializeField] private Material gridMaterial;

        private Mesh _gridMesh;
        private Vector3[] _baseVertices;
        private Vector3[] _deformedVertices;
        private MeshRenderer _meshRenderer;

        public double GravitationalConstant
        {
            get => gravitationalConstant;
            set => gravitationalConstant = value;
        }

        public double SpeedOfLight
        {
            get => speedOfLight;
            set => speedOfLight = value;
        }

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            InitializeMesh();
        }

        private void InitializeMesh()
        {
            _gridMesh = new Mesh { name = "SpacetimeCurvatureGridMesh" };
            _gridMesh.MarkDynamic();

            int vertCount = (resolution + 1) * (resolution + 1);
            _baseVertices = new Vector3[vertCount];
            _deformedVertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            float step = gridExtent / resolution;
            float halfExtent = gridExtent * 0.5f;

            int idx = 0;
            for (int z = 0; z <= resolution; z++)
            {
                float zPos = (z * step) - halfExtent;
                for (int x = 0; x <= resolution; x++)
                {
                    float xPos = (x * step) - halfExtent;
                    _baseVertices[idx] = new Vector3(xPos, 0f, zPos);
                    _deformedVertices[idx] = _baseVertices[idx];
                    uvs[idx] = new Vector2((float)x / resolution, (float)z / resolution);
                    idx++;
                }
            }

            // Create grid wireframe line indices
            List<int> lines = new List<int>();
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * (resolution + 1) + x;
                    lines.Add(i);
                    lines.Add(i + 1);
                }
            }

            for (int x = 0; x <= resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    int i = z * (resolution + 1) + x;
                    int iNext = (z + 1) * (resolution + 1) + x;
                    lines.Add(i);
                    lines.Add(iNext);
                }
            }

            _gridMesh.vertices = _baseVertices;
            _gridMesh.uv = uvs;
            _gridMesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = _gridMesh;
        }

        /// <summary>
        /// Analytical potential depression sampler conforming to test contract.
        /// </summary>
        public double CalculateWarpAt(double worldX, double worldZ, IList<CelestialBodyState> bodies)
        {
            if (bodies == null || bodies.Count == 0) return 0.0;

            double cSq = speedOfLight * speedOfLight;
            double totalDepression = 0.0;

            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == null || b.IsDestroyed) continue;

                double dx = worldX - b.Position.x;
                double dz = worldZ - b.Position.z;
                double dist = Math.Sqrt(dx * dx + dz * dz);
                totalDepression -= (gravitationalConstant * b.Mass) / (dist + cSq);
            }

            return totalDepression;
        }

        /// <summary>
        /// Updates grid vertex heights based on current celestial body states.
        /// </summary>
        public void UpdateGrid(IList<CelestialBodyState> bodies, double G, double c)
        {
            gravitationalConstant = G;
            speedOfLight = c;

            if (_gridMesh == null || _baseVertices == null) return;

            Vector3d origin = FloatingOrigin.CurrentOrigin;
            double cSq = speedOfLight * speedOfLight;

            for (int i = 0; i < _baseVertices.Length; i++)
            {
                Vector3 basePos = _baseVertices[i];
                double worldX = basePos.x + origin.x;
                double worldZ = basePos.z + origin.z;

                double warp = 0.0;
                if (bodies != null)
                {
                    for (int b = 0; b < bodies.Count; b++)
                    {
                        var body = bodies[b];
                        if (body == null || body.IsDestroyed) continue;

                        double dx = worldX - body.Position.x;
                        double dz = worldZ - body.Position.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);
                        warp -= (gravitationalConstant * body.Mass) / (dist + cSq);
                    }
                }

                _deformedVertices[i] = new Vector3(basePos.x, (float)(warp * warpVisualScale), basePos.z);
            }

            _gridMesh.vertices = _deformedVertices;
            _gridMesh.RecalculateBounds();
        }

        public void SetVisible(bool visible)
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = visible;
            }
        }
    }
}
