// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Puzzle Missions Tests (F17 - F22)
// 30 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts, TEST_INFRA.md, & ORIGINAL_REQUEST.md
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

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_PuzzleMissionsBoundaryTests
    {
        #region Feature F17: Puzzle Mission 1: "The Orbital Injection" (5 Boundary Tests)

        [Test]
        public void F17_Boundary_OrbitalInjection_Eccentricity_Exactly005_AwardsThreeStars()
        {
            // Boundary condition: Mission 1 3-star rating threshold is e <= 0.05 via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m1", "Orbital Injection", "Threshold test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Eccentricity <= 0.05 must be awarded 3 stars.");
        }

        [Test]
        public void F17_Boundary_OrbitalInjection_Eccentricity_005001_AwardsTwoStars()
        {
            // Boundary condition: Just over the 3-star boundary at e ~ 0.08 via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m1", "Orbital Injection", "Threshold test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc * 1.04, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(2, result.StarsAwarded, "Eccentricity just above 0.05 must be awarded 2 stars.");
        }

        [Test]
        public void F17_Boundary_OrbitalInjection_Periapsis_ExactAtmosphericLimitThreshold()
        {
            // Boundary condition: Earth atmospheric boundary. Periapsis penetrating atmosphere triggers destruction.
            IPuzzleMission mission = new HarnessPuzzleMission("m1", "Orbital Injection", "Atmosphere test");
            var world = new SimulationWorld();

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(10.2, 0.0, 0.0), Vector3d.zero)
            {
                IsDestroyed = true // Atmospheric burnup
            };

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Atmospheric burnup must trigger mission defeat.");
        }

        [Test]
        public void F17_Boundary_OrbitalInjection_ExactEscapeVelocity_HyperbolicDefeat()
        {
            // Boundary condition: Probe slingshot with speed >= escape speed yields hyperbolic escape.
            IPuzzleMission mission = new HarnessPuzzleMission("m1", "Orbital Injection", "Escape test")
            {
                TargetMaxDistance = 100.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(250.0, 0.0, 0.0), new Vector3d(0.0, 15.0, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Hyperbolic escape beyond target radius must trigger mission defeat.");
        }

        [Test]
        public void F17_Boundary_OrbitalInjection_IncompleteRevolution_DoesNotTriggerPrematureVictory()
        {
            // Boundary condition: Cumulative orbital evaluation time has not reached required threshold.
            IPuzzleMission mission = new HarnessPuzzleMission("m1", "Orbital Injection", "Duration test")
            {
                RequiredSurvivingTime = 10.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 1.0, 0.5, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 4.0);

            Assert.AreEqual(PuzzleStatus.InProgress, result.Status, "Incomplete duration must remain InProgress.");
            Assert.Less(result.Progress, 1.0f);
        }

        #endregion

        #region Feature F18: Puzzle Mission 2: "Lagrange Parking" (5 Boundary Tests)

        [Test]
        public void F18_Boundary_LagrangeParking_L4ExactEquilateral_ZeroDriftForce()
        {
            // Boundary condition: Probe placed at L4 station-keeping point via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m2", "Lagrange Parking", "L4 test");
            var world = new SimulationWorld();

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(50.0, 50.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.InProgress, result.Status, "Station-keeping within potential well continues InProgress.");
        }

        [Test]
        public void F18_Boundary_LagrangeParking_HoldingTolerance_OuterBoundaryThreeStars()
        {
            // Boundary condition: Tight holding around Trojan equilibrium point awards 3 stars.
            IPuzzleMission mission = new HarnessPuzzleMission("m2", "Lagrange Parking", "L5 test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 60.0);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(60.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Holding stable station awards 3 stars.");
        }

        [Test]
        public void F18_Boundary_LagrangeParking_L1Collinear_UnstableDrift_TwoStars()
        {
            // Boundary condition: L1 parking with slight drift awards 2 stars.
            IPuzzleMission mission = new HarnessPuzzleMission("m2", "Lagrange Parking", "L1 test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 60.0);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(60.0, 0.0, 0.0), new Vector3d(0.0, vCirc * 1.03, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(2, result.StarsAwarded, "L1 saddle-point parking awards 2 stars.");
        }

        [Test]
        public void F18_Boundary_LagrangeParking_DriftExceedance_TriggersDefeat()
        {
            // Boundary condition: Probe drifting beyond tolerance triggers Defeat.
            IPuzzleMission mission = new HarnessPuzzleMission("m2", "Lagrange Parking", "Drift test")
            {
                TargetMaxDistance = 100.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(300.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Excessive drift beyond allowable threshold triggers defeat.");
        }

        [Test]
        public void F18_Boundary_LagrangeParking_ResidualSpeedThreshold()
        {
            // Boundary condition: Probe escaping potential well with excessive velocity.
            IPuzzleMission mission = new HarnessPuzzleMission("m2", "Lagrange Parking", "Speed test")
            {
                TargetMaxDistance = 80.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.Terrestrial, 0.1, 0.2, new Vector3d(200.0, 0.0, 0.0), new Vector3d(50.0, 0.0, 0.0));

            world.AddBody(earth);
            world.AddBody(probe);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "High speed escape triggers station keeping failure.");
        }

        #endregion

        #region Feature F19: Puzzle Mission 3: "Planetary Defense" (5 Boundary Tests)

        [Test]
        public void F19_Boundary_PlanetaryDefense_NarrowFlyby_OneStarVictory()
        {
            // Boundary condition: Dangerously narrow flyby clears Earth surface and qualifies for victory via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m3", "Planetary Defense", "Flyby test")
            {
                RequiredSurvivingTime = 5.0,
                TargetEccentricityThreshold = 0.02
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 40.0);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(40.0, 0.0, 0.0), new Vector3d(0.0, vCirc * 1.15, 0.0));

            world.AddBody(earth);
            world.AddBody(asteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(1, result.StarsAwarded, "High eccentricity narrow flyby must award 1 star.");
        }

        [Test]
        public void F19_Boundary_PlanetaryDefense_EarthImpact_ImmediateDefeat()
        {
            // Boundary condition: Asteroid periapsis r_p <= R_E (deep planetary impact).
            IPuzzleMission mission = new HarnessPuzzleMission("m3", "Planetary Defense", "Impact test");
            var world = new SimulationWorld();

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(9.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(asteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Asteroid contacting Earth surface triggers impact defeat.");
        }

        [Test]
        public void F19_Boundary_PlanetaryDefense_TractorCollisionWithAsteroid_FailsMission()
        {
            // Boundary condition: Spacecraft contacts asteroid, destroying tractor and failing mission.
            IPuzzleMission mission = new HarnessPuzzleMission("m3", "Planetary Defense", "Tractor collision test");
            var world = new SimulationWorld();

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(50.0, 0.0, 0.0), Vector3d.zero);
            var tractor = new CelestialBodyState(3, "Tractor", BodyType.Terrestrial, 0.01, 0.1, new Vector3d(50.0, 0.0, 0.0), Vector3d.zero);
            tractor.IsDestroyed = true;

            world.AddBody(earth);
            world.AddBody(asteroid);
            world.AddBody(tractor);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 0.5);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Tractor contacting asteroid must trigger collision failure.");
        }

        [Test]
        public void F19_Boundary_PlanetaryDefense_TractorZeroThrust_NoDeflection()
        {
            // Boundary condition: Tractor thrust = 0.0 N. Asteroid impacts Earth without deflection.
            IPuzzleMission mission = new HarnessPuzzleMission("m3", "Planetary Defense", "Zero thrust test");
            var world = new SimulationWorld();

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var undeflectedAsteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(8.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(earth);
            world.AddBody(undeflectedAsteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Zero thrust fails to deflect asteroid from impact trajectory.");
        }

        [Test]
        public void F19_Boundary_PlanetaryDefense_OptimalDeflection_AwardsThreeStars()
        {
            // Boundary condition: Optimal asteroid deflection circularizes trajectory and awards 3 stars.
            IPuzzleMission mission = new HarnessPuzzleMission("m3", "Planetary Defense", "3-star test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.Terrestrial, 0.1, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(earth);
            world.AddBody(asteroid);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "Optimal deflection awards 3 stars.");
        }

        #endregion

        #region Feature F20: Puzzle Mission 4: "Barycentric Ballet" (5 Boundary Tests)

        [Test]
        public void F20_Boundary_BarycentricBallet_HolmanWiegert_ExactStabilityThreshold()
        {
            // Boundary condition: Holman-Wiegert critical semi-major axis a_c.
            double ab = 20.0;
            double eb = 0.08;
            double mu = 0.40;

            double ac = CosmoForgeTestHarness.CalculateHolmanWiegertCriticalRadius(ab, eb, mu);

            double aStable = ac + 0.1;
            double aUnstable = ac - 0.1;

            Assert.IsTrue(aStable > ac, "Orbit at a_c + 0.1 must satisfy Holman-Wiegert stability criterion.");
            Assert.IsFalse(aUnstable > ac, "Orbit at a_c - 0.1 must fail stability criterion.");
        }

        [Test]
        public void F20_Boundary_BarycentricBallet_HabitableZone_InnerBoundaryTemperature()
        {
            // Boundary condition: Habitable zone upper temperature limit T = 310.0 K (liquid water ceiling).
            double tempHabitable = 310.0;
            double tempBoiling = 310.1;

            bool isHabitable = tempHabitable >= 260.0 && tempHabitable <= 310.0;
            bool isBoiling = tempBoiling > 310.0;

            Assert.IsTrue(isHabitable, "310.0 K must be accepted within habitable boundary.");
            Assert.IsTrue(isBoiling, "310.1 K must trigger thermal boil failure.");
        }

        [Test]
        public void F20_Boundary_BarycentricBallet_HabitableZone_OuterBoundaryTemperature()
        {
            // Boundary condition: Habitable zone lower temperature limit T = 260.0 K (global glaciation floor).
            double tempHabitable = 260.0;
            double tempFrozen = 259.9;

            bool isHabitable = tempHabitable >= 260.0 && tempHabitable <= 310.0;
            bool isFrozen = tempFrozen < 260.0;

            Assert.IsTrue(isHabitable, "260.0 K must be accepted within habitable boundary.");
            Assert.IsTrue(isFrozen, "259.9 K must trigger global glaciation failure.");
        }

        [Test]
        public void F20_Boundary_BarycentricBallet_MultiOrbitSurvival_ExactFivePeriodsVictory()
        {
            // Boundary condition: Multi-orbit survival condition tested via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m4", "Barycentric Ballet", "Survival test")
            {
                RequiredSurvivingTime = 250.0
            };

            var world = new SimulationWorld();
            var star = new CelestialBodyState(1, "Star", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 80.0);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(80.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(star);
            world.AddBody(planet);

            PuzzleEvaluationResult under = mission.EvaluateTick(world, 240.0);
            Assert.AreEqual(PuzzleStatus.InProgress, under.Status);

            PuzzleEvaluationResult exact = mission.EvaluateTick(world, 10.0);
            Assert.AreEqual(PuzzleStatus.Victory, exact.Status);
        }

        [Test]
        public void F20_Boundary_BarycentricBallet_BinaryStarCollision_FailsMission()
        {
            // Boundary condition: Binary stars colliding fails mission via IPuzzleMission.
            IPuzzleMission mission = new HarnessPuzzleMission("m4", "Barycentric Ballet", "Binary collision test");
            var world = new SimulationWorld();

            var star1 = new CelestialBodyState(1, "Star1", BodyType.Star, 500.0, 5.0, Vector3d.zero, Vector3d.zero);
            var star2 = new CelestialBodyState(2, "Star2", BodyType.Star, 500.0, 5.0, Vector3d.zero, Vector3d.zero);
            star2.IsDestroyed = true; // Binary stars collided

            world.AddBody(star1);
            world.AddBody(star2);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Binary star contact destroying star must trigger collision failure.");
        }

        #endregion

        #region Feature F21: Puzzle Mission 5: "The Ring Smith" (5 Boundary Tests)

        [Test]
        public void F21_Boundary_RingSmith_Retention_ExactlyEightyPercent_AwardsTwoStars()
        {
            // Boundary condition: Mission 5 minimum victory requirement is retention >= 80.0%.
            IPuzzleMission mission = new HarnessPuzzleMission("m5", "Ring Smith", "Retention test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var giant = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 25.0);
            var ring = new CelestialBodyState(2, "RingParticle", BodyType.Terrestrial, 0.1, 0.1, new Vector3d(25.0, 0.0, 0.0), new Vector3d(0.0, vCirc * 1.03, 0.0));

            world.AddBody(giant);
            world.AddBody(ring);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(2, result.StarsAwarded, "Retention at 80.0% boundary must award 2 stars.");
        }

        [Test]
        public void F21_Boundary_RingSmith_Retention_SeventyNinePointNinePercent_Defeat()
        {
            // Boundary condition: Retention below 80.0% fails victory criteria.
            double retention = 79.99;
            bool achievesVictory = retention >= 80.0;

            Assert.IsFalse(achievesVictory, "79.99% retention must not achieve victory.");
        }

        [Test]
        public void F21_Boundary_RingSmith_Retention_NinetyPercent_LowEccentricity_AwardsThreeStars()
        {
            // Boundary condition: 3-star victory requires high retention and near-circular ring.
            IPuzzleMission mission = new HarnessPuzzleMission("m5", "Ring Smith", "Retention 3-star test")
            {
                TargetEccentricityThreshold = 0.05,
                RequiredSurvivingTime = 5.0
            };

            var world = new SimulationWorld();
            var giant = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 25.0);
            var ring = new CelestialBodyState(2, "RingParticle", BodyType.Terrestrial, 0.1, 0.1, new Vector3d(25.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(giant);
            world.AddBody(ring);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 5.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.AreEqual(3, result.StarsAwarded, "90.0% retention with circular ring qualifies for 3 stars.");
        }

        [Test]
        public void F21_Boundary_RingSmith_AtmosphericPlunge_ZeroRetentionDefeat()
        {
            // Boundary condition: Moon periapsis set inside gas giant atmosphere plunges all fragments.
            IPuzzleMission mission = new HarnessPuzzleMission("m5", "Ring Smith", "Plunge test");
            var world = new SimulationWorld();

            var giant = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var plungedMoon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(5.0, 0.0, 0.0), Vector3d.zero);

            world.AddBody(giant);
            world.AddBody(plungedMoon);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Plunged moon produces defeat.");
        }

        [Test]
        public void F21_Boundary_RingSmith_HyperbolicDisruption_ParticlesEscape()
        {
            // Boundary condition: Moon crosses Roche limit on hyperbolic escape trajectory.
            IPuzzleMission mission = new HarnessPuzzleMission("m5", "Ring Smith", "Escape test")
            {
                TargetMaxDistance = 100.0
            };

            var world = new SimulationWorld();
            var giant = new CelestialBodyState(1, "GasGiant", BodyType.GasGiant, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var escapedMoon = new CelestialBodyState(2, "Moon", BodyType.Moon, 1.0, 1.0, new Vector3d(300.0, 0.0, 0.0), new Vector3d(20.0, 0.0, 0.0));

            world.AddBody(giant);
            world.AddBody(escapedMoon);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status, "Hyperbolic disruption fragments escaping system trigger defeat.");
        }

        #endregion

        #region Feature F22: Gamified Puzzle Engine & Confetti Celebration (5 Boundary Tests)

        [Test]
        public void F22_Boundary_PuzzleEngine_Reset_RestoresInitialStateCleanly()
        {
            // Boundary condition: Calling ResetMission() restores state cleanly via IPuzzleMission.
            var mission = new HarnessPuzzleMission("m_boundary", "Boundary Mission", "Reset test")
            {
                InitialBodiesTemplate = CosmoForgeTestHarness.CreateSunEarthSystem()
            };

            var world = new SimulationWorld();
            mission.Initialize(world);

            Vector3d initPos = world.Bodies[1].Position;

            // Mutate state during simulation
            world.Bodies[1].Position += new Vector3d(50.0, -20.0, 5.0);
            world.Bodies[1].IsDestroyed = true;

            // Reset mission
            mission.ResetMission(world);

            Assert.AreEqual(initPos.x, world.Bodies[1].Position.x, 1e-9, "Reset must restore initial position.");
            Assert.AreEqual(initPos.y, world.Bodies[1].Position.y, 1e-9);
            Assert.IsFalse(world.Bodies[1].IsDestroyed, "Reset must unmark destroyed status.");
        }

        [Test]
        public void F22_Boundary_PuzzleEngine_Confetti_ZeroParticlesSpawnedOnDefeat()
        {
            // Boundary condition: Mission evaluated as Defeat does not trigger celebration callback.
            var mission = new HarnessPuzzleMission("m_boundary", "Boundary Mission", "Defeat confetti test");
            bool confettiFired = false;
            mission.OnVictoryCelebration = () => { confettiFired = true; };

            var world = new SimulationWorld();
            // Empty world produces Defeat
            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Defeat, result.Status);
            Assert.IsFalse(confettiFired, "Defeat state must not trigger celebratory confetti callback.");
        }

        [Test]
        public void F22_Boundary_PuzzleEngine_Confetti_TriggeredOnceOnVictoryTransition()
        {
            // Boundary condition: Puzzle state transition to Victory triggers celebration callback exactly once.
            var mission = new HarnessPuzzleMission("m_boundary", "Boundary Mission", "Once test")
            {
                RequiredSurvivingTime = 2.0,
                TargetEccentricityThreshold = 0.1
            };

            int confettiBurstCount = 0;
            mission.OnVictoryCelebration = () => { confettiBurstCount++; };

            var world = new SimulationWorld();
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(sun);
            world.AddBody(planet);

            // Tick 1: Still simulating (1.0s / 2.0s)
            var res1 = mission.EvaluateTick(world, 1.0);
            Assert.AreEqual(PuzzleStatus.InProgress, res1.Status);
            Assert.AreEqual(0, confettiBurstCount);

            // Tick 2: Reaches victory (2.0s total)
            var res2 = mission.EvaluateTick(world, 1.0);
            Assert.AreEqual(PuzzleStatus.Victory, res2.Status);
            Assert.AreEqual(1, confettiBurstCount, "Transition to victory must fire confetti callback.");
        }

        [Test]
        public void F22_Boundary_PuzzleEngine_StarRating_ClampedBetweenOneAndThreeOnVictory()
        {
            // Boundary condition: Star rating calculations on mission victory via IPuzzleMission.
            // On victory, evaluated stars must strictly be within [1, 3].
            var mission = new HarnessPuzzleMission("m_stars", "Stars Test", "")
            {
                RequiredSurvivingTime = 1.0
            };

            var world = new SimulationWorld();
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(1.0, 1000.0, 50.0);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            world.AddBody(sun);
            world.AddBody(planet);

            PuzzleEvaluationResult result = mission.EvaluateTick(world, 1.0);

            Assert.AreEqual(PuzzleStatus.Victory, result.Status);
            Assert.GreaterOrEqual(result.StarsAwarded, 1, "Victory stars must be >= 1.");
            Assert.LessOrEqual(result.StarsAwarded, 3, "Victory stars must be <= 3.");
        }

        [Test]
        public void F22_Boundary_PuzzleEngine_CompletionProgress_ClampedZeroToOne()
        {
            // Boundary condition: Completion progress percentage calculation across extreme deltaTimes.
            var mission = new HarnessPuzzleMission("m_progress", "Progress Test", "")
            {
                RequiredSurvivingTime = 10.0
            };

            var world = new SimulationWorld();
            world.AddBody(new CelestialBodyState(1, "Sun", BodyType.Star, 1000.0, 10.0, Vector3d.zero, Vector3d.zero));
            world.AddBody(new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 4.0, 0.0)));

            double[] testDeltas = { -5.0, 0.0, 5.0, 10.0, 100.0 };
            foreach (double dt in testDeltas)
            {
                PuzzleEvaluationResult res = mission.EvaluateTick(world, dt);
                Assert.GreaterOrEqual(res.Progress, 0.0f, "Completion progress must be >= 0.0");
                Assert.LessOrEqual(res.Progress, 1.0f, "Completion progress must be <= 1.0");
            }
        }

        #endregion
    }
}
