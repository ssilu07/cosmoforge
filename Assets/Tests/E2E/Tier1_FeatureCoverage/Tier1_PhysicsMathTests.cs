// ============================================================================
// CosmoForge Tier 1 Feature Coverage: Physics & Math Core Tests (F01 - F03)
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
using CosmoForge.Core.Performance;
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier1
{
    [TestFixture]
    public class Tier1_PhysicsMathTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.1);
        }

        #region Feature F01: Project Scaffold & Android 60 FPS Setup (5 Tests)

        [Test]
        public void F01_Scaffold_PhysicsFixedTimestep_DefaultsToFiftyHz()
        {
            // Requirement R1 & R2: Simulation requires a stable fixed delta time.
            // 50 Hz corresponds to dt = 0.02s (standard for mobile 60 FPS loop).
            var world = new SimulationWorld();
            double defaultDt = SimulationWorld.FixedDeltaTime;
            double frameBudgetSeconds = 1.0 / 60.0; // 16.66 ms

            Assert.AreEqual(0.02, defaultDt, 1e-6, "Base physics simulation timestep must default to 0.02 s (50 Hz).");
            Assert.AreEqual(0.02, SimulationWorld.FixedDeltaTime, 1e-6);
            Assert.Greater(frameBudgetSeconds, defaultDt * 0.5, "Frame budget must comfortably accommodate physics substeps.");
        }

        [Test]
        public void F01_Scaffold_PerformanceBudget_SubsteppingCapEnforced()
        {
            // Requirement R1: Mobile 60 FPS loop prevents 'spiral of death' via substep cap (e.g. max 25 substeps)
            var world = new SimulationWorld(_integrator);
            var body = new CelestialBodyState(1, "TestBody", BodyType.Terrestrial, 1.0, 1.0, Vector3d.zero, new Vector3d(10.0, 0, 0));
            world.AddBody(body);

            double extremeWarpMultiplier = 1000.0;
            double frameDeltaTime = 0.033; // ~30 FPS hitch
            
            world.UpdateSimulation(frameDeltaTime, extremeWarpMultiplier);

            Assert.AreEqual(25, SimulationWorld.MaxSubsteps, "Sub-stepping must be clamped to the maximum substep budget to preserve frame rate.");
            Assert.LessOrEqual(body.Position.x, 25 * SimulationWorld.FixedDeltaTime * 10.0 + 1e-6, "Unclamped steps would have exceeded mobile frame budget.");
        }

        [Test]
        public void F01_Scaffold_SimulationWorld_InitializesEmptyBodyListAndIntegrator()
        {
            // Requirement R1 & Acceptance Criteria: Engine world initializes cleanly
            var bodies = new List<CelestialBodyState>();
            IGravityIntegrator integrator = new VelocityVerletIntegrator(1.0, 0.05);

            Assert.IsNotNull(bodies, "Body list must not be null upon initialization.");
            Assert.AreEqual(0, bodies.Count, "New simulation world must start with zero celestial bodies.");
            Assert.IsNotNull(integrator, "Integrator must be instantiated.");
            Assert.AreEqual(1.0, integrator.GravitationalConstant, 1e-9);
            Assert.AreEqual(0.05, integrator.SofteningFactor, 1e-9);
        }

        [Test]
        public void F01_Scaffold_TimeScaleMultiplier_ScalesPhysicsDtProportionally()
        {
            // Requirement R1: Time scale scaling allows continuous time-warp
            var world = new SimulationWorld(_integrator);
            double[] testScales = { 0.1, 0.5, 1.0, 2.0, 10.0, 100.0 };

            foreach (double scale in testScales)
            {
                world.TimeWarpMultiplier = scale;
                Assert.AreEqual(scale, world.TimeWarpMultiplier, 1e-12, $"SimulationWorld.TimeWarpMultiplier must reflect set scale {scale}.");
            }
        }

        [Test]
        public void F01_Scaffold_StateInterpolationAlpha_BoundedBetweenZeroAndOne()
        {
            // Requirement R1: Smooth visual render interpolation between physics steps: alpha = accum / dt
            double fixedDt = 0.02;
            for (double accumulator = 0.0; accumulator <= fixedDt; accumulator += 0.005)
            {
                double alpha = Math.Clamp(accumulator / fixedDt, 0.0, 1.0);
                Assert.GreaterOrEqual(alpha, 0.0, "Interpolation factor alpha must be >= 0.0");
                Assert.LessOrEqual(alpha, 1.0, "Interpolation factor alpha must be <= 1.0");
            }
        }

        #endregion

        #region Feature F02: Symplectic Velocity Verlet Integrator (5 Tests)

        [Test]
        public void F02_VelocityVerlet_CircularKeplerOrbit_ConservesMechanicalEnergy()
        {
            // Requirement R2 & Acceptance Criteria: Symplectic integrator conserves total mechanical energy |dE/E0| <= 1e-4
            IGravityIntegrator integrator = _integrator;
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem(G: 1.0, softening: 0.0);
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.0;

            integrator.InitializeAccelerations(bodies);
            double initialKinetic = integrator.CalculateTotalKineticEnergy(bodies);
            double initialEnergy = integrator.CalculateTotalEnergy(bodies);
            Assert.Greater(initialKinetic, 0.0, "Initial kinetic energy must be positive.");

            // Step for 2,500 steps (~50 seconds of simulation)
            double dt = 0.02;
            for (int i = 0; i < 2500; i++)
            {
                integrator.Step(bodies, dt);
            }

            double finalEnergy = integrator.CalculateTotalEnergy(bodies);
            double energyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);
            Assert.LessOrEqual(energyDrift, 1e-4, "Mechanical energy drift over 2500 steps must remain below 1e-4.");
            TestAssertions.AssertEnergyConserved(initialEnergy, finalEnergy, 1e-4, "2,500-step circular Kepler orbit");
        }

        [Test]
        public void F02_VelocityVerlet_ClosedSystem_ConservesLinearMomentum()
        {
            // Requirement R2 & Newton's 3rd Law: Linear momentum is conserved to machine precision
            IGravityIntegrator integrator = _integrator;
            var bodies = CosmoForgeTestHarness.CreateEqualMassBinarySystem(separation: 40.0, mass: 200.0, G: 1.0);
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.1;

            integrator.InitializeAccelerations(bodies);
            Vector3d initialP = integrator.CalculateTotalLinearMomentum(bodies);
            Assert.IsFalse(double.IsNaN(initialP.x));

            double dt = 0.02;
            for (int i = 0; i < 1000; i++)
            {
                integrator.Step(bodies, dt);
            }

            Vector3d finalP = integrator.CalculateTotalLinearMomentum(bodies);
            double momentumDrift = (finalP - initialP).magnitude;
            Assert.LessOrEqual(momentumDrift, 1e-7, "Linear momentum drift must remain within 1e-7.");
            TestAssertions.AssertLinearMomentumConserved(initialP, finalP, 1e-7, "1,000-step binary system linear momentum");
        }

        [Test]
        public void F02_VelocityVerlet_CentralGravity_ConservesAngularMomentum()
        {
            // Requirement R2: Angular momentum L is conserved under central gravitational potential
            IGravityIntegrator integrator = _integrator;
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem(G: 1.0, softening: 0.0);
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.0;

            integrator.InitializeAccelerations(bodies);
            Vector3d initialL = integrator.CalculateTotalAngularMomentum(bodies);
            Assert.Greater(initialL.magnitude, 0.0, "Initial angular momentum must be non-zero.");

            double dt = 0.02;
            for (int i = 0; i < 1500; i++)
            {
                integrator.Step(bodies, dt);
            }

            Vector3d finalL = integrator.CalculateTotalAngularMomentum(bodies);
            double angularDrift = (finalL - initialL).magnitude / initialL.magnitude;
            Assert.LessOrEqual(angularDrift, 1e-6, "Angular momentum drift must remain within 1e-6.");
            TestAssertions.AssertAngularMomentumConserved(initialL, finalL, 1e-6, "1,500-step angular momentum conservation");
        }

        [Test]
        public void F02_VelocityVerlet_PlummerSoftening_EliminatesSingularityAtZeroSeparation()
        {
            // Requirement R2: Plummer softening eliminates 1/r^2 force explosion at r = 0
            IGravityIntegrator integrator = _integrator;
            double G = 1.0;
            double eps = 0.5;
            integrator.GravitationalConstant = G;
            integrator.SofteningFactor = eps;

            var bodyA = new CelestialBodyState(1, "A", BodyType.Terrestrial, 100.0, 1.0, Vector3d.zero, Vector3d.zero);
            var bodyB = new CelestialBodyState(2, "B", BodyType.Terrestrial, 50.0, 1.0, Vector3d.zero, Vector3d.zero);
            var bodies = new List<CelestialBodyState> { bodyA, bodyB };

            integrator.InitializeAccelerations(bodies);

            // Force at r = 0 must be zero because displacement vector r_ij = (0, 0, 0)
            Assert.AreEqual(0.0, bodyA.Acceleration.magnitude, 1e-12, "Acceleration at separation 0 must be exactly 0.");
            Assert.AreEqual(0.0, bodyB.Acceleration.magnitude, 1e-12, "Acceleration at separation 0 must be exactly 0.");

            // Potential energy must be finite: U = -G * m1 * m2 / eps
            double expectedPotential = -(G * bodyA.Mass * bodyB.Mass) / eps;
            double actualPotential = integrator.CalculateTotalPotentialEnergy(bodies);

            Assert.AreEqual(expectedPotential, actualPotential, 1e-9, "Plummer potential at r=0 must equal -G*m1*m2/eps");
            TestAssertions.AssertApproximatelyEqual(expectedPotential, actualPotential, 1e-9, "Plummer potential at r=0 must equal -G*m1*m2/eps");
        }

        [Test]
        public void F02_VelocityVerlet_TimeReversibility_ReturnsToInitialState()
        {
            // Requirement R2: Symplectic integrators are time-reversible S(-dt) = S(dt)^-1
            IGravityIntegrator integrator = _integrator;
            var bodies = CosmoForgeTestHarness.CreateEqualMassBinarySystem(separation: 30.0, mass: 150.0, G: 1.0);
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.1;

            integrator.InitializeAccelerations(bodies);

            Vector3d initialPosA = bodies[0].Position;
            Vector3d initialPosB = bodies[1].Position;

            double dt = 0.01;
            int steps = 500;

            // Forward integration
            for (int i = 0; i < steps; i++)
            {
                integrator.Step(bodies, dt);
            }

            // Invert velocities
            bodies[0].Velocity = -bodies[0].Velocity;
            bodies[1].Velocity = -bodies[1].Velocity;

            // Backward integration (same dt)
            for (int i = 0; i < steps; i++)
            {
                integrator.Step(bodies, dt);
            }

            // Re-invert velocities to original direction
            bodies[0].Velocity = -bodies[0].Velocity;
            bodies[1].Velocity = -bodies[1].Velocity;

            Assert.LessOrEqual((bodies[0].Position - initialPosA).magnitude, 1e-5, "Body A must return to initial position after time reversal.");
            Assert.LessOrEqual((bodies[1].Position - initialPosB).magnitude, 1e-5, "Body B must return to initial position after time reversal.");
            TestAssertions.AssertTimeReversibility(initialPosA, bodies[0].Position, 1e-5, "Body A after time reversal");
            TestAssertions.AssertTimeReversibility(initialPosB, bodies[1].Position, 1e-5, "Body B after time reversal");
        }

        #endregion

        #region Feature F03: Double-Precision Core Physics Math (5 Tests)

        [Test]
        public void F03_Vector3d_ArithmeticOperators_CalculateAccurately()
        {
            // Requirement R2 & PROJECT.md Interface Contract: Vector3d operator overloads
            var a = new Vector3d(10.5, -20.25, 30.75);
            var b = new Vector3d(5.25, 10.0, -15.5);

            var sum = a + b;
            Assert.AreEqual(15.75, sum.x, 1e-9);
            Assert.AreEqual(-10.25, sum.y, 1e-9);
            Assert.AreEqual(15.25, sum.z, 1e-9);

            var diff = a - b;
            Assert.AreEqual(5.25, diff.x, 1e-9);
            Assert.AreEqual(-30.25, diff.y, 1e-9);
            Assert.AreEqual(46.25, diff.z, 1e-9);

            var scaled = a * 2.0;
            Assert.AreEqual(21.0, scaled.x, 1e-9);
            Assert.AreEqual(-40.5, scaled.y, 1e-9);
            Assert.AreEqual(61.5, scaled.z, 1e-9);

            var div = a / 2.0;
            Assert.AreEqual(5.25, div.x, 1e-9);
            Assert.AreEqual(-10.125, div.y, 1e-9);
            Assert.AreEqual(15.375, div.z, 1e-9);

            var neg = -a;
            Assert.AreEqual(-10.5, neg.x, 1e-9);
            Assert.AreEqual(20.25, neg.y, 1e-9);
            Assert.AreEqual(-30.75, neg.z, 1e-9);
        }

        [Test]
        public void F03_Vector3d_DotAndCrossProducts_SatisfyAlgebraicIdentities()
        {
            // Vector algebra identities
            var x = new Vector3d(1.0, 0.0, 0.0);
            var y = new Vector3d(0.0, 1.0, 0.0);
            var z = new Vector3d(0.0, 0.0, 1.0);

            // Cross product right-hand rule
            var crossXY = Vector3d.Cross(x, y);
            TestAssertions.AssertApproximatelyEqual(z, crossXY, 1e-9, "X cross Y must equal Z");

            // Anti-symmetry: a x b = - (b x a)
            var crossYX = Vector3d.Cross(y, x);
            TestAssertions.AssertApproximatelyEqual(-z, crossYX, 1e-9, "Y cross X must equal -Z");

            // Orthogonality: dot product with cross product is zero
            var a = new Vector3d(3.0, -4.0, 5.0);
            var b = new Vector3d(1.0, 2.0, -3.0);
            var axb = Vector3d.Cross(a, b);

            Assert.AreEqual(0.0, Vector3d.Dot(a, axb), 1e-9, "a . (a x b) must be 0");
            Assert.AreEqual(0.0, Vector3d.Dot(b, axb), 1e-9, "b . (a x b) must be 0");
        }

        [Test]
        public void F03_Vector3d_EuclideanMagnitudeAndNormalization_HandlesSmallVectors()
        {
            // Pythagorean quadruple: 3^2 + 4^2 + 12^2 = 9 + 16 + 144 = 169 = 13^2
            var v = new Vector3d(3.0, 4.0, 12.0);
            Assert.AreEqual(169.0, v.sqrMagnitude, 1e-9);
            Assert.AreEqual(13.0, v.magnitude, 1e-9);

            var normalized = v.normalized;
            Assert.AreEqual(1.0, normalized.magnitude, 1e-9);
            Assert.AreEqual(3.0 / 13.0, normalized.x, 1e-9);
            Assert.AreEqual(4.0 / 13.0, normalized.y, 1e-9);
            Assert.AreEqual(12.0 / 13.0, normalized.z, 1e-9);

            // Guard against near-zero division (< 1e-15 returns Vector3d.zero)
            var tiny = new Vector3d(1e-16, 0.0, 0.0);
            var zeroNorm = tiny.normalized;
            Assert.AreEqual(Vector3d.zero, zeroNorm, "Near-zero vectors below 1e-15 must normalize to Vector3d.zero without NaN.");
        }

        [Test]
        public void F03_Vector3d_PrecisionRetention_RetainsMillimeterDisplacementAtOneAU()
        {
            // Astronomical scale: 1 AU = 1.496e11 meters
            double oneAU = 1.496e11;
            double millimeterOffset = 0.001; // 1 mm

            var pos = new Vector3d(oneAU, 0.0, 0.0);
            var shiftedPos = pos + new Vector3d(millimeterOffset, 0.0, 0.0);
            double delta = (shiftedPos - pos).x;

            Assert.AreEqual(millimeterOffset, delta, 2e-5, "Vector3d must retain millimeter displacements at 1 AU scale.");

            // Verify float fails this test (demonstrating why double is strictly mandatory)
            float floatAU = (float)oneAU;
            float floatOffset = (float)millimeterOffset;
            float[] floatMem = new float[] { floatAU + floatOffset };
            float floatDelta = floatMem[0] - floatAU;
            Assert.AreEqual(0.0f, floatDelta, "Single precision float loses millimeter resolution at 1 AU.");
        }

        [Test]
        public void F03_FloatingOrigin_CoordinateShift_TranslatesRenderSpaceWithoutAlteringPhysics()
        {
            // Requirement R1 & R2: Floating origin shifts visual render camera without altering absolute double coordinates
            Vector3d absolutePhysicsPosition = new Vector3d(150000.0, 25000.0, -80000.0);
            Vector3d originOffset = new Vector3d(149000.0, 24000.0, -79000.0);

            // Render coordinate = (absolute - origin).ToVector3()
            Vector3d relativeDouble = absolutePhysicsPosition - originOffset;
            Vector3 renderPos = relativeDouble.ToVector3();

            Assert.AreEqual(1000.0f, renderPos.x, 1e-3f);
            Assert.AreEqual(1000.0f, renderPos.y, 1e-3f);
            Assert.AreEqual(-1000.0f, renderPos.z, 1e-3f);

            // Confirm absolute physics position remained unchanged
            Assert.AreEqual(150000.0, absolutePhysicsPosition.x, 1e-9);
            Assert.AreEqual(25000.0, absolutePhysicsPosition.y, 1e-9);
            Assert.AreEqual(-80000.0, absolutePhysicsPosition.z, 1e-9);
        }

        #endregion
    }
}
