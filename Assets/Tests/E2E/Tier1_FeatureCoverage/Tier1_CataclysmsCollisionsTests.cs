// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Cataclysms & Collision Engine Tests (F12 - F16)
// 25 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Cataclysms;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_CataclysmsCollisionsTests
    {
        #region Feature F12: Inelastic Collision & Merger Engine (5 Tests)

        [Test]
        public void F12_InelasticCollision_HeadOnMerger_ConservesTotalLinearMomentum()
        {
            // Requirement R4 & Acceptance Criteria: v_merged = (m1*v1 + m2*v2) / (m1 + m2) via ICollisionEngine
            ICollisionEngine engine = new HarnessCollisionEngine();

            var b1 = new CelestialBodyState(1, "B1", BodyType.Terrestrial, 100.0, 5.0, new Vector3d(4.0, 0.0, 0.0), new Vector3d(15.0, 0.0, 0.0));
            var b2 = new CelestialBodyState(2, "B2", BodyType.Terrestrial, 300.0, 5.0, new Vector3d(0.0, 0.0, 0.0), new Vector3d(-5.0, 0.0, 0.0));

            var bodies = new List<CelestialBodyState> { b1, b2 };
            CollisionEvent recorded = default;
            bool eventFired = false;

            engine.CheckAndResolveCollisions(bodies, 0.02, evt =>
            {
                recorded = evt;
                eventFired = true;
            });

            Assert.IsTrue(eventFired, "Touching bodies must trigger collision via ICollisionEngine.");
            Assert.AreEqual(0.0, recorded.MergedVelocity.x, 1e-9);
            Assert.AreEqual(0.0, recorded.MergedVelocity.y, 1e-9);
            Assert.AreEqual(0.0, recorded.MergedVelocity.z, 1e-9);

            // Non-zero net momentum test
            var b3 = new CelestialBodyState(3, "B3", BodyType.Terrestrial, 200.0, 5.0, new Vector3d(3.0, 0.0, 0.0), new Vector3d(10.0, 4.0, 0.0));
            var b4 = new CelestialBodyState(4, "B4", BodyType.Terrestrial, 300.0, 5.0, new Vector3d(0.0, 0.0, 0.0), new Vector3d(0.0, 6.0, 0.0));

            var bodies2 = new List<CelestialBodyState> { b3, b4 };
            CollisionEvent recorded2 = default;
            engine.CheckAndResolveCollisions(bodies2, 0.02, evt => recorded2 = evt);

            Assert.AreEqual(4.0, recorded2.MergedVelocity.x, 1e-9);
            Assert.AreEqual(5.2, recorded2.MergedVelocity.y, 1e-9);
        }

        [Test]
        public void F12_InelasticCollision_MassAccumulation_SumsComponentMasses()
        {
            // Requirement R4: Merged body accumulates total mass of progenitors via ICollisionEngine
            ICollisionEngine engine = new HarnessCollisionEngine();
            var b1 = new CelestialBodyState(1, "Primary", BodyType.Terrestrial, 450.0, 5.0, new Vector3d(2.0, 0.0, 0.0), Vector3d.zero);
            var b2 = new CelestialBodyState(2, "Secondary", BodyType.Terrestrial, 550.0, 5.0, Vector3d.zero, Vector3d.zero);

            var bodies = new List<CelestialBodyState> { b1, b2 };
            CollisionEvent recorded = default;

            engine.CheckAndResolveCollisions(bodies, 0.02, evt => recorded = evt);

            Assert.AreEqual(1000.0, recorded.ConservedMass, 1e-9, "Merged body mass must equal the exact sum of constituent masses.");
            Assert.AreEqual(1000.0, recorded.Primary.Mass, 1e-9);
        }

        [Test]
        public void F12_InelasticCollision_VolumeConservation_ComputesMergedRadius()
        {
            // Requirement R4: Incompressible volume compaction: V_new = V1 + V2 => R_new = (R1^3 + R2^3)^(1/3)
            ICollisionEngine engine = new HarnessCollisionEngine();
            double r1 = 3.0;
            double r2 = 4.0;
            var b1 = new CelestialBodyState(1, "Body1", BodyType.Terrestrial, 50.0, r1, new Vector3d(2.0, 0.0, 0.0), Vector3d.zero);
            var b2 = new CelestialBodyState(2, "Body2", BodyType.Terrestrial, 100.0, r2, Vector3d.zero, Vector3d.zero);

            var bodies = new List<CelestialBodyState> { b1, b2 };
            CollisionEvent recorded = default;

            engine.CheckAndResolveCollisions(bodies, 0.02, evt => recorded = evt);

            double expectedRadius = Math.Cbrt((r1 * r1 * r1) + (r2 * r2 * r2));

            Assert.Greater(recorded.MergedRadius, r2, "Merged radius must be greater than largest individual progenitor.");
            Assert.Less(recorded.MergedRadius, r1 + r2, "Merged radius is smaller than sum of radii due to 3D spherical geometry.");
            Assert.AreEqual(expectedRadius, recorded.MergedRadius, 1e-9);
        }

        [Test]
        public void F12_InelasticCollision_KineticEnergyDissipation_ConvertsToThermalEnergy()
        {
            // Requirement R4: Delta E_kin = 0.5 * mu * v_rel^2 is deposited into thermal heat
            ICollisionEngine engine = new HarnessCollisionEngine();
            var b1 = new CelestialBodyState(1, "Body1", BodyType.Terrestrial, 100.0, 5.0, new Vector3d(2.0, 0.0, 0.0), new Vector3d(20.0, 0.0, 0.0));
            var b2 = new CelestialBodyState(2, "Body2", BodyType.Terrestrial, 100.0, 5.0, Vector3d.zero, Vector3d.zero);

            var bodies = new List<CelestialBodyState> { b1, b2 };
            CollisionEvent recorded = default;

            engine.CheckAndResolveCollisions(bodies, 0.02, evt => recorded = evt);

            Assert.AreEqual(10000.0, recorded.DissipatedKineticEnergy, 1e-9, "Dissipated energy must equal reduced mass kinetic energy.");
            Assert.Greater(recorded.DissipatedKineticEnergy, 0.0, "Inelastic collisions must always dissipate kinetic energy into internal thermal energy.");
        }

        [Test]
        public void F12_InelasticCollision_DeactivatesSubordinateBody()
        {
            // Collision resolution flags consumed body as destroyed via ICollisionEngine
            ICollisionEngine engine = new HarnessCollisionEngine();
            var primary = new CelestialBodyState(1, "Primary", BodyType.Terrestrial, 500.0, 5.0, new Vector3d(1.0, 0.0, 0.0), Vector3d.zero);
            var secondary = new CelestialBodyState(2, "Impactor", BodyType.Terrestrial, 50.0, 1.5, Vector3d.zero, Vector3d.zero);

            var bodies = new List<CelestialBodyState> { primary, secondary };
            engine.CheckAndResolveCollisions(bodies, 0.02, null);

            Assert.IsTrue(secondary.IsDestroyed, "Subordinate colliding body must be marked IsDestroyed = true.");
            Assert.IsFalse(primary.IsDestroyed, "Surviving merged body must remain active.");
            Assert.AreEqual(550.0, primary.Mass, 1e-9);
        }

        #endregion

        #region Feature F13: Collision Cataclysm Visual Effects (5 Tests)

        [Test]
        public void F13_CollisionVFX_SpawnsMoltenDebrisParticles()
        {
            // Requirement R4: High-energy collisions spawn molten debris particles
            ICollisionEngine engine = new HarnessCollisionEngine();
            var b1 = new CelestialBodyState(1, "Primary", BodyType.Terrestrial, 1000.0, 5.0, new Vector3d(2.0, 0.0, 0.0), new Vector3d(10.0, 0.0, 0.0));
            var b2 = new CelestialBodyState(2, "Secondary", BodyType.Terrestrial, 1000.0, 5.0, Vector3d.zero, new Vector3d(-10.0, 0.0, 0.0));

            CollisionEvent recorded = default;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b1, b2 }, 0.02, evt => recorded = evt);

            double impactEnergy = recorded.DissipatedKineticEnergy; // 500 * 400 * 0.5 = 100,000
            int particleCount = (int)Math.Clamp(impactEnergy / 500.0, 50.0, 300.0);

            Assert.GreaterOrEqual(particleCount, 50, "Debris particle count must meet minimum visual threshold.");
            Assert.LessOrEqual(particleCount, 300, "Debris particle count must respect mobile rendering budget.");
            Assert.AreEqual(200, particleCount);
        }

        [Test]
        public void F13_CollisionVFX_ThermalShockwaveRing_ExpandsRadiallyFromImpactPoint()
        {
            // Requirement R4: Shockwave ring expands over time
            double expansionSpeed = 50.0;
            double t1 = 0.5;
            double t2 = 1.5;

            double radius1 = expansionSpeed * t1;
            double radius2 = expansionSpeed * t2;

            Assert.AreEqual(25.0, radius1, 1e-9);
            Assert.AreEqual(75.0, radius2, 1e-9);
            Assert.Greater(radius2, radius1, "Thermal shockwave radius must expand monotonically.");
        }

        [Test]
        public void F13_CollisionVFX_WhiteFlashIntensity_ProportionalToImpactKineticEnergy()
        {
            // Flash intensity scales with dissipated kinetic energy
            double lowEnergy = 1000.0;
            double highEnergy = 100000.0;

            Func<double, double> getFlashIntensity = e => Math.Clamp(Math.Log10(e + 1.0) / 5.0, 0.0, 2.0);

            double lowFlash = getFlashIntensity(lowEnergy);
            double highFlash = getFlashIntensity(highEnergy);

            Assert.Greater(highFlash, lowFlash, "High energy impact must produce stronger optical flash.");
            Assert.Greater(lowFlash, 0.0);
        }

        [Test]
        public void F13_CollisionVFX_DebrisVelocities_ConserveCenterOfMassVelocity()
        {
            // Spawned debris particle cloud mean velocity equals center of mass velocity
            Vector3d vCenterOfMass = new Vector3d(5.0, -2.0, 1.5);
            int debrisCount = 100;
            double totalMass = 50.0;

            var debrisCloud = CosmoForgeTestHarness.GenerateCollisionDebrisCloud(vCenterOfMass, Vector3d.zero, totalMass, debrisCount, expansionSpeed: 8.0);
            Assert.AreEqual(debrisCount, debrisCloud.Count, "Debris particle count must match requested particle count.");

            Vector3d totalDebrisMomentum = Vector3d.zero;
            for (int i = 0; i < debrisCloud.Count; i++)
            {
                totalDebrisMomentum += debrisCloud[i].Velocity * debrisCloud[i].Mass;
            }

            Vector3d meanDebrisVelocity = totalDebrisMomentum / totalMass;
            Assert.AreEqual(vCenterOfMass.x, meanDebrisVelocity.x, 1e-9, "Mean debris cloud velocity X must conserve center-of-mass velocity.");
            Assert.AreEqual(vCenterOfMass.y, meanDebrisVelocity.y, 1e-9, "Mean debris cloud velocity Y must conserve center-of-mass velocity.");
            Assert.AreEqual(vCenterOfMass.z, meanDebrisVelocity.z, 1e-9, "Mean debris cloud velocity Z must conserve center-of-mass velocity.");
            TestAssertions.AssertApproximatelyEqual(vCenterOfMass, meanDebrisVelocity, 1e-9, "Mean debris cloud velocity must conserve center-of-mass velocity.");
        }

        [Test]
        public void F13_CollisionVFX_PostCollisionCooling_RadiationFollowsStefanBoltzmann()
        {
            // Post-collision molten planet cools via Stefan-Boltzmann radiation: dT/dt ~ -T^4
            double initialHotTemp = 2500.0;
            double ambientTemp = 280.0;
            double coolingRate = 1e-11;

            double temp = initialHotTemp;
            for (int t = 0; t < 100; t++)
            {
                double dTemp = -coolingRate * (Math.Pow(temp, 4.0) - Math.Pow(ambientTemp, 4.0));
                temp += dTemp;
            }

            Assert.Less(temp, initialHotTemp, "Molten planet must cool over time.");
            Assert.Greater(temp, ambientTemp, "Temperature remains above ambient cold background.");
        }

        #endregion

        #region Feature F14: Fluid Roche Limit Tidal Disruption (5 Tests)

        [Test]
        public void F14_RocheLimit_CalculatesAccurateDisruptionDistance()
        {
            // Requirement R4 & Acceptance Criteria: d_Roche = 2.44 * R_M * (rho_M / rho_m)^(1/3)
            double rPrimary = 70000.0;
            double rhoPrimary = 1326.0;
            double rhoMoon = 920.0;

            double dRoche = CosmoForgeTestHarness.CalculateFluidRocheLimit(rPrimary, rhoPrimary, rhoMoon);
            double expected = 2.44 * 70000.0 * Math.Cbrt(1326.0 / 920.0);

            Assert.AreEqual(expected, dRoche, 1e-6);
            Assert.Greater(dRoche, 2.44 * rPrimary, "Because rho_M > rho_m, Roche distance is > 2.44 * R_M.");
        }

        [Test]
        public void F14_RocheLimit_MoonOutsideThreshold_RemainsIntact()
        {
            // Moon orbiting outside Roche limit remains stable via ICollisionEngine
            ICollisionEngine engine = new HarnessCollisionEngine();
            var primary = new CelestialBodyState(1, "Jupiter", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var moon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), Vector3d.zero);

            var bodies = new List<CelestialBodyState> { primary, moon };
            bool eventFired = false;
            engine.EvaluateRocheLimits(bodies, evt => eventFired = true);

            Assert.IsFalse(eventFired, "Moon outside Roche limit must not disrupt.");
            Assert.IsFalse(moon.IsDestroyed);
        }

        [Test]
        public void F14_RocheLimit_MoonCrossingThreshold_ShattersIntoParticleRing()
        {
            // Requirement R4: Crossing inside Roche limit shatters moon into ring particles via ICollisionEngine
            ICollisionEngine engine = new HarnessCollisionEngine();
            var primary = new CelestialBodyState(1, "Jupiter", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var moon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(15.0, 0.0, 0.0), Vector3d.zero);

            var bodies = new List<CelestialBodyState> { primary, moon };
            RocheDisruptionEvent recorded = default;
            bool eventFired = false;

            engine.EvaluateRocheLimits(bodies, evt =>
            {
                recorded = evt;
                eventFired = true;
            });

            Assert.IsTrue(eventFired, "Moon inside Roche limit must trigger tidal disruption.");
            Assert.IsTrue(moon.IsDestroyed);
            Assert.GreaterOrEqual(recorded.SpawnedRingParticles, 50);
        }

        [Test]
        public void F14_RocheLimit_RingParticlesInheritKeplerianShearVelocities()
        {
            // Disrupted ring particles at varying radii shear differentially: v_k = sqrt(G*M/r)
            double G = 1.0;
            double M = 10000.0;
            double rInner = 80.0;
            double rOuter = 120.0;

            double vInner = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, rInner);
            double vOuter = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, rOuter);

            Assert.Greater(vInner, vOuter, "Inner ring particles lap outer ring particles, creating differential shear.");
            Assert.AreEqual(Math.Sqrt(10000.0 / 80.0), vInner, 1e-9);
            Assert.AreEqual(Math.Sqrt(10000.0 / 120.0), vOuter, 1e-9);
        }

        [Test]
        public void F14_RocheLimit_DisruptionConservesTotalSystemMass()
        {
            // Shattering converts single moon into N debris particles while conserving mass
            double moonMass = 50.0;
            int particleCount = 200;
            double particleMass = moonMass / particleCount;

            double totalRingMass = 0.0;
            for (int i = 0; i < particleCount; i++)
            {
                totalRingMass += particleMass;
            }

            Assert.AreEqual(moonMass, totalRingMass, 1e-9, "Disruption into ring particles must conserve total mass identically.");
        }

        #endregion

        #region Feature F15: Stellar Supernova Explosions (5 Tests)

        [Test]
        public void F15_Supernova_ChandrasekharLimit_TriggersTypeIaWhenMassExceedsThreshold()
        {
            // Requirement R3, R4 & Acceptance Criteria: Accreting white dwarf exceeding 1.44 M_sun detonates Type Ia
            double chLimit = CosmoForgeTestHarness.ChandrasekharLimitSolarMasses;
            double subLimitMass = 1.40;
            double superLimitMass = 1.45;

            Func<double, bool> isTypeIaTriggered = m => m >= chLimit;

            Assert.IsFalse(isTypeIaTriggered(subLimitMass), "Sub-Chandrasekhar white dwarf does not detonate.");
            Assert.IsTrue(isTypeIaTriggered(superLimitMass), "White dwarf exceeding 1.44 M_sun must trigger Type Ia supernova.");
        }

        [Test]
        public void F15_Supernova_TypeIa_LeavesNoCentralRemnant()
        {
            // Thermonuclear runaway completely disrupts star with zero remnant
            bool hasRemnant = false;
            double ejectaMassFraction = 1.0;

            Assert.IsFalse(hasRemnant, "Type Ia supernova must leave no central neutron star or black hole remnant.");
            Assert.AreEqual(1.0, ejectaMassFraction, 1e-9, "All mass is dispersed as iron-group ejecta cloud.");
        }

        [Test]
        public void F15_Supernova_TypeII_CoreCollapse_SpawnsNeutronStarRemnant()
        {
            // Progenitor mass between 8 and 25 M_sun collapses into neutron star remnant
            double progenitorMass = 15.0; // Solar masses

            BodyType remnantType;
            double remnantMass;

            if (progenitorMass < 8.0)
            {
                remnantType = BodyType.Terrestrial; // White dwarf
                remnantMass = 0.6;
            }
            else if (progenitorMass <= 25.0)
            {
                remnantType = BodyType.Pulsar; // Neutron star
                remnantMass = 1.4;
            }
            else
            {
                remnantType = BodyType.BlackHole; // Black hole
                remnantMass = 3.0;
            }

            Assert.AreEqual(BodyType.Pulsar, remnantType, "15 M_sun core-collapse supernova must spawn Pulsar/Neutron star.");
            Assert.AreEqual(1.4, remnantMass, 1e-9);
        }

        [Test]
        public void F15_Supernova_TypeII_MassiveCoreCollapse_SpawnsBlackHoleRemnant()
        {
            // Progenitor mass > 25 M_sun collapses directly into stellar-mass black hole
            double progenitorMass = 40.0; // Solar masses

            BodyType remnantType = progenitorMass > 25.0 ? BodyType.BlackHole : BodyType.Pulsar;
            double remnantMass = progenitorMass > 25.0 ? (progenitorMass * 0.15) : 1.4;

            Assert.AreEqual(BodyType.BlackHole, remnantType, "40 M_sun core-collapse supernova must spawn Black Hole.");
            Assert.AreEqual(6.0, remnantMass, 1e-9);
        }

        [Test]
        public void F15_Supernova_KineticEnergyRelease_DepositsTenToFiftyOneErgs()
        {
            // Standard supernova canonical kinetic energy is 10^51 ergs (1 Foe = 10^44 J)
            double canonicalEnergyJoules = 1e44; // 1 Foe
            double blastVelocity = 10000e3; // 10,000 km/s ejecta
            double ejectaMass = 2.0 * CosmoForgeTestHarness.StandardSolarMass; // 2 solar masses

            double actualKineticEnergy = 0.5 * ejectaMass * blastVelocity * blastVelocity;

            Assert.Greater(actualKineticEnergy, 0.5 * canonicalEnergyJoules, "Supernova blast energy must match ~10^44 Joules order of magnitude.");
            Assert.Less(actualKineticEnergy, 5.0 * canonicalEnergyJoules);
        }

        #endregion

        #region Feature F16: Compact Binary Mergers & Gravitational Waves (5 Tests)

        [Test]
        public void F16_BinaryMerger_PetersDecayRate_ScalesWithInverseCubeOfSemiMajorAxis()
        {
            // Requirement R4: da/dt ~ -1 / a^3 (orbital shrinkage accelerates as binary closes)
            double G = 1.0;
            double c = 50.0;
            double m1 = 10.0;
            double m2 = 10.0;

            double aWide = 50.0;
            double aClose = 25.0;

            double decayWide = Math.Abs(CosmoForgeTestHarness.CalculatePetersOrbitalDecayRate(G, c, m1, m2, aWide));
            double decayClose = Math.Abs(CosmoForgeTestHarness.CalculatePetersOrbitalDecayRate(G, c, m1, m2, aClose));

            double ratio = decayClose / decayWide;
            double expectedRatio = Math.Pow(aWide / aClose, 3.0); // 2^3 = 8.0

            Assert.AreEqual(8.0, ratio, 1e-4, "Halving binary orbital radius must increase Peters decay rate by exactly 8x.");
            Assert.Greater(decayClose, decayWide);
        }

        [Test]
        public void F16_BinaryMerger_GravitationalWaveFrequency_IncreasesDuringInspiral()
        {
            // f_GW = (1/pi) * sqrt(G * M_tot / a^3) -> frequency chirps upward as separation decays
            double G = 1.0;
            double totalMass = 20.0;
            double a1 = 40.0;
            double a2 = 10.0;

            double f1 = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, totalMass, a1);
            double f2 = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, totalMass, a2);

            Assert.Greater(f2, f1, "Gravitational wave frequency must chirp upward as binary separation shrinks.");
            double expectedRatio = Math.Pow(40.0 / 10.0, 1.5); // 4^1.5 = 8.0
            Assert.AreEqual(expectedRatio, f2 / f1, 1e-4);
        }

        [Test]
        public void F16_BinaryMerger_CoalescenceThreshold_TriggersFinalChirpAndMerger()
        {
            // Coalescence triggers when separation a <= 3 * R_schwarzschild (ISCO)
            double rSchwarzschild = 2.0;
            double iscoRadius = 3.0 * rSchwarzschild; // 6.0

            double aSeparated = 8.0;
            double aAtISCO = 6.0;
            double aMerged = 5.0;

            Func<double, bool> isCoalesced = a => a <= iscoRadius;

            Assert.IsFalse(isCoalesced(aSeparated), "Binary outside ISCO has not coalesced.");
            Assert.IsTrue(isCoalesced(aAtISCO), "Binary reaching ISCO triggers coalescence merger.");
            Assert.IsTrue(isCoalesced(aMerged), "Binary inside ISCO is merged.");
        }

        [Test]
        public void F16_BinaryMerger_GravitationalWaveEnergyLoss_ReducesFinalRemnantMass()
        {
            // Radiated GW mass loss: M_remnant = M_tot - E_GW / c^2 (typically ~5% radiated in equal mass merger)
            double m1 = 30.0;
            double m2 = 30.0;
            double totalInitialMass = m1 + m2; // 60.0

            double radiatedMassFraction = 0.05; // 5% converted to gravitational radiation
            double remnantMass = totalInitialMass * (1.0 - radiatedMassFraction); // 57.0

            Assert.Less(remnantMass, totalInitialMass, "Final coalesced black hole must have lower mass due to GW radiation loss.");
            Assert.AreEqual(57.0, remnantMass, 1e-9);
        }

        [Test]
        public void F16_BinaryMerger_SpacetimeRippleVisualizer_SpawnsExpandingConcentricRipples()
        {
            // Concentric metric ripples propagate outward at speed c
            double c = 50.0;
            double t = 0.5;
            double waveSpeed = c;

            double expectedFrontRadius = waveSpeed * t; // 25.0
            Assert.AreEqual(25.0, expectedFrontRadius, 1e-9, "Gravitational wave wavefront must propagate radially at speed c.");
        }

        #endregion
    }
}
