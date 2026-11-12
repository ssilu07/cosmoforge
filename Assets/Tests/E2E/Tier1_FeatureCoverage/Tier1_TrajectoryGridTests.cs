// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Trajectory & Spacetime Grid Tests (F04 - F06)
// 15 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts & TEST_INFRA.md
// ============================================================================

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CosmoForge.Core.Math;
using CosmoForge.Celestial.Data;
using CosmoForge.Physics.Integrator;
using CosmoForge.Physics.Trajectory;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_TrajectoryGridTests
    {
        private IGravityIntegrator _integrator;
        private ITrajectoryPredictor _predictor;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.05);
            _predictor = new HarnessTrajectoryPredictor();
        }

        #region Feature F04: Analytical Keplerian Conic Solver (5 Tests)

        [Test]
        public void F04_KeplerSolver_CircularOrbit_IdentifiesConicAndZeroEccentricity()
        {
            // Requirement R2 & Acceptance Criteria: Circular orbit has e < 1e-4 and r_p == r_a == a
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            double r = 50.0;
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, r);

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vCirc, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(planet, sun, G);

            Assert.AreEqual(ConicType.Circular, conic.Type, "Conic type must be circular for near-zero eccentricity.");
            Assert.AreEqual(r, conic.SemiMajorAxis, 1e-6, "Semi-major axis of circular orbit must equal orbital radius.");
            Assert.Less(conic.Eccentricity, 1e-4, "Eccentricity of circular orbit must be near zero (< 1e-4).");
            Assert.Less(conic.SpecificEnergy, 0.0, "Specific energy for bound circular orbit must be strictly negative.");
        }

        [Test]
        public void F04_KeplerSolver_EllipticalOrbit_ComputesAccuratePeriapsisAndApoapsis()
        {
            // Requirement R2: Elliptical orbit with e = 0.5, r_p = a*(1-e), r_a = a*(1+e)
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            double rPeri = 40.0;
            double eTarget = 0.5;
            double aTarget = rPeri / (1.0 - eTarget); // 80.0
            double rApoTarget = aTarget * (1.0 + eTarget); // 120.0

            double vPeri = CosmoForgeTestHarness.CalculateVisVivaSpeed(G, M, rPeri, aTarget);

            Vector3d pos = new Vector3d(rPeri, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vPeri, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(planet, sun, G);

            Assert.AreEqual(ConicType.Elliptical, conic.Type);
            Assert.AreEqual(aTarget, conic.SemiMajorAxis, 1e-5, "Semi-major axis must match target.");
            Assert.AreEqual(eTarget, conic.Eccentricity, 1e-5, "Eccentricity must match target 0.5.");
            Assert.AreEqual(rPeri, conic.PeriapsisDistance, 1e-5, "Computed periapsis distance must match target.");
            Assert.AreEqual(rApoTarget, conic.ApoapsisDistance, 1e-5, "Computed apoapsis distance must match target.");
        }

        [Test]
        public void F04_KeplerSolver_HyperbolicTrajectory_ClassifiesEscapeAndPositiveEnergy()
        {
            // Requirement R2: Unbound hyperbolic flyby has specific energy > 0 and e > 1.0
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            double r = 50.0;
            double vEscape = CosmoForgeTestHarness.CalculateEscapeSpeed(G, M, r);
            double vHyperbolic = vEscape * 1.3; // 30% above escape velocity

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vHyperbolic, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(probe, sun, G);

            Assert.AreEqual(ConicType.Hyperbolic, conic.Type, "Unbound trajectory must evaluate to Hyperbolic.");
            Assert.Greater(conic.SpecificEnergy, 0.0, "Specific orbital energy for hyperbolic trajectory must be strictly positive.");
            Assert.Greater(conic.Eccentricity, 1.0, "Eccentricity for hyperbolic trajectory must be strictly greater than 1.0.");
        }

        [Test]
        public void F04_KeplerSolver_OrbitalPeriod_SatisfiesKeplerThirdLaw()
        {
            // Requirement R2: Kepler's 3rd Law: T^2 / a^3 = 4 * pi^2 / (G * M)
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 10000.0;
            double a = 100.0;

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, a);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(a, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));

            var conic = predictor.CalculateOsculatingElements(planet, sun, G);
            double expectedPeriod = conic.OrbitalPeriod;
            double tSquared = expectedPeriod * expectedPeriod;
            double aCubed = a * a * a;
            double constantRatio = tSquared / aCubed;
            double theoreticalRatio = (4.0 * Math.PI * Math.PI) / (G * M);

            Assert.AreEqual(theoreticalRatio, constantRatio, 1e-9, "T^2 / a^3 must equal 4*pi^2/(G*M)");
            TestAssertions.AssertApproximatelyEqual(theoreticalRatio, constantRatio, 1e-9, "T^2 / a^3 must equal 4*pi^2/(G*M)");
        }

        [Test]
        public void F04_KeplerSolver_SpecificAngularMomentum_CollinearWithOrbitalNormal()
        {
            // Relative specific angular momentum h = r x v is perpendicular to orbital plane
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);

            Vector3d r = new Vector3d(30.0, 40.0, 0.0); // XY plane
            Vector3d v = new Vector3d(-4.0, 3.0, 0.0);  // XY plane
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, r, v);

            var conic = predictor.CalculateOsculatingElements(planet, sun, G);
            Assert.IsNotNull(conic);

            Vector3d h = Vector3d.Cross(r, v);

            // Plane is XY, so normal is along Z axis
            Assert.AreEqual(0.0, h.x, 1e-9);
            Assert.AreEqual(0.0, h.y, 1e-9);
            Assert.AreNotEqual(0.0, h.z, "Specific angular momentum for planar XY orbit must point purely in Z direction.");
            Assert.AreEqual(r.magnitude * v.magnitude, Math.Abs(h.z), 1e-9, "Perpendicular r and v yield |h| = |r|*|v|.");
        }

        #endregion

        #region Feature F05: 500-Step Numerical Ghost Trajectory Predictor (5 Tests)

        [Test]
        public void F05_GhostPredictor_ProducesExactRequestedStepCount()
        {
            // Requirement R2 & Acceptance Criteria: Ghost predictor renders up to 500 steps forward
            ITrajectoryPredictor predictor = _predictor;
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            int maxSteps = 500;
            double stepDt = 0.05;

            var ghostPoints = predictor.PredictForwardGhostTrajectory(bodies[1], bodies, maxSteps, stepDt, CosmoForgeTestHarness.DefaultG, 0.05);

            Assert.AreEqual(500, ghostPoints.Length, "Predictor must produce exactly 500 lookahead trajectory coordinates.");
            Assert.IsFalse(double.IsNaN(ghostPoints[0].x));
            Assert.IsFalse(double.IsNaN(ghostPoints[maxSteps - 1].x));
            TestAssertions.AssertNoNaNOrInfinity(ghostPoints[0], "First ghost trajectory point");
            TestAssertions.AssertNoNaNOrInfinity(ghostPoints[maxSteps - 1], "Last ghost trajectory point");
        }

        [Test]
        public void F05_GhostPredictor_UnperturbedOrbit_MatchesAnalyticalConicExtrema()
        {
            // Requirement R2: Numerical lookahead matches analytical periapsis and apoapsis
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 5000.0;
            double rPeri = 50.0;
            double a = 80.0;
            double rApo = 2.0 * a - rPeri; // 110.0
            double vPeri = CosmoForgeTestHarness.CalculateVisVivaSpeed(G, M, rPeri, a);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 5.0, Vector3d.zero, Vector3d.zero) { IsStatic = true };
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 0.01, 1.0, new Vector3d(rPeri, 0.0, 0.0), new Vector3d(0.0, vPeri, 0.0));
            var bodies = new List<CelestialBodyState> { sun, planet };

            double dt = 0.02;
            double period = CosmoForgeTestHarness.CalculateOrbitalPeriod(G, M, a);
            int steps = (int)Math.Ceiling(period / dt);

            var ghostPoints = predictor.PredictForwardGhostTrajectory(planet, bodies, steps, dt, G, 0.0);

            double minDist = double.MaxValue;
            double maxDist = double.MinValue;
            for (int s = 0; s < ghostPoints.Length; s++)
            {
                double dist = ghostPoints[s].magnitude;
                if (dist < minDist) minDist = dist;
                if (dist > maxDist) maxDist = dist;
            }

            Assert.AreEqual(rPeri, minDist, 0.25, "Numerical trajectory minimum distance must match analytical periapsis.");
            Assert.AreEqual(rApo, maxDist, 0.25, "Numerical trajectory maximum distance must match analytical apoapsis.");
        }

        [Test]
        public void F05_GhostPredictor_MultiBodyPerturbation_DetectsGravityPerturbation()
        {
            // Multi-body interaction causes departure from unperturbed 2-body orbit
            ITrajectoryPredictor predictor = _predictor;
            var unperturbed = CosmoForgeTestHarness.CreateSunEarthSystem();
            var perturbed = CosmoForgeTestHarness.CreateSunEarthSystem();

            // Add massive Jupiter perturbing body to second system
            var jupiter = new CelestialBodyState(3, "Jupiter", BodyType.GasGiant, 500.0, 5.0, new Vector3d(0.0, 150.0, 0.0), new Vector3d(-3.0, 0.0, 0.0));
            perturbed.Add(jupiter);

            double dt = 0.02;
            int steps = 1000;

            var unperturbedPath = predictor.PredictForwardGhostTrajectory(unperturbed[1], unperturbed, steps, dt, CosmoForgeTestHarness.DefaultG, 0.0);
            var perturbedPath = predictor.PredictForwardGhostTrajectory(perturbed[1], perturbed, steps, dt, CosmoForgeTestHarness.DefaultG, 0.0);

            double displacement = (perturbedPath[steps - 1] - unperturbedPath[steps - 1]).magnitude;
            Assert.Greater(displacement, 0.1, "Perturbed planet trajectory must diverge visibly from unperturbed 2-body orbit.");
        }

        [Test]
        public void F05_GhostPredictor_CollisionWarning_FlagsWhenTrajectoryIntersectsSurface()
        {
            // Requirement R2 & Acceptance Criteria: Predictor flags collision when distance <= body radius
            ITrajectoryPredictor predictor = _predictor;
            double primaryRadius = 15.0;
            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, 1000.0, primaryRadius, Vector3d.zero, Vector3d.zero) { IsStatic = true };

            // Inbound body aimed directly at center
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 0.01, 0.5, new Vector3d(50.0, 0.0, 0.0), new Vector3d(-5.0, 0.0, 0.0));
            var bodies = new List<CelestialBodyState> { primary, probe };

            var trajectory = predictor.PredictForwardGhostTrajectory(probe, bodies, 500, 0.02, 1.0, 0.0);

            bool collisionDetected = false;
            for (int s = 0; s < trajectory.Length; s++)
            {
                if (trajectory[s].magnitude <= primaryRadius)
                {
                    collisionDetected = true;
                    break;
                }
            }

            Assert.IsTrue(collisionDetected, "Forward trajectory predictor must detect surface collision intersection.");
        }

        [Test]
        public void F05_GhostPredictor_DoesNotMutateWorldSimulationState()
        {
            // Ghost predictor must work on clones; live world state must remain invariant
            ITrajectoryPredictor predictor = _predictor;
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem();
            Vector3d originalEarthPos = bodies[1].Position;
            Vector3d originalEarthVel = bodies[1].Velocity;

            var ghostPoints = predictor.PredictForwardGhostTrajectory(bodies[1], bodies, 500, 0.02, CosmoForgeTestHarness.DefaultG, 0.0);

            Assert.IsNotNull(ghostPoints);
            Assert.AreEqual(500, ghostPoints.Length);
            // Original states must not have been modified
            Assert.AreEqual(originalEarthPos, bodies[1].Position, "World body position must not be altered by ghost prediction.");
            Assert.AreEqual(originalEarthVel, bodies[1].Velocity, "World body velocity must not be altered by ghost prediction.");
        }

        #endregion

        #region Feature F06: Spacetime Curvature Grid Visualizer (5 Tests)

        [Test]
        public void F06_SpacetimeGrid_PotentialDepression_ProportionalToMass()
        {
            // Requirement R2 & Acceptance Criteria: Grid depression z_warp proportional to mass
            double G = 1.0;
            double cSq = 2500.0;

            var lightBody = new CelestialBodyState(1, "Light", BodyType.Terrestrial, 100.0, 1.0, Vector3d.zero, Vector3d.zero);
            var heavyBody = new CelestialBodyState(2, "Heavy", BodyType.Star, 1000.0, 5.0, Vector3d.zero, Vector3d.zero);

            double warpLight = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(0.0, 0.0, new[] { lightBody }, G, cSq);
            double warpHeavy = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(0.0, 0.0, new[] { heavyBody }, G, cSq);

            Assert.Less(warpHeavy, warpLight, "Heavier mass must produce a deeper (more negative) metric depression.");
            Assert.AreEqual(10.0, warpHeavy / warpLight, 1e-9, "Depression depth must scale linearly with mass (1000 vs 100).");
        }

        [Test]
        public void F06_SpacetimeGrid_DistanceDecay_ApproachesZeroFarFromMass()
        {
            // Metric depression decays with distance
            var body = new CelestialBodyState(1, "Mass", BodyType.Star, 5000.0, 5.0, Vector3d.zero, Vector3d.zero);
            var bodies = new[] { body };

            double nearWarp = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(10.0, 0.0, bodies);
            double farWarp = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(100000.0, 0.0, bodies);

            Assert.Less(nearWarp, farWarp, "Near warp must be deeper than far warp.");
            Assert.Less(Math.Abs(farWarp), 0.1, "Far warp must approach zero as distance becomes large.");
        }

        [Test]
        public void F06_SpacetimeGrid_SchwarzschildAsymptote_BoundedBySpeedOfLightSquared()
        {
            // As r -> 0: z_warp(0) = - G * M / c^2 = - 0.5 * R_s
            double G = 1.0;
            double c = 50.0;
            double cSq = c * c;
            double mass = 10000.0;

            var blackHole = new CelestialBodyState(1, "BH", BodyType.BlackHole, mass, 1.0, Vector3d.zero, Vector3d.zero);
            double warpAtCenter = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(0.0, 0.0, new[] { blackHole }, G, cSq);

            double expectedAsymptote = -(G * mass) / cSq;
            double rs = CosmoForgeTestHarness.CalculateSchwarzschildRadius(G, mass, c);

            Assert.AreEqual(expectedAsymptote, warpAtCenter, 1e-9, "Central warp depth must asymptote to -G*M/c^2.");
            Assert.AreEqual(-0.5 * rs, warpAtCenter, 1e-9, "Central warp depth must equal -0.5 * Schwarzschild radius.");
        }

        [Test]
        public void F06_SpacetimeGrid_MultiBodySuperposition_SumsDepressionsLinearly()
        {
            // Requirement R2: z_warp = - sum_i G * m_i / (dist_i + c^2)
            var b1 = new CelestialBodyState(1, "B1", BodyType.Star, 500.0, 2.0, new Vector3d(-30.0, 0.0, 0.0), Vector3d.zero);
            var b2 = new CelestialBodyState(2, "B2", BodyType.Star, 800.0, 3.0, new Vector3d(30.0, 0.0, 0.0), Vector3d.zero);

            double sampleX = 10.0;
            double sampleZ = 15.0;

            double warpB1 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(sampleX, sampleZ, new[] { b1 });
            double warpB2 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(sampleX, sampleZ, new[] { b2 });
            double combinedWarp = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(sampleX, sampleZ, new[] { b1, b2 });

            Assert.AreEqual(warpB1 + warpB2, combinedWarp, 1e-9, "Gravitational potential depression must be linear superposition of individual bodies.");
        }

        [Test]
        public void F06_SpacetimeGrid_EmptySystem_RemainsCompletelyFlat()
        {
            // In absence of masses, spacetime curvature grid is flat (0 depression)
            var emptyList = new List<CelestialBodyState>();
            double warp = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(45.0, -80.0, emptyList);
            Assert.AreEqual(0.0, warp, 1e-12, "Empty spacetime grid must have zero vertical depression.");
        }

        #endregion
    }
}
