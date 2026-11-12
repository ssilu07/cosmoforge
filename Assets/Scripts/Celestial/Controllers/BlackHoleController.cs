// ============================================================================
// CosmoForge: Supermassive Black Hole & Relativistic Lensing Controller
// Conforms to PROJECT.md § Architecture & Requirement F11
// ============================================================================

namespace CosmoForge.Celestial.Controllers
{
    using System;
    using UnityEngine;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Controls relativistic visual effects of a Supermassive Black Hole:
    /// Schwarzschild radius Rs = 2GM/c^2, photon sphere, Einstein ring lensing,
    /// and Doppler-beamed accretion disk.
    /// </summary>
    public class BlackHoleController : MonoBehaviour
    {
        [Header("Physical Constants")]
        [SerializeField] private double G = 1.0;
        [SerializeField] private double c = 50.0;

        [Header("Visual Elements")]
        [SerializeField] private Transform eventHorizonMesh;
        [SerializeField] private Transform accretionDiskMesh;
        [SerializeField] private Material lensingMaterial;
        [SerializeField] private Material accretionDiskMaterial;

        [Header("Accretion Disk Dynamics")]
        [SerializeField] private float diskSpinSpeed = 45f; // degrees/sec
        [SerializeField] private float dopplerBeamingFactor = 1.8f;

        private static readonly int SchwarzschildRadiusProp = Shader.PropertyToID("_SchwarzschildRadius");
        private static readonly int DopplerFactorProp = Shader.PropertyToID("_DopplerFactor");

        public double CalculateSchwarzschildRadius(double mass)
        {
            double cSq = c * c;
            return (2.0 * G * mass) / cSq;
        }

        public double CalculatePhotonSphereRadius(double mass)
        {
            return 1.5 * CalculateSchwarzschildRadius(mass);
        }

        public double CalculateISCORadius(double mass)
        {
            return 3.0 * CalculateSchwarzschildRadius(mass);
        }

        public void UpdateBlackHole(CelestialBodyState state)
        {
            if (state == null) return;

            double rs = CalculateSchwarzschildRadius(state.Mass);
            state.Radius = rs;

            // Scale event horizon sphere
            if (eventHorizonMesh != null)
            {
                float visualRs = (float)Math.Max(0.2, rs);
                eventHorizonMesh.localScale = new Vector3(visualRs * 2f, visualRs * 2f, visualRs * 2f);
            }

            // Scale accretion disk from ISCO (3 Rs) to ~8 Rs
            if (accretionDiskMesh != null)
            {
                float diskRadius = (float)(rs * 8.0);
                accretionDiskMesh.localScale = new Vector3(diskRadius * 2f, 0.1f, diskRadius * 2f);
            }

            if (lensingMaterial != null)
            {
                lensingMaterial.SetFloat(SchwarzschildRadiusProp, (float)rs);
            }

            if (accretionDiskMaterial != null)
            {
                accretionDiskMaterial.SetFloat(DopplerFactorProp, dopplerBeamingFactor);
            }
        }

        private void Update()
        {
            if (accretionDiskMesh != null)
            {
                accretionDiskMesh.Rotate(Vector3.up, diskSpinSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
