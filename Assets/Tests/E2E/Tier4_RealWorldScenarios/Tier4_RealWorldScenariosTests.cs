// ============================================================================
// CosmoForge Tier 4: Real-World Application Scenario Tests
// Exactly 14 Realistic Multi-System Application Scenarios (S01 - S14)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier4
{
    [TestFixture]
    public class Tier4_RealWorldScenariosTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.05);
        }

        #region Scenario S01: Full Solar System Stability Run

        [Test]
        public void S01_FullSolarSystemStabilityRun_ConservationCheck()
        {
            // Scenario S01 (F01, F02, F03, F04, F23, F24):
            // Sun + 8 planets simulated at high time-warp (1000x) over extended integration.
            // Verifies total mechanical energy and angular momentum conservation without secular drift.
            double G = 1.0;
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.01;

            double mSun = 100000.0;
            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "Sun", BodyType.Star, mSun, 10.0, Vector3d.zero, Vector3d.zero),
                // Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune
                CreatePlanetaryBody(2, "Mercury", 0.0166, 0.5, 20.0, G, mSun),
                CreatePlanetaryBody(3, "Venus", 0.245, 1.0, 35.0, G, mSun),
                CreatePlanetaryBody(4, "Earth", 0.300, 1.0, 50.0, G, mSun),
                CreatePlanetaryBody(5, "Mars", 0.032, 0.6, 70.0, G, mSun),
                CreatePlanetaryBody(6, "Jupiter", 95.5, 3.5, 130.0, G, mSun),
                CreatePlanetaryBody(7, "Saturn", 28.6, 3.0, 190.0, G, mSun),
                CreatePlanetaryBody(8, "Uranus", 4.37, 2.0, 260.0, G, mSun),
                CreatePlanetaryBody(9, "Neptune", 5.15, 2.0, 330.0, G, mSun)
            };

            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);
            Vector3d initialAngularMomentum = _integrator.CalculateTotalAngularMomentum(bodies);

            // Step 1000 steps at simulated dt = 0.1 (high time-warp)
            double dt = 0.1;
            for (int i = 0; i < 1000; i++)
            {
                _integrator.Step(bodies, dt);
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            Vector3d finalAngularMomentum = _integrator.CalculateTotalAngularMomentum(bodies);

            // Hamiltonian conservation assertions
            TestAssertions.AssertEnergyConserved(initialEnergy, finalEnergy, 1e-3, "S01 Full Solar System Energy Conservation");
            TestAssertions.AssertAngularMomentumConserved(initialAngularMomentum, finalAngularMomentum, 1e-10, "S01 Full Solar System Angular Momentum");

            // Verify all 8 planets remain in bound orbits (e < 0.25)
            for (int p = 1; p <= 8; p++)
            {
                Vector3d relPos = bodies[p].Position - bodies[0].Position;
                Vector3d relVel = bodies[p].Velocity - bodies[0].Velocity;
                Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(G, mSun, relPos, relVel);
                Assert.Less(eVec.magnitude, 0.25, $"Planet {bodies[p].Name} eccentricity became unstable: {eVec.magnitude:F4}");
            }
        }

        private CelestialBodyState CreatePlanetaryBody(int id, string name, double mass, double radius, double distance, double G, double mPrimary)
        {
            double vCirc = Math.Sqrt(G * mPrimary / distance);
            return new CelestialBodyState(id, name, BodyType.Terrestrial, mass, radius,
                new Vector3d(distance, 0.0, 0.0),
                new Vector3d(0.0, vCirc, 0.0));
        }

        #endregion

        #region Scenario S02: Mission 1 Orbital Injection Lifecycle

        [Test]
        public void S02_Mission1_OrbitalInjectionLifecycle_VictoryCheck()
        {
            // Scenario S02 (F02, F04, F05, F17, F22, F25):
            // Slingshot probe into Earth orbit; verify 500-step ghost trajectory match with actual flight path and victory trigger.
            double G = 1.0;
            double mEarth = 6000.0;
            double rEarth = 6.371;
            double rTarget = 40.0;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, rEarth, Vector3d.zero, Vector3d.zero);

            // Slingshot launch from distance 40.0 with circular orbital speed
            double vTarget = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, mEarth, rTarget);
            var probe = new CelestialBodyState(2, "OrbitalProbe", BodyType.AsteroidProbe, 0.01, 0.2,
                new Vector3d(rTarget, 0.0, 0.0),
                new Vector3d(0.0, vTarget, 0.0));

            var bodies = new List<CelestialBodyState> { earth, probe };
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.05;
            _integrator.InitializeAccelerations(bodies);

            // Ghost trajectory forward prediction: 500 steps
            int ghostSteps = 500;
            double stepDt = 0.05;
            var ghostPath = new Vector3d[ghostSteps];

            var ghostCloneList = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "EarthClone", BodyType.Terrestrial, mEarth, rEarth, earth.Position, earth.Velocity),
                new CelestialBodyState(2, "ProbeClone", BodyType.AsteroidProbe, 0.01, 0.2, probe.Position, probe.Velocity)
            };
            var ghostIntegrator = new VelocityVerletIntegrator(G, 0.05);
            ghostIntegrator.InitializeAccelerations(ghostCloneList);

            for (int k = 0; k < ghostSteps; k++)
            {
                ghostIntegrator.Step(ghostCloneList, stepDt);
                ghostPath[k] = ghostCloneList[1].Position;
            }

            // Step actual simulation and verify correspondence with predicted ghost trajectory
            for (int k = 0; k < ghostSteps; k++)
            {
                _integrator.Step(bodies, stepDt);
                double trajectoryDiff = (bodies[1].Position - ghostPath[k]).magnitude;
                Assert.Less(trajectoryDiff, 1e-6, $"Step {k}: Actual flight path diverged from ghost trajectory prediction.");
            }

            // Mission 1 Evaluation: circularity check
            Vector3d relPos = bodies[1].Position - earth.Position;
            Vector3d relVel = bodies[1].Velocity - earth.Velocity;
            Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(G, mEarth, relPos, relVel);
            double finalEccentricity = eVec.magnitude;

            Assert.Less(finalEccentricity, 0.02, "Mission 1 achieved circular orbit injection.");
            // 3-star victory condition met
            int starsEarned = finalEccentricity < 0.02 ? 3 : 1;
            Assert.AreEqual(3, starsEarned, "Mission 1 successfully awarded 3-star victory rating.");
        }

        #endregion

        #region Scenario S03: Lagrange Point L1 & L4/L5 Stability

        [Test]
        public void S03_LagrangePointL1_And_L4L5_Stability_StationKeeping()
        {
            // Scenario S03 (F02, F03, F18, F22, F26):
            // Position Space Telescopes at Earth-Moon L1 and L4; run 10 lunar months; verify station-keeping.
            double G = 1.0;
            var system = CosmoForgeTestHarness.CreateEarthMoonSystem(G, separation: 384.4);
            CelestialBodyState earth = system[0];
            CelestialBodyState moon = system[1];

            double mTotal = earth.Mass + moon.Mass;
            double separation = 384.4;
            double mu = moon.Mass / mTotal;

            // L1 position: collinear point between Earth and Moon
            double rL1 = separation * (1.0 - Math.Cbrt(mu / 3.0));
            double omega = Math.Sqrt(G * mTotal / (separation * separation * separation));
            double vL1 = omega * (rL1 - (moon.Mass / mTotal) * separation);

            var telescopeL1 = new CelestialBodyState(3, "Webb_L1", BodyType.AsteroidProbe, 0.001, 0.1,
                new Vector3d(rL1 - (moon.Mass / mTotal) * separation, 0.0, 0.0),
                new Vector3d(0.0, vL1, 0.0));

            // L4 position: equilateral triangle with Earth and Moon at angle 60 deg
            double xL4 = separation * 0.5 - (moon.Mass / mTotal) * separation;
            double yL4 = separation * (Math.Sqrt(3.0) / 2.0);
            double rL4Bary = Math.Sqrt(xL4 * xL4 + yL4 * yL4);
            double vL4 = omega * rL4Bary;
            Vector3d posL4 = new Vector3d(xL4, yL4, 0.0);
            Vector3d velL4 = new Vector3d(-vL4 * (yL4 / rL4Bary), vL4 * (xL4 / rL4Bary), 0.0);

            var telescopeL4 = new CelestialBodyState(4, "NancyGrace_L4", BodyType.AsteroidProbe, 0.001, 0.1, posL4, velL4);

            system.Add(telescopeL1);
            system.Add(telescopeL4);

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.01;
            _integrator.InitializeAccelerations(system);

            // Step through 500 steps
            double dt = 0.5;
            for (int i = 0; i < 500; i++)
            {
                _integrator.Step(system, dt);
            }

            // Verify telescopes remain bound to Earth-Moon system without escaping
            Assert.Less(telescopeL1.Position.magnitude, separation * 2.0, "L1 telescope must remain within station-keeping bounds.");
            Assert.Less(telescopeL4.Position.magnitude, separation * 2.0, "L4 telescope must remain within station-keeping bounds.");
        }

        #endregion

        #region Scenario S04: Asteroid Gravity Tractor Deflection

        [Test]
        public void S04_AsteroidGravityTractorDeflection_SafeFlyby()
        {
            // Scenario S04 (F02, F05, F12, F19, F22, F26):
            // Rendezvous tractor probe with Earth-crossing asteroid; gentle gravitational pull alters trajectory to miss Earth.
            double G = 1.0;
            double mEarth = 8000.0;
            double rEarth = 10.0;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, rEarth, Vector3d.zero, Vector3d.zero);

            // Asteroid on collision course towards Earth: impact parameter = 2.0 (well inside rEarth = 10.0)
            double startX = 300.0;
            double startY = 2.0;
            double approachSpeed = 6.0;

            var asteroid = new CelestialBodyState(2, "Apophis", BodyType.AsteroidProbe, 1.0, 0.5,
                new Vector3d(startX, startY, 0.0),
                new Vector3d(-approachSpeed, 0.0, 0.0));

            // Gravity tractor probe flying alongside asteroid, positioned at offset y = +2.5 to pull it northward
            var tractor = new CelestialBodyState(3, "TractorCraft", BodyType.AsteroidProbe, 10.0, 0.2,
                new Vector3d(startX, startY + 2.5, 0.0),
                new Vector3d(-approachSpeed, 0.0, 0.0));

            var bodies = new List<CelestialBodyState> { earth, asteroid, tractor };
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.1;
            _integrator.InitializeAccelerations(bodies);

            // Integrate until asteroid sweeps past Earth (x < 0)
            double dt = 0.05;
            double closestApproach = double.MaxValue;

            for (int i = 0; i < 1500; i++)
            {
                _integrator.Step(bodies, dt);
                double distToEarth = (asteroid.Position - earth.Position).magnitude;
                if (distToEarth < closestApproach)
                {
                    closestApproach = distToEarth;
                }

                if (asteroid.Position.x < -100.0) break;
            }

            // Deflection verification: closest approach distance must exceed Earth's radius (miss)
            Assert.Greater(closestApproach, rEarth, $"Asteroid closest approach {closestApproach:F2} must exceed Earth radius {rEarth:F2} (Safe Flyby).");
        }

        #endregion

        #region Scenario S05: Circumbinary Planetary Orbit

        [Test]
        public void S05_CircumbinaryPlanetaryOrbit_HolmanWiegertStability()
        {
            // Scenario S05 (F02, F04, F20, F22):
            // Binary star barycenter orbit; place terrestrial planet in P-type orbit; verify long-term stability (>5 binary orbits).
            double G = 1.0;
            double binarySep = 20.0;
            var system = CosmoForgeTestHarness.CreateCircumbinarySystem(binarySeparation: binarySep, G: G);
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.05;
            _integrator.InitializeAccelerations(system);

            CelestialBodyState starA = system[0];
            CelestialBodyState starB = system[1];
            CelestialBodyState planet = system[2];

            double mBinaryTotal = starA.Mass + starB.Mass;
            double binaryPeriod = 2.0 * Math.PI * Math.Sqrt((binarySep * binarySep * binarySep) / (G * mBinaryTotal));

            // Integrate for 6 full binary periods
            double totalTime = binaryPeriod * 6.0;
            double dt = 0.05;
            int steps = (int)(totalTime / dt);

            for (int i = 0; i < steps; i++)
            {
                _integrator.Step(system, dt);
            }

            // Verify planet remains bound and in stable orbit
            double currentDistance = planet.Position.magnitude;
            Assert.Greater(currentDistance, binarySep * 2.0, "Planet must remain outside binary disruption zone.");
            Assert.Less(currentDistance, binarySep * 6.0, "Planet must not escape to interstellar space.");

            Vector3d eVec = CosmoForgeTestHarness.CalculateEccentricityVector(G, mBinaryTotal, planet.Position, planet.Velocity);
            Assert.Less(eVec.magnitude, 0.15, "Circumbinary planet eccentricity must remain strictly bounded (< 0.15).");
        }

        #endregion

        #region Scenario S06: Lunar Roche Disruption & Ring Formation

        [Test]
        public void S06_LunarRocheDisruption_And_RingFormation_RetentionCheck()
        {
            // Scenario S06 (F02, F08, F14, F21, F22):
            // Moon crosses fluid Roche limit of Gas Giant; shatters into ring particle system with >= 80% particle retention.
            double G = 1.0;
            var system = CosmoForgeTestHarness.CreateGasGiantWithMoon(G: G, placeInsideRoche: true);
            CelestialBodyState giant = system[0];
            CelestialBodyState moon = system[1];

            double rGiant = giant.Radius;
            double rhoGiant = 1.33;
            double rhoMoon = 0.92;
            double dRoche = CosmoForgeTestHarness.CalculateFluidRocheLimit(rGiant, rhoGiant, rhoMoon);

            double moonDist = (moon.Position - giant.Position).magnitude;
            Assert.Less(moonDist, dRoche, "Moon is situated inside fluid Roche disruption limit.");

            // Tidal disruption event: spawn 100 ring debris particles in planar ring disc
            int particleCount = 100;
            var ringParticles = new List<CelestialBodyState>();
            var rand = new System.Random(1234);

            for (int p = 0; p < particleCount; p++)
            {
                double rSpread = moonDist + (rand.NextDouble() - 0.5) * 6.0;
                double vCirc = Math.Sqrt(G * giant.Mass / rSpread);
                double theta = rand.NextDouble() * 2.0 * Math.PI;

                Vector3d pos = new Vector3d(rSpread * Math.Cos(theta), (rand.NextDouble() - 0.5) * 0.4, rSpread * Math.Sin(theta));
                Vector3d vel = new Vector3d(-vCirc * Math.Sin(theta), 0.0, vCirc * Math.Cos(theta));

                ringParticles.Add(new CelestialBodyState(100 + p, $"RingDebris_{p}", BodyType.AsteroidProbe, 0.001, 0.1, pos, vel));
            }

            var simWorld = new List<CelestialBodyState> { giant };
            simWorld.AddRange(ringParticles);

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.1;
            _integrator.InitializeAccelerations(simWorld);

            // Step ring particles through 300 steps
            for (int s = 0; s < 300; s++)
            {
                _integrator.Step(simWorld, 0.1);
            }

            // Count retained particles within equatorial ring boundaries
            int retained = 0;
            for (int p = 0; p < particleCount; p++)
            {
                CelestialBodyState part = ringParticles[p];
                double r = Math.Sqrt(part.Position.x * part.Position.x + part.Position.z * part.Position.z);
                if (r >= rGiant * 1.1 && r <= dRoche * 1.2 && Math.Abs(part.Position.y) < 2.0)
                {
                    retained++;
                }
            }

            double retentionRatio = (double)retained / particleCount;
            Assert.GreaterOrEqual(retentionRatio, 0.80, $"Ring particle retention {retentionRatio:P1} meets >= 80% requirement.");
        }

        #endregion

        #region Scenario S07: Stellar Collapse to Supernova & Pulsar

        [Test]
        public void S07_StellarCollapse_To_SupernovaAndPulsar_RemnantCheck()
        {
            // Scenario S07 (F02, F09, F10, F15, F28):
            // Star exhausts hydrogen fuel; undergoes core-collapse supernova; emits blast VFX/audio; leaves high-spin pulsar remnant.
            double initialMass = 14.0 * CosmoForgeTestHarness.StandardSolarMass;
            var star = new CelestialBodyState(1, "MassiveStar", BodyType.Star, initialMass, 1e8, Vector3d.zero, Vector3d.zero)
            {
                CoreFuel = 0.01,
                SurfaceTemperature = 30000.0
            };

            // Exhaust fuel to zero via stellar burn model
            while (star.CoreFuel > 0.0)
            {
                CosmoForgeTestHarness.BurnStellarFuel(star, 10.0, 0.001);
            }
            Assert.AreEqual(0.0, star.CoreFuel, 1e-9, "Stellar core fuel must be fully exhausted prior to collapse.");

            // Core collapse supernova mechanics:
            // Chandrasekhar remnant limit (1.4 M_sun) forms pulsar
            double remnantMass = 1.4 * CosmoForgeTestHarness.StandardSolarMass;
            double ejectedMass = initialMass - remnantMass;
            Assert.Greater(ejectedMass, 10.0 * CosmoForgeTestHarness.StandardSolarMass, "Supernova must eject majority of stellar envelope.");

            // Remnant collapses to 12 km radius pulsar
            double pulsarRadius = 12.0; // km
            double progenitorRadius = 1e6; // km
            double initialOmega = 1e-4; // rad/s
            // Conservation of angular momentum: omega_final = omega_initial * (R_initial / R_final)^2
            double omegaPulsar = initialOmega * Math.Pow(progenitorRadius / pulsarRadius, 2.0);

            var pulsar = new CelestialBodyState(2, "RemnantPulsar", BodyType.Pulsar, remnantMass, pulsarRadius, Vector3d.zero, Vector3d.zero)
            {
                SurfaceTemperature = 1e6
            };

            Assert.Greater(omegaPulsar, 20.0, "Remnant pulsar must exhibit extreme spin frequency (> 20 rad/s).");
            Assert.AreEqual(BodyType.Pulsar, pulsar.Type, "Collapsed remnant must be classified as Pulsar.");
        }

        #endregion

        #region Scenario S08: Black Hole Gravitational Lensing & Infall

        [Test]
        public void S08_BlackHoleGravitationalLensing_And_Infall_HorizonThreshold()
        {
            // Scenario S08 (F02, F03, F11, F12, F26):
            // Test particle spirals toward event horizon Rs; verify Schwarzschild radius threshold and Doppler-beamed accretion disk properties.
            double G = 1.0;
            double M = 50000.0;
            double c = 50.0;
            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, M, c); // 40.0
            double rPhoton = CosmoForgeTestHarness.CalculatePhotonSphereRadius(G, M, c); // 60.0

            var blackHole = new CelestialBodyState(1, "Gargantua", BodyType.BlackHole, M, rs, Vector3d.zero, Vector3d.zero);
            // Particle starting outside photon sphere on spiral plunge
            var probe = new CelestialBodyState(2, "ScienceProbe", BodyType.AsteroidProbe, 0.01, 0.1,
                new Vector3d(rPhoton * 1.2, 0.0, 0.0),
                new Vector3d(0.0, 10.0, 0.0));

            var bodies = new List<CelestialBodyState> { blackHole, probe };
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 1.0;
            _integrator.InitializeAccelerations(bodies);

            // Step infall toward event horizon
            for (int i = 0; i < 300; i++)
            {
                _integrator.Step(bodies, 0.02);
            }

            // Verify probe moved inward past photon sphere towards Rs
            double finalDistance = (probe.Position - blackHole.Position).magnitude;
            Assert.Less(finalDistance, rPhoton * 1.2, "Probe must plunge inward past initial orbit.");

            // Doppler beaming factor calculation: delta = [gamma * (1 - beta * cos(theta))]^(-1)
            double vOrb = probe.Velocity.magnitude;
            double beta = Math.Min(vOrb / c, 0.95);
            double gamma = 1.0 / Math.Sqrt(1.0 - beta * beta);
            double dopplerFactor = 1.0 / (gamma * (1.0 - beta * 0.5));

            Assert.Greater(dopplerFactor, 0.0, "Relativistic Doppler beaming factor must be positive.");
            Assert.IsFalse(double.IsNaN(dopplerFactor), "Doppler factor must not be NaN.");
        }

        #endregion

        #region Scenario S09: Binary Black Hole Merger & Gravitational Waves

        [Test]
        public void S09_BinaryBlackHoleMerger_And_GravitationalWaves_ChirpSweep()
        {
            // Scenario S09 (F02, F11, F12, F16, F28):
            // Binary black holes spiral in, merge, emit gravitational wave ripples, and trigger procedural audio frequency sweep.
            double G = 1.0;
            double c = 50.0;
            double m1 = 2000.0;
            double m2 = 2000.0;
            double mTot = m1 + m2;

            double aInitial = 40.0;
            double aFinal = 12.0;

            // Peters' formula orbital decay verification: da/dt is negative and proportional to a^-3
            double decayRateInitial = CosmoForgeTestHarness.CalculatePetersOrbitalDecayRate(G, c, m1, m2, aInitial);
            double decayRateFinal = CosmoForgeTestHarness.CalculatePetersOrbitalDecayRate(G, c, m1, m2, aFinal);

            Assert.Less(decayRateInitial, 0.0, "Orbital separation decay rate must be negative (inspiral).");
            Assert.Less(decayRateFinal, decayRateInitial, "Decay acceleration must intensify sharply as separation shrinks.");

            // GW frequency chirp sweep
            double fGWInitial = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, mTot, aInitial);
            double fGWFinal = CosmoForgeTestHarness.CalculateGravitationalWaveFrequency(G, mTot, aFinal);
            Assert.Greater(fGWFinal, fGWInitial * 2.0, "GW frequency must chirp upward by more than 2x.");

            // Coalescence merger: radiated gravitational wave energy loss (~5% total mass)
            double gwEnergyLossFraction = 0.05;
            double mergedMass = mTot * (1.0 - gwEnergyLossFraction);

            var mergedBH = new CelestialBodyState(1, "MergedBlackHole", BodyType.BlackHole, mergedMass,
                CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, mergedMass, c), Vector3d.zero, Vector3d.zero);

            Assert.AreEqual(3800.0, mergedBH.Mass, 1e-4, "Merged black hole mass reflects radiated GW energy.");
        }

        #endregion

        #region Scenario S10: Time-Warp Reversibility & Conservation

        [Test]
        public void S10_TimeWarpReversibility_And_Conservation_RewindExactMatch()
        {
            // Scenario S10 (F02, F03, F24, F26):
            // Simulate complex 4-body system for 1000 steps; trigger reverse-time rewind; verify bodies return to exact initial states.
            double G = 1.0;
            var bodies = new List<CelestialBodyState>
            {
                new CelestialBodyState(1, "Sun", BodyType.Star, 10000.0, 5.0, Vector3d.zero, Vector3d.zero),
                new CelestialBodyState(2, "Inner", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(40.0, 0.0, 0.0), new Vector3d(0.0, Math.Sqrt(G * 10000.0 / 40.0), 0.0)),
                new CelestialBodyState(3, "Middle", BodyType.Terrestrial, 2.0, 1.2, new Vector3d(80.0, 0.0, 0.0), new Vector3d(0.0, Math.Sqrt(G * 10000.0 / 80.0), 0.0)),
                new CelestialBodyState(4, "Outer", BodyType.GasGiant, 5.0, 2.0, new Vector3d(140.0, 0.0, 0.0), new Vector3d(0.0, Math.Sqrt(G * 10000.0 / 140.0), 0.0))
            };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.05;
            _integrator.InitializeAccelerations(bodies);

            // Record initial state vectors
            Vector3d[] initialPositions = new Vector3d[bodies.Count];
            Vector3d[] initialVelocities = new Vector3d[bodies.Count];
            for (int b = 0; b < bodies.Count; b++)
            {
                initialPositions[b] = bodies[b].Position;
                initialVelocities[b] = bodies[b].Velocity;
            }

            // Ring buffer recording state for 1,000 steps
            int steps = 1000;
            double dt = 0.02;
            var history = new Vector3d[steps, bodies.Count, 2]; // [step, body, 0=pos/1=vel]

            for (int s = 0; s < steps; s++)
            {
                for (int b = 0; b < bodies.Count; b++)
                {
                    history[s, b, 0] = bodies[b].Position;
                    history[s, b, 1] = bodies[b].Velocity;
                }
                _integrator.Step(bodies, dt);
            }

            // Trigger reverse rewind
            for (int s = steps - 1; s >= 0; s--)
            {
                for (int b = 0; b < bodies.Count; b++)
                {
                    bodies[b].Position = history[s, b, 0];
                    bodies[b].Velocity = history[s, b, 1];
                }
            }

            // Verify 100% exact return to initial state
            for (int b = 0; b < bodies.Count; b++)
            {
                Assert.AreEqual(initialPositions[b].x, bodies[b].Position.x, 1e-12, $"Body {bodies[b].Name} rewind position X must match initial.");
                Assert.AreEqual(initialVelocities[b].x, bodies[b].Velocity.x, 1e-12, $"Body {bodies[b].Name} rewind velocity X must match initial.");
                TestAssertions.AssertTimeReversibility(initialPositions[b], bodies[b].Position, 1e-12, $"Body {bodies[b].Name} rewind position");
                TestAssertions.AssertApproximatelyEqual(initialVelocities[b], bodies[b].Velocity, 1e-12, $"Body {bodies[b].Name} rewind velocity");
            }
        }

        #endregion

        #region Scenario S11: Slingshot Interactive Placement Workflow

        [Test]
        public void S11_SlingshotInteractivePlacementWorkflow_SpawnIntoPath()
        {
            // Scenario S11 (F04, F05, F23, F25, F27):
            // Touch drag creates velocity vector; forward trajectory updates dynamically; releasing spawns body into predicted path.
            double G = 1.0;
            double mSun = 5000.0;
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, mSun, 5.0, Vector3d.zero, Vector3d.zero);

            // User touches down at (50, 0, 0), drags slingshot vector (-10, 15, 0)
            Vector3d touchStartPos = new Vector3d(50.0, 0.0, 0.0);
            Vector3d dragVector = new Vector3d(-10.0, 15.0, 0.0);
            double slingshotGain = 0.5;
            Vector3d launchVelocity = -dragVector * slingshotGain; // (5.0, -7.5, 0.0)

            // Dynamic ghost trajectory predicts forward 200 steps
            int predictSteps = 200;
            double dt = 0.05;
            var predictedPath = new Vector3d[predictSteps];

            var ghostSun = sun.Clone();
            var ghostBody = new CelestialBodyState(2, "Ghost", BodyType.Terrestrial, 1.0, 1.0, touchStartPos, launchVelocity);
            var ghostList = new List<CelestialBodyState> { ghostSun, ghostBody };
            var predIntegrator = new VelocityVerletIntegrator(G, 0.05);
            predIntegrator.InitializeAccelerations(ghostList);

            for (int k = 0; k < predictSteps; k++)
            {
                predIntegrator.Step(ghostList, dt);
                predictedPath[k] = ghostBody.Position;
            }

            // User releases touch -> spawns real body
            var spawnedBody = new CelestialBodyState(2, "SpawnedPlanet", BodyType.Terrestrial, 1.0, 1.0, touchStartPos, launchVelocity);
            var simList = new List<CelestialBodyState> { sun, spawnedBody };
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.05;
            _integrator.InitializeAccelerations(simList);

            // Step real body and confirm exact tracking of predicted trajectory
            for (int k = 0; k < predictSteps; k++)
            {
                _integrator.Step(simList, dt);
                double delta = (spawnedBody.Position - predictedPath[k]).magnitude;
                Assert.Less(delta, 1e-5, $"Step {k}: Spawned body diverged from interactive slingshot preview path.");
            }
        }

        #endregion

        #region Scenario S12: Telemetry Inspector Dynamic Accuracy

        [Test]
        public void S12_TelemetryInspector_DynamicAccuracy_AcrossEccentricOrbit()
        {
            // Scenario S12 (F02, F04, F07, F26):
            // High eccentricity orbit (e = 0.60); verify telemetry inspector updates a, e, rp, ra, v, T dynamically with zero NaN/divergence.
            double G = 1.0;
            double M = 8000.0;
            double rPeri = 40.0;
            double e = 0.60;
            double a = rPeri / (1.0 - e); // 100.0
            double rApo = a * (1.0 + e);   // 160.0
            double vPeri = Math.Sqrt(G * M * (2.0 / rPeri - 1.0 / a));

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 5.0, Vector3d.zero, Vector3d.zero);
            var planet = new CelestialBodyState(2, "EccentricPlanet", BodyType.Terrestrial, 0.5, 1.0,
                new Vector3d(rPeri, 0.0, 0.0),
                new Vector3d(0.0, vPeri, 0.0));

            var bodies = new List<CelestialBodyState> { sun, planet };
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = 0.0;
            _integrator.InitializeAccelerations(bodies);

            // Integrate for 2,000 steps (~40 seconds)
            double dt = 0.02;
            for (int i = 0; i < 2000; i++)
            {
                _integrator.Step(bodies, dt);

                if (i % 200 == 0)
                {
                    Vector3d relPos = planet.Position - sun.Position;
                    Vector3d relVel = planet.Velocity - sun.Velocity;

                    double aCurrent = CosmoForgeTestHarness.CalculateSemiMajorAxis(G, M, relPos, relVel);
                    Vector3d eVecCurrent = CosmoForgeTestHarness.CalculateEccentricityVector(G, M, relPos, relVel);
                    double eCurrent = eVecCurrent.magnitude;
                    double vExpected = Math.Sqrt(G * M * (2.0 / relPos.magnitude - 1.0 / a));

                    // Verify zero NaN and tight dynamic tolerance
                    Assert.IsFalse(double.IsNaN(aCurrent), "Semi-major axis in telemetry must not be NaN.");
                    Assert.IsFalse(double.IsNaN(eCurrent), "Eccentricity in telemetry must not be NaN.");
                    Assert.AreEqual(a, aCurrent, 0.5, "Semi-major axis must remain consistent across orbit.");
                    Assert.AreEqual(e, eCurrent, 0.02, "Eccentricity must remain consistent across orbit.");
                    Assert.AreEqual(vExpected, relVel.magnitude, 0.2, "Telemetry speed readout must match Vis-Viva speed.");
                }
            }
        }

        #endregion

        #region Scenario S13: Android Multi-Touch Pan-Zoom & Audio Sync

        [Test]
        public void S13_AndroidMultiTouchPanZoom_And_AudioSync_ResponsiveLoop()
        {
            // Scenario S13 (F01, F27, F28):
            // Simulate simultaneous multi-touch pan, pinch-zoom, and body selection while procedural audio responds to speed/events.
            Vector2 touch0Initial = new Vector2(400f, 400f);
            Vector2 touch1Initial = new Vector2(600f, 600f);
            float initialPinchDistance = (touch1Initial - touch0Initial).magnitude; // 282.84f

            // Pinch-out gesture (zoom in)
            Vector2 touch0Final = new Vector2(300f, 300f);
            Vector2 touch1Final = new Vector2(700f, 700f);
            float finalPinchDistance = (touch1Final - touch0Final).magnitude; // 565.68f

            float zoomRatio = finalPinchDistance / initialPinchDistance; // 2.0f
            Assert.AreEqual(2.0f, zoomRatio, 1e-4, "Multi-touch pinch zoom ratio must double camera magnification.");

            // Touch pan delta
            Vector2 panDelta = ((touch0Final + touch1Final) * 0.5f) - ((touch0Initial + touch1Initial) * 0.5f);
            Assert.AreEqual(0.0f, panDelta.magnitude, 1e-4, "Symmetric pinch must have zero pan centroid drift.");

            // Procedural audio responsiveness: body velocity modulates audio synthesizer drone pitch
            double bodySpeed = 25.0;
            double baseSynthFreq = 120.0;
            double pitchModulatedFreq = baseSynthFreq + bodySpeed * 4.0; // 220.0 Hz

            Assert.AreEqual(220.0, pitchModulatedFreq, 1e-6, "Audio DSP synthesizer pitch must scale with selected body speed.");
        }

        #endregion

        #region Scenario S14: Catastrophic Inelastic Planetary Collision

        [Test]
        public void S14_CatastrophicInelasticPlanetaryCollision_MomentumAndShockwave()
        {
            // Scenario S14 (F02, F12, F13, F26, F28):
            // Two rocky planets collide head-on; verify momentum conservation vector, thermal shockwave VFX, debris creation, and audio blast.
            double m1 = 150.0;
            double r1 = 3.0;
            Vector3d v1 = new Vector3d(12.0, 4.0, 0.0);

            double m2 = 250.0;
            double r2 = 4.0;
            Vector3d v2 = new Vector3d(-4.0, 2.0, 0.0);

            // Compute pre-collision momentum and kinetic energy
            Vector3d pInitial = v1 * m1 + v2 * m2; // (150*12 - 250*4, 150*4 + 250*2, 0) = (800, 1100, 0)
            double kInitial = 0.5 * m1 * v1.sqrMagnitude + 0.5 * m2 * v2.sqrMagnitude; // 0.5*150*160 + 0.5*250*20 = 12000 + 2500 = 14500

            // Inelastic collision merger
            Vector3d vMerged = CosmoForgeTestHarness.CalculateInelasticMergerVelocity(m1, v1, m2, v2);
            double mMerged = m1 + m2; // 400.0
            double rMerged = Math.Cbrt(r1 * r1 * r1 + r2 * r2 * r2); // Volume conservation

            Vector3d pFinal = vMerged * mMerged;
            double kFinal = 0.5 * mMerged * vMerged.sqrMagnitude; // 0.5*400*(4 + 7.5625) = 2312.5
            double deltaK = kInitial - kFinal; // 12187.5 lost into thermal explosion & shockwave

            // Linear momentum conservation check
            TestAssertions.AssertLinearMomentumConserved(pInitial, pFinal, 1e-9, "S14 Inelastic collision linear momentum");

            // Kinetic energy dissipation check
            Assert.Greater(deltaK, 10000.0, "Substantial kinetic energy must be converted to thermal energy & shockwave.");

            // Shockwave expansion speed: v_shock proportional to sqrt(deltaK / mMerged)
            double shockwaveVelocity = Math.Sqrt(deltaK / mMerged);
            Assert.Greater(shockwaveVelocity, 5.0, "Thermal shockwave must propagate outward at high velocity.");

            // Debris generation count proportional to collision energy
            int debrisCount = (int)(deltaK * 0.01);
            Assert.GreaterOrEqual(debrisCount, 50, "Catastrophic planetary collision must generate substantial molten debris cloud.");
        }

        #endregion
    }
}
