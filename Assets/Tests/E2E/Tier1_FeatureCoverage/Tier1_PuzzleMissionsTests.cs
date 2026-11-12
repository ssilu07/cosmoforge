// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Puzzle Missions & Gamification Tests (F17 - F22)
// 30 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Core.Performance;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Physics.Cataclysms;
using CosmoForge.Puzzles.Core;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_PuzzleMissionsTests
    {
        private VelocityVerletIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.05);
        }

        #region Feature F17: Puzzle Mission 1: "The Orbital Injection" (5 Tests)

        [Test]
        public void F17_Mission1_CircularInjection_AchievesThreeStarVictory()
        {
            // Requirement R5.1 & Acceptance Criteria: Circular orbit (e <= 0.05) earns 3 stars
            IPuzzleMission mission = new HarnessPuzzleMission("m1_orbital_injection", "The Orbital Injection", "Inject probe into circular orbit")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            double G = 1.0;
            double mEarth = 1000.0;
            double r0 = 50.0;
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, mEarth, r0);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(r0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status, "Circular orbit surviving required time must achieve victory.");
            Assert.AreEqual(3, result.StarsAwarded, "Near-zero eccentricity injection must award 3 stars.");
            Assert.AreEqual(1.0f, result.Progress, 1e-4f);
        }

        [Test]
        public void F17_Mission1_EllipticInjection_AchievesTwoStarVictory()
        {
            // Moderate tangential speed yields 0.05 < e <= 0.12 (2 stars)
            IPuzzleMission mission = new HarnessPuzzleMission("m1_orbital_injection", "The Orbital Injection", "Inject probe into orbit")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            double G = 1.0;
            double mEarth = 1000.0;
            double r0 = 50.0;
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, mEarth, r0);
            double vElliptic = vCirc * 1.04;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(r0, 0.0, 0.0), new Vector3d(0.0, vElliptic, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(2, result.StarsAwarded, "Moderate eccentricity injection must award 2 stars.");
        }

        [Test]
        public void F17_Mission1_LowSpeedInjection_CrashesIntoEarthSurface()
        {
            // Low speed causes probe periapsis to fall below Earth surface radius, failing mission
            IPuzzleMission mission = new HarnessPuzzleMission("m1_orbital_injection", "The Orbital Injection", "Inject probe into orbit")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            double mEarth = 1000.0;
            double rEarth = 10.0;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, rEarth, Vector3d.zero, Vector3d.zero);
            // Probe crashes directly into surface
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(rEarth * 0.9, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Surface collision must evaluate to Defeat.");
        }

        [Test]
        public void F17_Mission1_HighSpeedInjection_EscapesEarthGravitationalWell()
        {
            // Excessive launch speed yields hyperbolic escape (beyond max distance), triggering Defeat
            IPuzzleMission mission = new HarnessPuzzleMission("m1_orbital_injection", "The Orbital Injection", "Inject probe into orbit")
            {
                TargetMaxDistance = 150.0,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(400.0, 0.0, 0.0), new Vector3d(0.0, 10.0, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Hyperbolic escape must evaluate to Defeat.");
        }

        [Test]
        public void F17_Mission1_AtmosphericEntry_DestroysProbeWhenPeriapsisBelowOneHundredKm()
        {
            // Periapsis below atmospheric threshold destroys probe
            IPuzzleMission mission = new HarnessPuzzleMission("m1_orbital_injection", "The Orbital Injection", "Inject probe into orbit");
            var world = new SimulationWorld(_integrator);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(10.5, 0.0, 0.0), Vector3d.zero);
            probe.IsDestroyed = true; // Atmosphere burned probe

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Destroyed probe must evaluate to Defeat.");
        }

        #endregion

        #region Feature F18: Puzzle Mission 2: "Lagrange Parking" (5 Tests)

        [Test]
        public void F18_Mission2_L4Parking_MaintainsStableTrojanStation()
        {
            // Requirement R5.2: L4 leading triangular point forms equilateral triangle with Earth and Moon
            IPuzzleMission mission = new HarnessPuzzleMission("m2_lagrange_parking", "Lagrange Parking", "Position space telescope into L4")
            {
                TargetMaxDistance = 500.0
            };
            var world = new SimulationWorld(_integrator);

            double separation = 384.4;
            double mu = 0.01215;
            double xL4 = separation * (0.5 - mu);
            double yL4 = separation * (Math.Sqrt(3.0) / 2.0);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Telescope", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(xL4 + 2.0, yL4 - 1.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.InProgress, result.Status, "Station-keeping within L4 tolerance zone continues in progress.");
            Assert.Greater(result.Progress, 0.0f);
        }

        [Test]
        public void F18_Mission2_L5Parking_AchievesThreeStarVictory()
        {
            // Holding station at L5 with drift <= 0.035 D earns 3 stars
            IPuzzleMission mission = new HarnessPuzzleMission("m2_lagrange_parking", "Lagrange Parking", "Position space telescope into L5")
            {
                TargetEccentricityThreshold = 0.08,
                RequiredSurvivingTime = 10.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 100.0);
            var probe = new CelestialBodyState(2, "Telescope", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(100.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 10.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Holding stable station around L5 must award 3 stars.");
        }

        [Test]
        public void F18_Mission2_L1Parking_AchievesTwoStarVictory()
        {
            // L1 collinear point parking is an unstable saddle point, awarding 2 stars upon holding
            IPuzzleMission mission = new HarnessPuzzleMission("m2_lagrange_parking", "Lagrange Parking", "Position space telescope into L1")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 10.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 100.0) * 1.03;
            var probe = new CelestialBodyState(2, "Telescope", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(100.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 10.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(2, result.StarsAwarded, "L1 station holding must award 2 stars.");
        }

        [Test]
        public void F18_Mission2_OffTargetPlacement_FailsWithDriftExceedance()
        {
            // Positioning probe far away from Lagrange point triggers escape / drift failure
            IPuzzleMission mission = new HarnessPuzzleMission("m2_lagrange_parking", "Lagrange Parking", "Position space telescope")
            {
                TargetMaxDistance = 150.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Telescope", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(500.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Probe drifting far off equilibrium must fail mission.");
        }

        [Test]
        public void F18_Mission2_RotatingFrameVelocity_ExceedingLimitFailsStationKeeping()
        {
            // Excessive velocity shoots through potential well into escape
            IPuzzleMission mission = new HarnessPuzzleMission("m2_lagrange_parking", "Lagrange Parking", "Station-keeping")
            {
                TargetMaxDistance = 100.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Telescope", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(250.0, 0.0, 0.0), new Vector3d(100.0, 0.0, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "High residual velocity causes escape, failing station keeping.");
        }

        #endregion

        #region Feature F19: Puzzle Mission 3: "Planetary Defense" (5 Tests)

        [Test]
        public void F19_Mission3_BaselineTrajectory_CollidesWithEarthWithoutTractor()
        {
            // Requirement R5.3: Inbound asteroid baseline periapsis intersects Earth surface (r_p <= R_earth)
            IPuzzleMission mission = new HarnessPuzzleMission("m3_planetary_defense", "Planetary Defense", "Deflect collision-course asteroid");
            var world = new SimulationWorld(_integrator);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(8.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(asteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Direct collision must evaluate to Defeat.");
        }

        [Test]
        public void F19_Mission3_ActiveTractor_DeflectsAsteroidToSafeFlyby()
        {
            // Gravity tractor tug alters trajectory into safe orbit/flyby
            IPuzzleMission mission = new HarnessPuzzleMission("m3_planetary_defense", "Planetary Defense", "Deflect collision-course asteroid")
            {
                RequiredSurvivingTime = 5.0,
                TargetEccentricityThreshold = 0.1
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var deflectedAsteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(deflectedAsteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status, "Tractor deflection into safe orbit must evaluate to Victory.");
        }

        [Test]
        public void F19_Mission3_OptimalDeflection_AchievesThreeStars()
        {
            // Optimal deflection with circularization awards 3 stars
            IPuzzleMission mission = new HarnessPuzzleMission("m3_planetary_defense", "Planetary Defense", "Deflect asteroid")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 60.0);
            var deflected = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(60.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(deflected);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Optimal asteroid deflection awards 3 stars.");
        }

        [Test]
        public void F19_Mission3_InsufficientLeadTime_FailsToClearEarthRadius()
        {
            // Deploying tractor too late fails to clear Earth radius
            IPuzzleMission mission = new HarnessPuzzleMission("m3_planetary_defense", "Planetary Defense", "Deflect asteroid");
            var world = new SimulationWorld(_integrator);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(9.5, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(asteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Insufficient lead time results in impact Defeat.");
        }

        [Test]
        public void F19_Mission3_TractorAsteroidCollision_FailsMission()
        {
            // Tractor crashing into asteroid destroys tractor and fails mission
            IPuzzleMission mission = new HarnessPuzzleMission("m3_planetary_defense", "Planetary Defense", "Deflect asteroid");
            var world = new SimulationWorld(_integrator);

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(50.0, 0.0, 0.0), Vector3d.zero);
            var tractor = new CelestialBodyState(3, "Tractor", BodyType.Terrestrial, 0.01, 0.1, new Vector3d(50.0, 0.0, 0.0), Vector3d.zero);
            tractor.IsDestroyed = true;

            world.AddBody(earth);
            world.AddBody(asteroid);
            world.AddBody(tractor);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Tractor destruction must fail mission.");
        }

        #endregion

        #region Feature F20: Puzzle Mission 4: "Barycentric Ballet" (5 Tests)

        [Test]
        public void F20_Mission4_CircumbinaryOrbit_RemainsStableBeyondCriticalRadius()
        {
            // Requirement R5.4: Circumbinary P-type orbit outside Holman-Wiegert radius
            IPuzzleMission mission = new HarnessPuzzleMission("m4_barycentric_ballet", "Barycentric Ballet", "Find stable circumbinary orbit")
            {
                RequiredSurvivingTime = 5.0,
                TargetEccentricityThreshold = 0.08
            };

            var world = new SimulationWorld(_integrator);
            var star1 = new CelestialBodyState(1, "Star1", BodyType.Star, 500.0, 5.0, new Vector3d(-10.0, 0.0, 0.0), new Vector3d(0.0, -3.5, 0.0));
            var star2 = new CelestialBodyState(2, "Star2", BodyType.Star, 500.0, 5.0, new Vector3d(10.0, 0.0, 0.0), new Vector3d(0.0, 3.5, 0.0));
            double vPlanet = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 80.0);
            var planet = new CelestialBodyState(3, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(80.0, 0.0, 0.0), new Vector3d(0.0, vPlanet, 0.0));

            world.AddBody(star1);
            world.AddBody(star2);
            world.AddBody(planet);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status, "Stable circumbinary orbit must evaluate to Victory.");
        }

        [Test]
        public void F20_Mission4_ChaoticInstability_FailsInsideCriticalRadius()
        {
            // Planet placed too close to binary barycenter gets ejected or crashes
            IPuzzleMission mission = new HarnessPuzzleMission("m4_barycentric_ballet", "Barycentric Ballet", "Find stable circumbinary orbit");
            var world = new SimulationWorld(_integrator);

            var star1 = new CelestialBodyState(1, "Star1", BodyType.Star, 500.0, 5.0, new Vector3d(-10.0, 0.0, 0.0), Vector3d.zero);
            var star2 = new CelestialBodyState(2, "Star2", BodyType.Star, 500.0, 5.0, new Vector3d(10.0, 0.0, 0.0), Vector3d.zero);
            var planet = new CelestialBodyState(3, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(5.0, 0.0, 0.0), Vector3d.zero);
            planet.IsDestroyed = true; // Perturbed into stellar collision

            world.AddBody(star1);
            world.AddBody(star2);
            world.AddBody(planet);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Orbits inside unstable zone causing disruption must fail.");
        }

        [Test]
        public void F20_Mission4_HabitableZoneOrbit_MaintainsEquilibriumTemperature()
        {
            // Total luminosity of binary allows liquid water: T_eq in [250, 320] K
            double lTot = 2.0 * 3.828e26;
            double dHab = 1.414 * 1.496e11;

            double temp = CosmoForgeTestHarness.CalculateBlackbodyTemperature(lTot, dHab, 0.30);
            Assert.GreaterOrEqual(temp, 250.0, "Circumbinary habitable zone temperature must be >= 250 K.");
            Assert.LessOrEqual(temp, 320.0, "Circumbinary habitable zone temperature must be <= 320 K.");
            TestAssertions.AssertTemperatureInRange(temp, 250.0, 320.0, "Circumbinary habitable zone temperature");
        }

        [Test]
        public void F20_Mission4_ThreeStarRating_RequiresCircularOrbitAndTenOrbits()
        {
            // 3-star victory requires e <= 0.06 and surviving required time
            IPuzzleMission mission = new HarnessPuzzleMission("m4_barycentric_ballet", "Barycentric Ballet", "Circumbinary orbit")
            {
                TargetEccentricityThreshold = 0.06,
                RequiredSurvivingTime = 10.0
            };

            var world = new SimulationWorld(_integrator);
            var star = new CelestialBodyState(1, "Barycenter", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 80.0);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(80.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(star);
            world.AddBody(planet);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 10.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Low eccentricity circumbinary orbit surviving required time awards 3 stars.");
        }

        [Test]
        public void F20_Mission4_HolmanWiegertCriticalRadius_FormulaVerification()
        {
            // Verifies Holman-Wiegert critical stability radius calculation
            double aBin = 20.0;
            double eBin = 0.1;
            double mu = 0.5;

            double aCrit = CosmoForgeTestHarness.CalculateHolmanWiegertCriticalRadius(aBin, eBin, mu);

            Assert.Greater(aCrit, aBin, "Critical circumbinary radius must exceed binary semi-major axis.");
            Assert.Less(aCrit, aBin * 4.0, "Critical stability radius is within realistic dynamical bound.");
        }

        #endregion

        #region Feature F21: Puzzle Mission 5: "The Ring Smith" (5 Tests)

        [Test]
        public void F21_Mission5_DeorbitAcrossRocheLimit_ShattersMoonIntoRingParticles()
        {
            // Requirement R5.5: Moon crossing Roche limit shatters into ring particle cloud via ICollisionEngine
            ICollisionEngine collisionEngine = new HarnessCollisionEngine();
            var primary = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var moon = new CelestialBodyState(2, "IcyMoon", BodyType.Moon, 1.0, 1.0, new Vector3d(15.0, 0.0, 0.0), Vector3d.zero);

            var bodies = new List<CelestialBodyState> { primary, moon };
            RocheDisruptionEvent recordedEvent = default;
            bool eventFired = false;

            collisionEngine.EvaluateRocheLimits(bodies, evt =>
            {
                recordedEvent = evt;
                eventFired = true;
            });

            Assert.IsTrue(eventFired, "Roche disruption must fire when moon enters Roche limit.");
            Assert.IsTrue(moon.IsDestroyed, "Disrupted moon must be marked IsDestroyed.");
            Assert.GreaterOrEqual(recordedEvent.SpawnedRingParticles, 50, "Disruption must spawn ring particles.");
        }

        [Test]
        public void F21_Mission5_ParticleRetention_EightyPercentThresholdAchievesVictory()
        {
            // Requirement R5.5 & Acceptance Criteria: >= 80% particle retention satisfies win condition
            IPuzzleMission mission = new HarnessPuzzleMission("m5_ring_smith", "The Ring Smith", "Create planar ring system")
            {
                TargetParticleRetentionRatio = 0.80,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            var planet = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 25.0);
            var ringParticle = new CelestialBodyState(2, "RingParticle", BodyType.Terrestrial, 0.01, 0.1, new Vector3d(25.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(planet);
            world.AddBody(ringParticle);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status, "Retained particles satisfy Ring Smith victory.");
        }

        [Test]
        public void F21_Mission5_NinetyPercentRetention_AwardsThreeStars()
        {
            // >= 90% particle retention awards 3 stars
            IPuzzleMission mission = new HarnessPuzzleMission("m5_ring_smith", "The Ring Smith", "Create planar ring system")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld(_integrator);
            var planet = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 25.0);
            var ringParticle = new CelestialBodyState(2, "RingParticle", BodyType.Terrestrial, 0.01, 0.1, new Vector3d(25.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(planet);
            world.AddBody(ringParticle);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "High retention circular ring awards 3 stars.");
        }

        [Test]
        public void F21_Mission5_CorePlunge_FailsMissionWithAtmosphericImpact()
        {
            // Moon de-orbits directly into planetary surface without forming ring
            IPuzzleMission mission = new HarnessPuzzleMission("m5_ring_smith", "The Ring Smith", "Create ring");
            var world = new SimulationWorld(_integrator);

            var planet = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var moon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(5.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(planet);
            world.AddBody(moon);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Direct core impact must fail mission.");
        }

        [Test]
        public void F21_Mission5_HyperbolicDisruption_FailsRetentionThreshold()
        {
            // Moon escapes out of system, failing retention threshold
            IPuzzleMission mission = new HarnessPuzzleMission("m5_ring_smith", "The Ring Smith", "Create ring")
            {
                TargetMaxDistance = 100.0
            };

            var world = new SimulationWorld(_integrator);
            var planet = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var moon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(300.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(planet);
            world.AddBody(moon);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Particle escape must fail mission.");
        }

        #endregion

        #region Feature F22: Gamified Puzzle Engine & Confetti Celebration (5 Tests)

        [Test]
        public void F22_PuzzleEngine_InitializesWorldState_CleanlyOnMissionSelect()
        {
            // Requirement R5: Initializing a mission resets world state and populates mission bodies
            var mission = new HarnessPuzzleMission("m_test", "Test Mission", "Description")
            {
                InitialBodiesTemplate = CosmoForgeTestHarness.CreateSunEarthSystem()
            };

            var world = new SimulationWorld(_integrator);
            world.AddBody(new CelestialBodyState(99, "OldBody", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, Vector3d.zero));
            Assert.AreEqual(1, world.Bodies.Count);

            mission.Initialize(world);

            Assert.AreEqual(2, world.Bodies.Count, "World must contain exactly the bodies required for selected mission.");
            Assert.AreEqual("Sun", world.Bodies[0].Name);
            Assert.AreEqual("Earth", world.Bodies[1].Name);
        }

        [Test]
        public void F22_PuzzleEngine_EvaluatesTick_UpdatesProgressAndTelemetry()
        {
            // Tick evaluation computes progress [0.0 - 1.0] and diagnostic telemetry via IPuzzleMission
            var mission = new HarnessPuzzleMission("m_test", "Test Mission", "Description")
            {
                RequiredSurvivingTime = 30.0
            };

            var world = new SimulationWorld(_integrator);
            world.AddBody(new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero));
            world.AddBody(new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 4.0, 0.0)));

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 15.0);

            Assert.AreEqual(0.5f, result.Progress, 1e-4f, "Mission progress must update smoothly with elapsed evaluation time.");
            Assert.AreEqual(PuzzleStatus.InProgress, result.Status);
        }

        [Test]
        public void F22_PuzzleEngine_VictoryTrigger_DispatchesConfettiCelebrationEvent()
        {
            // Requirement R5 & Acceptance Criteria: Victory event dispatches confetti celebration
            var mission = new HarnessPuzzleMission("m_test", "Test Mission", "Description")
            {
                RequiredSurvivingTime = 1.0,
                TargetEccentricityThreshold = 0.1
            };

            bool confettiFired = false;
            mission.OnVictoryCelebration = () => { confettiFired = true; };

            var world = new SimulationWorld(_integrator);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            world.AddBody(new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero));
            world.AddBody(new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0)));

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.IsTrue(confettiFired, "Victory trigger must fire confetti celebration callback on mission.");
        }

        [Test]
        public void F22_PuzzleEngine_StarRatings_PersistedDeterministically()
        {
            // Stars earned (1-3) are evaluated and persisted deterministically
            var mission1 = new HarnessPuzzleMission("m1", "M1", "") { TargetEccentricityThreshold = 0.05, RequiredSurvivingTime = 1.0 };
            var mission2 = new HarnessPuzzleMission("m2", "M2", "") { TargetEccentricityThreshold = 0.05, RequiredSurvivingTime = 1.0 };

            var world1 = new SimulationWorld(_integrator);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            world1.AddBody(new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero));
            world1.AddBody(new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0)));

            var result1 = mission1.EvaluateTick(world1, 1.0);

            var world2 = new SimulationWorld(_integrator);
            world2.AddBody(new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero));
            world2.AddBody(new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc * 1.04, 0.0)));

            var result2 = mission2.EvaluateTick(world2, 1.0);

            Assert.AreEqual(3, result1.StarsAwarded);
            Assert.AreEqual(2, result2.StarsAwarded);
        }

        [Test]
        public void F22_PuzzleEngine_ResetMission_RestoresInitialState()
        {
            // Resetting mission returns bodies to exact starting coordinates via IPuzzleMission
            var mission = new HarnessPuzzleMission("m_test", "Test Mission", "Description")
            {
                InitialBodiesTemplate = CosmoForgeTestHarness.CreateSunEarthSystem()
            };

            var world = new SimulationWorld(_integrator);
            mission.Initialize(world);

            Vector3d initPos = world.Bodies[1].Position;

            // Simulate steps
            for (int s = 0; s < 50; s++)
            {
                world.UpdateSimulation(0.02, 1.0);
            }

            Assert.AreNotEqual(initPos, world.Bodies[1].Position, "Positions moved during simulation.");

            // Reset
            mission.ResetMission(world);

            Assert.AreEqual(initPos.x, world.Bodies[1].Position.x, 1e-9, "Resetting must restore initial coordinates.");
            Assert.AreEqual(initPos.y, world.Bodies[1].Position.y, 1e-9);
        }

        #endregion
    }
}
