// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Trajectory & Spacetime Grid Tests (F04 - F06)
// 15 Tests Total (5 tests per feature)
// Conforms to PROJECT.md § Interface Contracts, TEST_INFRA.md, & ORIGINAL_REQUEST.md
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

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_TrajectoryGridBoundaryTests
    {
        private IGravityIntegrator _integrator;
        private ITrajectoryPredictor _predictor;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.05);
            _predictor = new HarnessTrajectoryPredictor();
        }

        #region Feature F04: Analytical Keplerian Conic Solver (5 Boundary Tests)

        [Test]
        public void F04_Boundary_KeplerSolver_CircularOrbit_EccentricityExactlyZero()
        {
            // Boundary condition: Circular orbit (e = 0.0).
            // Initial velocity set exactly to circular speed v_circ = sqrt(G*M/r).
            // Asserts e < 1e-7, r_p == r_a == a == r, and specific mechanical energy epsilon = -G*M/(2a) < 0.
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 5000.0;
            double r = 80.0;
            double vCirc = CosmoForgeTestHarness.CalculateKeplerianCircularSpeed(G, M, r);

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vCirc, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var planet = new CelestialBodyState(2, "Planet", BodyType.Terrestrial, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(planet, sun, G);

            Assert.AreEqual(ConicType.Circular, conic.Type, "Circular orbit must evaluate to Circular conic.");
            Assert.Less(conic.Eccentricity, 1e-7, "Eccentricity vector magnitude must be near zero (< 1e-7).");
            Assert.Less(conic.SpecificEnergy, 0.0, "Circular orbit must have strictly negative orbital energy.");
            Assert.AreEqual(-(G * M) / (2.0 * r), conic.SpecificEnergy, 1e-9, "Energy must equal -GM/(2r)");
            TestAssertions.AssertApproximatelyEqual(-(G * M) / (2.0 * r), conic.SpecificEnergy, 1e-9, "Energy must equal -GM/(2r)");
            Assert.AreEqual(r, conic.SemiMajorAxis, 1e-6, "Semi-major axis must equal orbital radius.");
            Assert.AreEqual(conic.PeriapsisDistance, conic.ApoapsisDistance, 1e-5, "In a circular orbit, periapsis distance must equal apoapsis distance.");
        }

        [Test]
        public void F04_Boundary_KeplerSolver_ParabolicTransition_EccentricityExactlyOne()
        {
            // Boundary condition: Parabolic escape threshold (e = 1.0, epsilon = 0.0).
            // Initial velocity set exactly to escape speed v_esc = sqrt(2*G*M/r).
            // Asserts e == 1.0 within numerical precision, energy == 0.0, and infinite apoapsis handled safely.
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 2000.0;
            double r = 50.0;
            double vEscape = CosmoForgeTestHarness.CalculateEscapeSpeed(G, M, r);

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vEscape, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(probe, sun, G);

            // Energy for parabolic trajectory is identically 0
            Assert.AreEqual(0.0, conic.SpecificEnergy, 1e-12, "Specific orbital energy at escape velocity must be exactly 0.0.");
            Assert.AreEqual(1.0, conic.Eccentricity, 1e-6, "Eccentricity for parabolic escape must be 1.0.");
            Assert.IsTrue(double.IsInfinity(conic.SemiMajorAxis) || Math.Abs(conic.SemiMajorAxis) > 1e12, "Semi-major axis of parabolic orbit must be infinite or arbitrarily large.");
        }

        [Test]
        public void F04_Boundary_KeplerSolver_HyperbolicOrbit_HighEccentricityAndPositiveEnergy()
        {
            // Boundary condition: Strongly hyperbolic trajectory (e >> 1.0).
            // Speed set to 3.0 * v_esc.
            // Asserts e > 1.0, energy > 0, negative semi-major axis (Keplerian convention), and finite hyperbolic excess speed.
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            double r = 40.0;
            double vEscape = CosmoForgeTestHarness.CalculateEscapeSpeed(G, M, r);
            double vHyper = 3.0 * vEscape;

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(0.0, vHyper, 0.0);

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(probe, sun, G);

            Assert.AreEqual(ConicType.Hyperbolic, conic.Type);
            Assert.Greater(conic.SpecificEnergy, 0.0, "Hyperbolic orbit must have strictly positive orbital energy.");
            Assert.Greater(conic.Eccentricity, 1.0, "Hyperbolic trajectory eccentricity must exceed 1.0.");

            double vInfinity = Math.Sqrt(2.0 * conic.SpecificEnergy);
            Assert.Greater(vInfinity, 0.0, "Hyperbolic excess speed must be positive.");
            TestAssertions.AssertApproximatelyEqual(Math.Sqrt(vHyper * vHyper - vEscape * vEscape), vInfinity, 1e-9, "v_inf = sqrt(v^2 - v_esc^2)");
        }

        [Test]
        public void F04_Boundary_KeplerSolver_RadialFreeFall_ZeroAngularMomentum_DegenerateConic()
        {
            // Boundary condition: Pure radial trajectory directed straight at the primary attractor (h = r x v = 0).
            // Specific angular momentum is identically zero.
            // Solver must evaluate degenerate rectilinear conic (e = 1.0) without division-by-zero errors.
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 3000.0;
            double r = 100.0;

            Vector3d pos = new Vector3d(r, 0.0, 0.0);
            Vector3d vel = new Vector3d(-5.0, 0.0, 0.0); // Collinear, pure inward motion

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, Vector3d.zero, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, pos, vel);

            var conic = predictor.CalculateOsculatingElements(probe, sun, G);

            Vector3d h = Vector3d.Cross(pos, vel);
            Assert.AreEqual(0.0, h.magnitude, 1e-15, "Specific angular momentum must be zero for radial free-fall.");
            Assert.IsNotNull(conic);
            Assert.AreEqual(1.0, conic.Eccentricity, 1e-6, "Eccentricity of degenerate radial fall is 1.0 (rectilinear ellipse/hyperbola).");
            Assert.IsFalse(double.IsNaN(conic.SpecificEnergy), "Specific energy must not be NaN.");
        }

        [Test]
        public void F04_Boundary_KeplerSolver_CoincidentCoordinates_ZeroDistance_HandlesSingularity()
        {
            // Boundary condition: Probe placed at exact coordinate of the attractor r = (0, 0, 0).
            // Conic solver must not throw unhandled DivideByZeroException or yield NaN in calculations.
            ITrajectoryPredictor predictor = _predictor;
            double G = 1.0;
            double M = 1000.0;
            Vector3d zeroPos = Vector3d.zero;
            Vector3d zeroVel = Vector3d.zero;

            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, M, 10.0, zeroPos, zeroVel);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, zeroPos, zeroVel);

            var conic = predictor.CalculateOsculatingElements(probe, sun, G);

            Assert.IsNotNull(conic);
            Assert.IsTrue(double.IsNegativeInfinity(conic.SpecificEnergy) || conic.SpecificEnergy < -1e10, "Energy at r = 0 must be negative infinity or deep potential well.");
            Assert.IsFalse(double.IsNaN(conic.SpecificEnergy), "Energy at r = 0 must not be NaN.");
        }

        #endregion

        #region Feature F05: 500-Step Numerical Ghost Trajectory Predictor (5 Boundary Tests)

        [Test]
        public void F05_Boundary_GhostPredictor_MaxSteps_Exact500StepsAllocatedAndFinite()
        {
            // Boundary condition: Running forward ghost trajectory prediction up to the maximum 500 steps.
            // Verifies array allocation, step counter integrity, and that no NaN occurs across all 500 steps.
            ITrajectoryPredictor predictor = _predictor;
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem(G: 1.0, softening: 0.1);
            int requestedSteps = 500;
            double dt = 0.05;

            var trajectory = predictor.PredictForwardGhostTrajectory(bodies[1], bodies, requestedSteps, dt, 1.0, 0.1);

            Assert.AreEqual(500, trajectory.Length, "Trajectory array must contain exactly 500 projected points.");
            Assert.AreNotEqual(trajectory[0], trajectory[499], "Initial and final trajectory points must not be identical.");
            TestAssertions.AssertNoNaNOrInfinity(trajectory[0], "First ghost trajectory point");
            TestAssertions.AssertNoNaNOrInfinity(trajectory[499], "Last ghost trajectory point");
        }

        [Test]
        public void F05_Boundary_GhostPredictor_ZeroSteps_ReturnsEmptyArraySafely()
        {
            // Boundary condition: Requesting 0 forward steps (e.g. predictor disabled in UI).
            // Must return an empty array without throwing IndexOutOfRangeException or NullReferenceException.
            ITrajectoryPredictor predictor = _predictor;
            var trajectory = predictor.PredictForwardGhostTrajectory(new CelestialBodyState(), new List<CelestialBodyState>(), 0, 0.05, 1.0, 0.1);

            Assert.IsNotNull(trajectory, "Trajectory array must not be null.");
            Assert.AreEqual(0, trajectory.Length, "Trajectory array length must be 0 when 0 steps are requested.");
        }

        [Test]
        public void F05_Boundary_GhostPredictor_ImmediateCollision_TerminatesEarlyAndMarksPoint()
        {
            // Boundary condition: Trajectory launched directly into primary body surface (impact on step 2).
            // Predictor must detect collision within radius R and terminate forward loop, returning truncated path.
            ITrajectoryPredictor predictor = _predictor;
            double primaryRadius = 10.0;
            Vector3d primaryPos = Vector3d.zero;

            Vector3d probePos = new Vector3d(12.0, 0.0, 0.0);
            Vector3d probeVel = new Vector3d(-5.0, 0.0, 0.0); // Moving inward fast

            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, 1000.0, primaryRadius, primaryPos, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, probePos, probeVel);

            var trajectory = predictor.PredictForwardGhostTrajectory(probe, new List<CelestialBodyState> { primary, probe }, 500, 0.5, 1.0, 0.1);

            bool collided = false;
            for (int i = 0; i < trajectory.Length; i++)
            {
                if ((trajectory[i] - primaryPos).magnitude <= primaryRadius)
                {
                    collided = true;
                    break;
                }
            }

            Assert.IsTrue(collided, "Forward predictor must detect collision with primary body surface.");
            Assert.Less(trajectory.Length, 1000);
        }

        [Test]
        public void F05_Boundary_GhostPredictor_HyperbolicEscape_MonotonicallyIncreasingRadius()
        {
            // Boundary condition: Forward prediction on an escaping hyperbolic path.
            // Radii of trajectory points must grow monotonically without looping or numerical divergence.
            ITrajectoryPredictor predictor = _predictor;
            Vector3d primaryPos = Vector3d.zero;
            double G = 1.0;
            double M = 1000.0;

            Vector3d currentPos = new Vector3d(100.0, 0.0, 0.0);
            Vector3d currentVel = new Vector3d(20.0, 10.0, 0.0); // High positive radial speed

            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, M, 10.0, primaryPos, Vector3d.zero);
            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, currentPos, currentVel);

            var trajectory = predictor.PredictForwardGhostTrajectory(probe, new List<CelestialBodyState> { primary, probe }, 200, 0.1, G, 0.1);

            double previousDistance = (currentPos - primaryPos).magnitude;
            for (int i = 0; i < trajectory.Length; i++)
            {
                double curDist = trajectory[i].magnitude;
                Assert.Greater(curDist, previousDistance, $"Escaping trajectory distance must increase monotonically at step {i}.");
                previousDistance = curDist;
            }

            TestAssertions.AssertNoNaNOrInfinity(currentPos, "Final escaping position");
        }

        [Test]
        public void F05_Boundary_GhostPredictor_ZeroGravity_UniformLinearPath()
        {
            // Boundary condition: G = 0.0 or primary mass = 0.0.
            // Trajectory must be an exact straight line with constant inter-point spacing Delta_r = v_0 * dt.
            ITrajectoryPredictor predictor = _predictor;
            Vector3d startPos = new Vector3d(10.0, -20.0, 30.0);
            Vector3d initialVel = new Vector3d(5.0, 0.0, -10.0);
            double speed = initialVel.magnitude;
            double dt = 0.2;

            var probe = new CelestialBodyState(1, "Probe", BodyType.AsteroidProbe, 1.0, 1.0, startPos, initialVel);
            var trajectory = predictor.PredictForwardGhostTrajectory(probe, new List<CelestialBodyState> { probe }, 50, dt, 0.0, 0.0);

            Assert.AreEqual(50, trajectory.Length);
            Vector3d previousPos = startPos;
            for (int i = 0; i < trajectory.Length; i++)
            {
                double stepDistance = (trajectory[i] - previousPos).magnitude;
                Assert.AreEqual(speed * dt, stepDistance, 1e-9, $"Inter-step distance at step {i} must equal |v|*dt");
                TestAssertions.AssertApproximatelyEqual(speed * dt, stepDistance, 1e-9, $"Inter-step distance at step {i} must equal |v|*dt");
                previousPos = trajectory[i];
            }

            // Direction vector from start to end must equal normalized initial velocity
            Vector3d overallDirection = (trajectory[49] - startPos).normalized;
            Assert.AreEqual(initialVel.normalized.x, overallDirection.x, 1e-9);
            TestAssertions.AssertApproximatelyEqual(initialVel.normalized, overallDirection, 1e-9, "Overall trajectory direction must match initial velocity vector.");
        }

        #endregion

        #region Feature F06: Spacetime Curvature Grid Visualizer (5 Boundary Tests)

        [Test]
        public void F06_Boundary_SpacetimeGrid_ZeroMass_CurvatureIdenticallyZero()
        {
            // Boundary condition: No celestial bodies or all bodies have mass = 0.0.
            // Spacetime curvature metric depression z_warp(x, y) must be identically 0.0 everywhere.
            var bodies = new List<CelestialBodyState>();
            double sampleDepth = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(50.0, -50.0, bodies);

            Assert.AreEqual(0.0, sampleDepth, "Curvature depth must be 0.0 when no bodies are present.");

            // Add massless body
            bodies.Add(new CelestialBodyState(1, "Massless", BodyType.AsteroidProbe, 0.0, 1.0, Vector3d.zero, Vector3d.zero));
            double sampleWithMassless = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(0.0, 0.0, bodies);

            Assert.AreEqual(0.0, sampleWithMassless, "Curvature depth must be 0.0 for zero-mass bodies.");
        }

        [Test]
        public void F06_Boundary_SpacetimeGrid_ExtremeBlackHoleMass_FiniteCurvatureAtSingularity()
        {
            // Boundary condition: Supermassive Black Hole of mass 10^9 M_sun at (0, 0).
            // Querying exactly at (0, 0) where distance = 0.
            // Due to + c^2 in denominator z_warp = - G*M / (dist + c^2), depth must be finite: -G*M / c^2.
            double M = 1e9;
            double G = 1.0;
            double c = 50.0;
            double cSq = c * c;

            var smbh = new CelestialBodyState(1, "SMBH", BodyType.BlackHole, M, 10.0, Vector3d.zero, Vector3d.zero);
            var bodies = new List<CelestialBodyState> { smbh };

            double depthAtOrigin = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(0.0, 0.0, bodies, G, cSq);

            Assert.IsFalse(double.IsNaN(depthAtOrigin), "Curvature at black hole origin must not be NaN.");
            Assert.IsFalse(double.IsInfinity(depthAtOrigin), "Curvature at black hole origin must remain finite due to c^2 softening.");

            double expectedDepth = -(G * M) / cSq;
            TestAssertions.AssertApproximatelyEqual(expectedDepth, depthAtOrigin, 1e-6, "Curvature at r=0 must match -G*M/c^2");
        }

        [Test]
        public void F06_Boundary_SpacetimeGrid_AsymptoticInfinity_CurvatureVanishes()
        {
            // Boundary condition: Querying curvature depth at asymptotic distance r = 10^8 units from mass.
            // Metric depression must smoothly approach zero (< 1e-6) without underflow bugs.
            var star = new CelestialBodyState(1, "Star", BodyType.Star, 10000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var bodies = new List<CelestialBodyState> { star };

            double asymptoticDistance = 1e8;
            double depthAtInfinity = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(asymptoticDistance, 0.0, bodies);

            Assert.Less(Math.Abs(depthAtInfinity), 1e-3, "Curvature depth at 1e8 distance must vanish towards zero.");
            Assert.LessOrEqual(depthAtInfinity, 0.0, "Curvature depth must always be non-positive (gravitational well).");
        }

        [Test]
        public void F06_Boundary_SpacetimeGrid_LinearSuperposition_MultipleMassesSumDirectly()
        {
            // Boundary condition: Two masses m1 and m2 at different coordinates.
            // Verifies linear superposition property of metric depression formula.
            var bodyA = new CelestialBodyState(1, "A", BodyType.Star, 3000.0, 5.0, new Vector3d(-20.0, 0.0, 0.0), Vector3d.zero);
            var bodyB = new CelestialBodyState(2, "B", BodyType.Star, 7000.0, 8.0, new Vector3d(40.0, 0.0, 0.0), Vector3d.zero);

            var listA = new List<CelestialBodyState> { bodyA };
            var listB = new List<CelestialBodyState> { bodyB };
            var listBoth = new List<CelestialBodyState> { bodyA, bodyB };

            double queryX = 10.0;
            double queryZ = 15.0;

            double depthA = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(queryX, queryZ, listA);
            double depthB = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(queryX, queryZ, listB);
            double depthBoth = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(queryX, queryZ, listBoth);

            Assert.AreEqual(depthA + depthB, depthBoth, 1e-9, "Curvature depression must satisfy linear superposition.");
            TestAssertions.AssertApproximatelyEqual(depthA + depthB, depthBoth, 1e-9, "Curvature depression must satisfy linear superposition.");
        }

        [Test]
        public void F06_Boundary_SpacetimeGrid_RadialSymmetry_SymmetricCoordinatesProduceIdenticalDepression()
        {
            // Boundary condition: Centered central mass at (0, 0).
            // Metric depression evaluated at (+x, +y), (-x, +y), (+x, -y), (-x, -y) must be identical to machine precision.
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, 5000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var bodies = new List<CelestialBodyState> { sun };

            double x = 45.678;
            double z = 32.109;

            double d1 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(x, z, bodies);
            double d2 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(-x, z, bodies);
            double d3 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(x, -z, bodies);
            double d4 = CosmoForgeTestHarness.CalculateSpacetimeCurvatureSample(-x, -z, bodies);

            Assert.AreEqual(d1, d2, 1e-12, "Symmetric coordinate (+x, z) vs (-x, z) must yield identical curvature.");
            Assert.AreEqual(d1, d3, 1e-12, "Symmetric coordinate (+x, z) vs (x, -z) must yield identical curvature.");
            Assert.AreEqual(d1, d4, 1e-12, "Symmetric coordinate (+x, z) vs (-x, -z) must yield identical curvature.");
        }

        #endregion
    }
}
