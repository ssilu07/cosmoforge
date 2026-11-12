// ============================================================================
// CosmoForge: Stellar Supernova & Cataclysm Engine
// Conforms to PROJECT.md § Architecture & Requirement F15
// ============================================================================

namespace CosmoForge.Physics.Cataclysms
{
    using System;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Manages supernova detonations, progenitor disruption, remnant creation
    /// (Neutron Star / Black Hole), thermal shockwave expansion, and spacetime ripples.
    /// </summary>
    public class SupernovaEngine : MonoBehaviour
    {
        [Header("Remnant Classification")]
        [Tooltip("Solar mass normalization.")]
        [SerializeField] private double solarMass = 1000.0;

        [Header("VFX & Visuals")]
        [SerializeField] private ParticleSystem blastParticleSystem;

        public event Action<CelestialBodyState, CelestialBodyState> OnRemnantCreated; // (progenitor, remnant)

        /// <summary>
        /// Detonates a supernova for the given progenitor star.
        /// </summary>
        public CelestialBodyState DetonateSupernova(CelestialBodyState progenitor, bool isTypeIa)
        {
            if (progenitor == null || progenitor.IsDestroyed) return null;

            progenitor.IsDestroyed = true;

            // Trigger blast particles
            if (blastParticleSystem != null)
            {
                blastParticleSystem.transform.position = progenitor.Position.ToVector3();
                blastParticleSystem.Play();
            }

            if (isTypeIa)
            {
                // Type Ia: complete thermonuclear destruction, no remnant
                return null;
            }
            else
            {
                // Type II Core-Collapse: forms Neutron Star / Pulsar or Black Hole
                double solarMassRatio = progenitor.Mass / solarMass;
                BodyType remnantType;
                double remnantMass;
                double remnantRadius;
                string remnantName;

                if (solarMassRatio >= 20.0)
                {
                    remnantType = BodyType.BlackHole;
                    remnantMass = progenitor.Mass * 0.15;
                    remnantRadius = 2.0;
                    remnantName = $"BH_Remnant_{progenitor.Id}";
                }
                else
                {
                    remnantType = BodyType.Pulsar;
                    remnantMass = Math.Min(2.1 * solarMass, Math.Max(1.4 * solarMass, progenitor.Mass * 0.1));
                    remnantRadius = 0.8;
                    remnantName = $"Pulsar_Remnant_{progenitor.Id}";
                }

                var remnant = new CelestialBodyState(
                    id: progenitor.Id + 10000,
                    name: remnantName,
                    type: remnantType,
                    mass: remnantMass,
                    radius: remnantRadius,
                    position: progenitor.Position,
                    velocity: progenitor.Velocity
                )
                {
                    SurfaceTemperature = remnantType == BodyType.Pulsar ? 1e6 : 10.0,
                    CoreFuel = 0.0
                };

                OnRemnantCreated?.Invoke(progenitor, remnant);
                return remnant;
            }
        }
    }
}
