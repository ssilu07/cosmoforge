// ============================================================================
// CosmoForge: Stellar Fusion Lifecycle Engine (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F09
// ============================================================================

namespace CosmoForge.Celestial.Controllers
{
    using System;
    using UnityEngine;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Tracks stellar core hydrogen fusion lifecycle, fuel exhaustion,
    /// mass-luminosity relation, and triggers supernovae when fuel exhausts
    /// or when mass exceeds the Chandrasekhar limit (1.44 Solar Masses).
    /// </summary>
    public class StellarLifecycle : MonoBehaviour
    {
        [Header("Stellar Configuration")]
        [Tooltip("Solar mass normalization unit in simulation mass units.")]
        [SerializeField] private double solarMass = 1000.0;

        [Tooltip("Chandrasekhar limit in solar masses (1.44 M_sun).")]
        [SerializeField] private double chandrasekharLimitMultiplier = 1.44;

        [Tooltip("Base nuclear fuel burn rate per second at 1 Solar Mass.")]
        [SerializeField] private double baseBurnRate = 0.001;

        public event Action<CelestialBodyState, bool> OnSupernovaTriggered; // (state, isThermonuclear)

        public double ChandrasekharMassLimit => solarMass * chandrasekharLimitMultiplier;

        /// <summary>
        /// Updates stellar fusion life bar and triggers supernova if fuel is exhausted
        /// or if mass exceeds the Chandrasekhar limit.
        /// </summary>
        public void UpdateLifecycle(CelestialBodyState starState, double deltaTime)
        {
            if (starState == null || starState.Type != BodyType.Star || starState.IsDestroyed) return;

            // 1. Check Chandrasekhar limit for white dwarf / accretion runaway
            if (starState.Mass >= ChandrasekharMassLimit)
            {
                OnSupernovaTriggered?.Invoke(starState, true);
                return;
            }

            // 2. Consume core fusion fuel: burn rate scales with M^2.5
            double massRatio = Math.Max(0.1, starState.Mass / solarMass);
            double burnRate = baseBurnRate * Math.Pow(massRatio, 2.5);
            starState.CoreFuel = Math.Max(0.0, starState.CoreFuel - (burnRate * deltaTime));

            // 3. Core collapse supernova upon fuel exhaustion
            if (starState.CoreFuel <= 0.0)
            {
                OnSupernovaTriggered?.Invoke(starState, false);
            }
        }
    }
}
