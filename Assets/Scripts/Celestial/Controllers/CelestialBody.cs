// ============================================================================
// CosmoForge: Celestial Body 3D Controller (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F07
// ============================================================================

namespace CosmoForge.Celestial.Controllers
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Controls the 3D visual representation, thermal equilibrium calculations,
    /// and orbital trail of a celestial body in the Unity scene.
    /// </summary>
    public class CelestialBody : MonoBehaviour
    {
        [Header("State Binding")]
        [SerializeField] private CelestialBodyState state;

        [Header("Rendering Components")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private Material bodyMaterial;

        [Header("Atmosphere & Thermodynamics")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float albedo = 0.30f; // Earth default albedo
        [SerializeField] private double solarLuminosityWatts = 3.828e26;
        [SerializeField] private Color atmosphereColor = new Color(0.2f, 0.5f, 1.0f, 0.8f);

        private const double StefanBoltzmannConstant = 5.670374419e-8;
        private static readonly int SurfaceTempProp = Shader.PropertyToID("_SurfaceTemperature");
        private static readonly int AtmosphereColorProp = Shader.PropertyToID("_AtmosphereColor");

        public CelestialBodyState State
        {
            get => state;
            set
            {
                state = value;
                SyncTransform();
            }
        }

        private void Awake()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (state == null)
            {
                state = new CelestialBodyState(GetHashCode(), name, BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, Vector3d.zero);
            }
        }

        private void Start()
        {
            SyncTransform();
        }

        public void Initialize(CelestialBodyState bodyState)
        {
            state = bodyState;
            name = bodyState.Name;
            SyncTransform();
        }

        public void SyncTransform()
        {
            if (state == null) return;

            Vector3d localPos = state.Position - FloatingOrigin.CurrentOrigin;
            transform.position = localPos.ToVector3();

            float visualRadius = (float)Math.Max(0.1, state.Radius);
            transform.localScale = new Vector3(visualRadius * 2f, visualRadius * 2f, visualRadius * 2f);
        }

        /// <summary>
        /// Calculates surface temperature via blackbody radiation equilibrium
        /// summing radiant flux across all active host stars in the system:
        /// T = [ sum_i (L_i * (1 - A) / (16 * pi * sigma * d_i^2)) ]^(1/4)
        /// </summary>
        public double UpdateThermodynamics(IList<CelestialBodyState> allBodies)
        {
            if (state == null || state.Type == BodyType.Star || state.Type == BodyType.Pulsar || state.Type == BodyType.BlackHole)
            {
                return state != null ? state.SurfaceTemperature : 0.0;
            }

            double totalFlux = 0.0;
            if (allBodies != null)
            {
                for (int i = 0; i < allBodies.Count; i++)
                {
                    var other = allBodies[i];
                    if (other == null || other.IsDestroyed || (other.Type != BodyType.Star && other.Type != BodyType.Pulsar))
                    {
                        continue;
                    }

                    double d = (state.Position - other.Position).magnitude;
                    if (d < 1e-3) d = 1e-3;

                    // Stellar luminosity scaling: L proportional to M^3.5
                    double lumScale = Math.Pow(Math.Max(0.1, other.Mass / 1000.0), 3.5);
                    double starLuminosity = solarLuminosityWatts * lumScale;

                    double flux = (starLuminosity * (1.0 - albedo)) / (16.0 * Math.PI * StefanBoltzmannConstant * d * d);
                    totalFlux += flux;
                }
            }

            // Minimum deep-space cosmic background temperature is 2.725 K
            double temp = totalFlux > 0.0 ? Math.Pow(totalFlux, 0.25) : 2.725;
            state.SurfaceTemperature = temp;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.SetFloat(SurfaceTempProp, (float)temp);
                meshRenderer.material.SetColor(AtmosphereColorProp, atmosphereColor);
            }

            return temp;
        }

        public void ClearTrail()
        {
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
    }
}
