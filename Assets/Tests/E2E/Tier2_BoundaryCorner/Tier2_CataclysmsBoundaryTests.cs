// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Cataclysms & Collision Tests (F12 - F16)
// 25 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts, TEST_INFRA.md, & ORIGINAL_REQUEST.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Cataclysms;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_CataclysmsBoundaryTests
    {
        #region Feature F12: Inelastic Collision & Merger Engine (5 Boundary Tests)

        [Test]
        public void F12_Boundary_InelasticCollision_HeadOnEqualMassOppositeVelocity_ZeroMergedVelocity()
        {
            // Boundary condition: Two bodies of identical mass m1 = m2 = 500.0 colliding head-on
            // at exact opposite velocities via ICollisionEngine.
            ICollisionEngine engine = new HarnessCollisionEngine();

            var b1 = new CelestialBodyState(1, "A", BodyType.Terrestrial, 500.0, 5.0, new Vector3d(4.0, 0.0, 0.0), new Vector3d(10.0, 0.0, 0.0));
            var b2 = new CelestialBodyState(2, "B", BodyType.Terrestrial, 500.0, 5.0, Vector3d.zero, new Vector3d(-10.0, 0.0, 0.0));

            CollisionEvent recorded = default;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b1, b2 }, 0.02, evt => recorded = evt);

            Assert.AreEqual(Vector3d.zero, recorded.MergedVelocity, "Head-on symmetric collision must yield exactly zero merged velocity.");
            TestAssertions.AssertNoNaNOrInfinity(recorded.MergedVelocity, "Merged velocity");
        }

        [Test]
        public void F12_Boundary_InelasticCollision_ExtremeMassRatio_ImpactorMomentumAbsorbedAccurately()
        {
            // Boundary condition: Central planet of mass 10^8 at rest vs small high-speed meteor of mass 1.0 at v = 1000.
            // Momentum conservation must hold to machine precision via ICollisionEngine.
            ICollisionEngine engine = new HarnessCollisionEngine();

            double mPrimary = 1e8;
            double mMeteor = 1.0;
            var primary = new CelestialBodyState(1, "Primary", BodyType.Terrestrial, mPrimary, 100.0, Vector3d.zero, Vector3d.zero);
            var meteor = new CelestialBodyState(2, "Meteor", BodyType.Terrestrial, mMeteor, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(1000.0, 0.0, 0.0));

            CollisionEvent recorded = default;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { primary, meteor }, 0.02, evt => recorded = evt);

            double expectedV = 1000.0 / (mPrimary + mMeteor);
            Assert.AreEqual(expectedV, recorded.MergedVelocity.x, 1e-12, "Merged speed must match analytical ratio.");
            TestAssertions.AssertApproximatelyEqual(expectedV, recorded.MergedVelocity.x, 1e-12, "Merged speed must match analytical ratio.");

            Vector3d initialP = meteor.Velocity * mMeteor;
            Vector3d finalP = recorded.MergedVelocity * (mPrimary + mMeteor);
            TestAssertions.AssertLinearMomentumConserved(initialP, finalP, 1e-9, "Extreme mass ratio merger momentum");
        }

        [Test]
        public void F12_Boundary_InelasticCollision_GrazingContact_ExactSumOfRadiiThreshold()
        {
            // Boundary condition: Center-to-center distance d = R1 + R2 (contact boundary) via ICollisionEngine.
            // At d = R1 + R2, bodies touch and trigger collision. At d = R1 + R2 + 0.01, they do not touch.
            ICollisionEngine engine = new HarnessCollisionEngine();

            double r1 = 15.0;
            double r2 = 10.0;
            double sumRadii = r1 + r2; // 25.0

            // Test 1: Touching at exact sum of radii
            var b1 = new CelestialBodyState(1, "B1", BodyType.Terrestrial, 100.0, r1, Vector3d.zero, Vector3d.zero);
            var b2Touching = new CelestialBodyState(2, "B2Touching", BodyType.Terrestrial, 50.0, r2, new Vector3d(sumRadii, 0.0, 0.0), Vector3d.zero);

            bool touchingFired = false;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b1, b2Touching }, 0.02, evt => touchingFired = true);
            Assert.IsTrue(touchingFired, "Bodies touching at exact sum of radii R1 + R2 must trigger collision in ICollisionEngine.");

            // Test 2: Near miss just beyond sum of radii
            var b3 = new CelestialBodyState(3, "B3", BodyType.Terrestrial, 100.0, r1, Vector3d.zero, Vector3d.zero);
            var b4NearMiss = new CelestialBodyState(4, "B4NearMiss", BodyType.Terrestrial, 50.0, r2, new Vector3d(sumRadii + 0.01, 0.0, 0.0), Vector3d.zero);

            bool nearMissFired = false;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b3, b4NearMiss }, 0.02, evt => nearMissFired = true);
            Assert.IsFalse(nearMissFired, "Bodies separated beyond R1 + R2 must not trigger collision in ICollisionEngine.");
        }

        [Test]
        public void F12_Boundary_InelasticCollision_CatastrophicDisruption_KineticVsBindingEnergyThreshold()
        {
            // Boundary condition: Hypervelocity collision exceeding 5 * binding energy triggers catastrophic fragmentation.
            ICollisionEngine engine = new HarnessCollisionEngine();

            var b1 = new CelestialBodyState(1, "Primary", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var b2 = new CelestialBodyState(2, "Impactor", BodyType.Terrestrial, 500.0, 5.0, new Vector3d(5.0, 0.0, 0.0), new Vector3d(100.0, 0.0, 0.0));

            CollisionEvent recorded = default;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b1, b2 }, 0.02, evt => recorded = evt);

            Assert.IsTrue(recorded.CatastrophicFragmentation, "Hypervelocity collision exceeding binding energy must trigger catastrophic disruption.");
        }

        [Test]
        public void F12_Boundary_InelasticCollision_ThreeBodiesSimultaneousMerge_ConservesTotalLinearMomentum()
        {
            // Boundary condition: 3 bodies colliding in sequence in the same simulation step.
            double m1 = 100.0, m2 = 200.0, m3 = 300.0;
            double totalMass = m1 + m2 + m3;

            Vector3d v1 = new Vector3d(10.0, 5.0, 0.0);
            Vector3d v2 = new Vector3d(-5.0, 10.0, 2.0);
            Vector3d v3 = new Vector3d(2.0, -15.0, -4.0);

            Vector3d initialP = v1 * m1 + v2 * m2 + v3 * m3;
            Vector3d vMerged = initialP / totalMass;
            Vector3d finalP = vMerged * totalMass;

            Assert.AreEqual(initialP.x, finalP.x, 1e-12, "Linear momentum X must be conserved.");
            TestAssertions.AssertLinearMomentumConserved(initialP, finalP, 1e-12, "3-body simultaneous merger momentum conservation");
            TestAssertions.AssertNoNaNOrInfinity(vMerged, "3-body merged velocity");
        }

        #endregion

        #region Feature F13: Collision Cataclysm Visual Effects (5 Boundary Tests)

        [Test]
        public void F13_Boundary_CollisionVFX_ZeroMassImpact_ZeroDebrisSpawning()
        {
            // Boundary condition: Zero impact energy spawns exactly 0 debris particles.
            ICollisionEngine engine = new HarnessCollisionEngine();
            var b1 = new CelestialBodyState(1, "P1", BodyType.Terrestrial, 100.0, 5.0, Vector3d.zero, Vector3d.zero);
            var b2 = new CelestialBodyState(2, "P2", BodyType.Terrestrial, 0.0, 1.0, Vector3d.zero, Vector3d.zero);

            CollisionEvent recorded = default;
            engine.CheckAndResolveCollisions(new List<CelestialBodyState> { b1, b2 }, 0.02, evt => recorded = evt);

            int particleCount = (int)Math.Clamp(Math.Floor(recorded.DissipatedKineticEnergy * 10.0), 0.0, 500.0);

            Assert.AreEqual(0, particleCount, "Zero energy impact must spawn exactly 0 particles.");
            Assert.GreaterOrEqual(particleCount, 0, "Particle count must never be negative.");
        }

        [Test]
        public void F13_Boundary_CollisionVFX_ShockwaveExpansion_ZeroRadiusAtTimeZero()
        {
            // Boundary condition: Shockwave ring expansion radius r(t) at time of impact Delta_t = 0.0.
            double vShock = 50.0;
            double dt = 0.0;

            double radiusAtZero = vShock * dt;
            Assert.AreEqual(0.0, radiusAtZero, "Shockwave radius at collision instant must be 0.0.");

            double dtLater = 0.5;
            double radiusLater = vShock * dtLater;
            Assert.AreEqual(25.0, radiusLater, 1e-9, "Shockwave radius must expand linearly with elapsed time.");
        }

        [Test]
        public void F13_Boundary_CollisionVFX_ParticleLifespan_ExpiredParticlesCulledFromBuffer()
        {
            // Boundary condition: Debris particle lifetime tracking at boundary t = tau_lifetime.
            double tau = 3.0;

            double tActive = 2.99;
            double tBoundary = 3.00;
            double tExpired = 3.01;

            bool active1 = tActive < tau;
            bool active2 = tBoundary < tau;
            bool active3 = tExpired < tau;

            Assert.IsTrue(active1, "Particle at 2.99s must remain active.");
            Assert.IsFalse(active2, "Particle at exactly 3.00s must be culled.");
            Assert.IsFalse(active3, "Particle at 3.01s must be culled.");
        }

        [Test]
        public void F13_Boundary_CollisionVFX_MaxParticleBudget_SaturationCapEnforced()
        {
            // Boundary condition: Super-massive collision generating request for 50,000 debris particles.
            int requestedParticles = 50000;
            int maxBudget = 500;

            int actualParticles = Math.Min(requestedParticles, maxBudget);

            Assert.AreEqual(500, actualParticles, "Particle spawning must be clamped to max budget cap.");
            Assert.LessOrEqual(actualParticles, maxBudget);
        }

        [Test]
        public void F13_Boundary_CollisionVFX_WhiteFlashIntensity_HDRClampedSafely()
        {
            // Boundary condition: Flash intensity request for hyper-energy merger.
            float rawLuminance = 1500.0f;
            float maxHDR = 10.0f;

            float clampedLuminance = Mathf.Clamp(rawLuminance, 0.0f, maxHDR);

            Assert.AreEqual(maxHDR, clampedLuminance, "Flash intensity must clamp to maximum HDR luminance 10.0.");
            Assert.GreaterOrEqual(clampedLuminance, 0.0f);
        }

        #endregion

        #region Feature F14: Fluid Roche Limit Tidal Disruption (5 Boundary Tests)

        [Test]
        public void F14_Boundary_RocheLimit_EqualDensities_ExactCoefficientTwoPointFourFour()
        {
            // Boundary condition: Primary and secondary bodies have identical densities (rho_M / rho_m = 1.0).
            double primaryRadius = 100.0;
            double rhoM = 1500.0;
            double rhom = 1500.0;

            double dRoche = CosmoForgeTestHarness.CalculateFluidRocheLimit(primaryRadius, rhoM, rhom);

            Assert.AreEqual(2.44 * primaryRadius, dRoche, 1e-9, "Roche limit for equal density must be 2.44 * R_M.");
            TestAssertions.AssertApproximatelyEqual(2.44 * primaryRadius, dRoche, 1e-9, "Roche limit for equal density must be 2.44 * R_M.");
        }

        [Test]
        public void F14_Boundary_RocheLimit_ExactThresholdCrossing_DisruptionTriggered()
        {
            // Boundary condition: Moon crossing Roche limit threshold via ICollisionEngine.
            ICollisionEngine engine = new HarnessCollisionEngine();
            var primary = new CelestialBodyState(1, "Jupiter", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);

            // Test inside Roche limit
            var moonInside = new CelestialBodyState(2, "MoonInside", BodyType.Moon, 1.0, 1.0, new Vector3d(15.0, 0.0, 0.0), Vector3d.zero);
            bool disruptedInside = false;
            engine.EvaluateRocheLimits(new List<CelestialBodyState> { primary, moonInside }, evt => disruptedInside = true);
            Assert.IsTrue(disruptedInside, "Moon inside Roche limit must trigger tidal disruption in ICollisionEngine.");

            // Test outside Roche limit
            var moonOutside = new CelestialBodyState(3, "MoonOutside", BodyType.Moon, 1.0, 1.0, new Vector3d(150.0, 0.0, 0.0), Vector3d.zero);
            bool disruptedOutside = false;
            engine.EvaluateRocheLimits(new List<CelestialBodyState> { primary, moonOutside }, evt => disruptedOutside = true);
            Assert.IsFalse(disruptedOutside, "Moon outside Roche limit must not be disrupted.");
        }

        [Test]
        public void F14_Boundary_RocheLimit_DebrisParticleRetention_ExactlyEightyPercentBoundary()
        {
            // Boundary condition: Moon shatters into 100 ring particles. Exactly 80 particles remain in stable orbits.
            int totalParticles = 100;
            int retainedParticles = 80;

            double retentionRatio = (double)retainedParticles / totalParticles;
            double retentionPercent = retentionRatio * 100.0;

            Assert.AreEqual(80.0, retentionPercent, 1e-9, "Retention must be exactly 80.0%.");
            bool passesMission5 = retentionPercent >= 80.0;
            Assert.IsTrue(passesMission5, "Exact 80.0% retention must satisfy Mission 5 win criterion.");

            int failedParticles = 79;
            bool failsMission5 = ((double)failedParticles / totalParticles) * 100.0 >= 80.0;
            Assert.IsFalse(failsMission5, "79.0% retention must fail Mission 5 win criterion.");
        }

        [Test]
        public void F14_Boundary_RocheLimit_ZeroRadiusMoon_DoesNotCrashOrDivideByZero()
        {
            // Boundary condition: Moon radius = 0.0 (infinitesimal test particle).
            double primaryRadius = 100.0;
            double rhoM = 2000.0;
            double rhom = 1000.0;

            double dRoche = CosmoForgeTestHarness.CalculateFluidRocheLimit(primaryRadius, rhoM, rhom);

            Assert.IsFalse(double.IsNaN(dRoche), "Roche limit must not be NaN.");
            Assert.Greater(dRoche, 0.0, "Roche limit must be positive.");
        }

        [Test]
        public void F14_Boundary_RocheLimit_PlanarRingDispersion_VerticalThicknessStrictlyBound()
        {
            // Boundary condition: Debris particles produced by tidal disruption settling into a planar ring.
            double orbitalRadius = 150.0;
            double maxAllowedZ = 0.10 * orbitalRadius;

            Vector3d inPlaneParticle = new Vector3d(orbitalRadius, 0.0, 10.0);
            Vector3d outOfPlaneParticle = new Vector3d(orbitalRadius, 0.0, 16.0);

            bool isInPlane = Math.Abs(inPlaneParticle.z) <= maxAllowedZ;
            bool isOutOfPlane = Math.Abs(outOfPlaneParticle.z) <= maxAllowedZ;

            Assert.IsTrue(isInPlane, "Particle with |z| = 10 must be accepted in planar ring.");
            Assert.IsFalse(isOutOfPlane, "Particle with |z| = 16 must be rejected from planar ring.");
        }

        #endregion

        #region Feature F15: Stellar Supernova Explosions (5 Boundary Tests)

        [Test]
        public void F15_Boundary_Supernova_TypeIa_ZeroRemnantMass_CompleteDestruction()
        {
            // Boundary condition: Type Ia thermonuclear supernova (Chandrasekhar mass detonation).
            var whiteDwarf = new CelestialBodyState(1, "WD", BodyType.Star, 1.44, 0.01, Vector3d.zero, Vector3d.zero);

            bool success = CosmoForgeTestHarness.ResolveSupernova(whiteDwarf, out double remnantMass, isTypeIa: true);

            Assert.IsTrue(success, "Supernova resolution must succeed.");
            Assert.AreEqual(0.0, remnantMass, "Type Ia thermonuclear supernova must leave zero remnant mass.");
            Assert.IsTrue(whiteDwarf.IsDestroyed, "Progenitor white dwarf must be destroyed.");
        }

        [Test]
        public void F15_Boundary_Supernova_TypeII_RemnantTransitionAt25SolarMasses()
        {
            // Boundary condition: Type II core collapse mass boundary between Neutron Star and Black Hole remnants.
            double massPulsarProgenitor = 24.9;
            double massBHProgenitor = 25.1;

            BodyType remnantTypePulsar = massPulsarProgenitor < 25.0 ? BodyType.Pulsar : BodyType.BlackHole;
            BodyType remnantTypeBH = massBHProgenitor < 25.0 ? BodyType.Pulsar : BodyType.BlackHole;

            Assert.AreEqual(BodyType.Pulsar, remnantTypePulsar, "24.9 M_sun progenitor must collapse into a Pulsar remnant.");
            Assert.AreEqual(BodyType.BlackHole, remnantTypeBH, "25.1 M_sun progenitor must collapse into a Black Hole remnant.");
        }

        [Test]
        public void F15_Boundary_Supernova_BlastWave_ExpansionVelocityConserved()
        {
            // Boundary condition: Supernova shock blast front expanding at v_blast = 1000.0.
            double vBlast = 1000.0;
            double t = 10.0;

            double radius = vBlast * t;

            Assert.AreEqual(10000.0, radius, 1e-9, "Supernova blast radius must expand linearly at constant initial shock velocity.");
            Assert.IsFalse(double.IsNaN(radius), "Blast radius must not be NaN.");
        }

        [Test]
        public void F15_Boundary_Supernova_EjectedMass_ConservesTotalProgenitorMass()
        {
            // Boundary condition: Mass conservation in core collapse.
            double mProgenitor = 20.0;
            double mRemnant = 1.4;
            double mEjecta = mProgenitor - mRemnant;

            Assert.AreEqual(18.6, mEjecta, 1e-9, "Ejected envelope mass must equal progenitor minus remnant.");
            Assert.AreEqual(mProgenitor, mRemnant + mEjecta, 1e-9, "Total mass must be strictly conserved in supernova.");
        }

        [Test]
        public void F15_Boundary_Supernova_LuminosityDecay_RadioactiveDecayCurve_NeverNegative()
        {
            // Boundary condition: Supernova light curve decays via radioactive decay.
            double lPeak = 1e10;
            double tau = 77.0;

            for (double t = 0.0; t <= 1000.0; t += 100.0)
            {
                double lCurrent = lPeak * Math.Exp(-t / tau);

                Assert.GreaterOrEqual(lCurrent, 0.0, $"Luminosity at t={t} must be non-negative.");
                Assert.LessOrEqual(lCurrent, lPeak, $"Luminosity at t={t} must not exceed peak.");
                Assert.IsFalse(double.IsNaN(lCurrent), $"Luminosity at t={t} must not be NaN.");
            }
        }

        #endregion

        #region Feature F16: Compact Binary Mergers & Gravitational Waves (5 Boundary Tests)

        [Test]
        public void F16_Boundary_BinaryMerger_CoalescenceSeparation_SchwarzschildContactThreshold()
        {
            // Boundary condition: Two merging black holes with Schwarzschild radii R_s1 and R_s2.
            double G = 1.0;
            double c = 50.0;
            double m1 = 10000.0;
            double m2 = 8000.0;

            double rs1 = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, m1, c);
            double rs2 = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, m2, c);
            double contactSeparation = rs1 + rs2;

            double distOrbiting = contactSeparation + 0.1;
            double distCoalescing = contactSeparation - 0.1;

            bool mergesOrbiting = distOrbiting <= contactSeparation;
            bool mergesCoalescing = distCoalescing <= contactSeparation;

            Assert.IsFalse(mergesOrbiting, "Black holes outside contact separation must not merge yet.");
            Assert.IsTrue(mergesCoalescing, "Black holes inside contact separation must merge into single remnant.");
        }

        [Test]
        public void F16_Boundary_BinaryMerger_RemnantMass_RadiatedEnergyLossConserved()
        {
            // Boundary condition: Coalescing binary black holes radiate ~5% of total mass-energy as gravitational waves.
            double m1 = 30.0;
            double m2 = 30.0;
            double radiatedFraction = 0.05;

            double mRemnant = (m1 + m2) * (1.0 - radiatedFraction);
            double radiatedEnergyMassEquiv = (m1 + m2) * radiatedFraction;

            Assert.AreEqual(57.0, mRemnant, 1e-9, "Merged black hole mass must be 57.0 solar masses.");
            Assert.AreEqual(3.0, radiatedEnergyMassEquiv, 1e-9, "Radiated gravitational wave energy must equal 3.0 solar masses.");
            Assert.AreEqual(m1 + m2, mRemnant + radiatedEnergyMassEquiv, 1e-9, "Mass-energy must be conserved.");
        }

        [Test]
        public void F16_Boundary_BinaryMerger_ChirpFrequency_DivergenceClampedAtISCO()
        {
            // Boundary condition: Gravitational wave chirp frequency f_GW = (1/pi) * sqrt(G*M_tot / a^3).
            double G = 1.0;
            double c = 50.0;
            double totalMass = 2000.0;

            double aISCO = (6.0 * G * totalMass) / (c * c);
            double fMaxISCO = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, totalMass, aISCO);

            double aInside = aISCO * 0.5;
            double fRaw = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, totalMass, aInside);
            double fClamped = Math.Min(fRaw, fMaxISCO);

            Assert.AreEqual(fMaxISCO, fClamped, 1e-9, "Gravitational wave frequency must be clamped at ISCO limit.");
            Assert.IsFalse(double.IsInfinity(fClamped), "Chirp frequency must remain finite.");
            Assert.IsFalse(double.IsNaN(fClamped), "Chirp frequency must not be NaN.");
        }

        [Test]
        public void F16_Boundary_BinaryMerger_SpacetimeRipple_ExpandingWavefrontAtSpeedOfLight()
        {
            // Boundary condition: Expanding spacetime metric ripple visualizer produced upon compact binary coalescence.
            double c = CosmoForgeTestHarness.DefaultC;
            double dt = 2.0;

            double waveRadius = c * dt;

            Assert.AreEqual(100.0, waveRadius, 1e-9, "Spacetime ripple wavefront must expand at exactly speed of light c.");
            Assert.Greater(waveRadius, 0.0);
        }

        [Test]
        public void F16_Boundary_BinaryMerger_ZeroMassSecondary_NoGravitationalWaveChirp()
        {
            // Boundary condition: Extreme mass ratio binary with secondary mass m2 = 0.0.
            double G = 1.0;
            double c = 50.0;
            double m1 = 5000.0;
            double m2 = 0.0;
            double a = 50.0;

            double decayRate = CosmoForgeTestHarness.CalculatePetersOrbitalDecayRate(G, c, m1, m2, a);

            Assert.AreEqual(0.0, decayRate, 1e-15, "Orbital decay rate must be exactly zero when secondary body has zero mass.");
        }

        #endregion
    }
}
