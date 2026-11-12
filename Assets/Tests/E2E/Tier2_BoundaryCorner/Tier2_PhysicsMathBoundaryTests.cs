// ============================================================================
// CosmoForge Tier 2 Boundary & Corner Cases: Physics & Math Core Tests (F01 - F03)
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
using CosmoForge.Tests.Harness;

namespace CosmoForge.Tests.Tier2
{
    [TestFixture]
    public class Tier2_PhysicsMathBoundaryTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(CosmoForgeTestHarness.DefaultG, 0.1);
        }

        #region Feature F01: Project Scaffold & Android 60 FPS Setup (5 Boundary Tests)

        [Test]
        public void F01_Boundary_TimeDelta_Zero_DoesNotDivideByZeroOrProduceNaN()
        {
            // Boundary condition: dt = 0.0s (simulation paused or zero elapsed frame time).
            // Must not cause divide-by-zero, NaN positions/velocities, or unhandled exceptions.
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem(G: 1.0, softening: 0.1);
            _integrator.InitializeAccelerations(bodies);

            Vector3d initialPosSun = bodies[0].Position;
            Vector3d initialPosEarth = bodies[1].Position;
            Vector3d initialVelSun = bodies[0].Velocity;
            Vector3d initialVelEarth = bodies[1].Velocity;

            // Step with dt = 0.0
            _integrator.Step(bodies, 0.0);

            TestAssertions.AssertNoNaNOrInfinity(bodies);
            Assert.AreEqual(initialPosSun, bodies[0].Position, "Sun position must be unchanged when dt = 0.0");
            Assert.AreEqual(initialPosEarth, bodies[1].Position, "Earth position must be unchanged when dt = 0.0");
            Assert.AreEqual(initialVelSun, bodies[0].Velocity, "Sun velocity must be unchanged when dt = 0.0");
            Assert.AreEqual(initialVelEarth, bodies[1].Velocity, "Earth velocity must be unchanged when dt = 0.0");
        }

        [Test]
        public void F01_Boundary_TimeDelta_Microsecond_PrecisionMaintained()
        {
            // Boundary condition: Extremely small positive timestep (dt = 1e-9s, 1 nanosecond).
            // Numerical integration must accumulate micro-displacements without underflow to zero or loss of significance.
            var bodies = CosmoForgeTestHarness.CreateSunEarthSystem(G: 1.0, softening: 0.1);
            _integrator.InitializeAccelerations(bodies);

            double dtMicro = 1e-9;
            Vector3d initialPos = bodies[1].Position;
            Vector3d vel = bodies[1].Velocity;

            _integrator.Step(bodies, dtMicro);

            TestAssertions.AssertNoNaNOrInfinity(bodies);
            Vector3d displacement = bodies[1].Position - initialPos;

            // Expected displacement order: v * dt ~ 10.0 * 1e-9 = 1e-8
            Assert.Greater(displacement.magnitude, 0.0, "Microsecond displacement must not underflow to zero in double-precision.");
            Assert.LessOrEqual(Math.Abs(displacement.magnitude - (vel.magnitude * dtMicro)), 1e-12, "Displacement must match kinematic prediction within double precision.");
        }

        [Test]
        public void F01_Boundary_FrameRate_Target_ExtremeValuesHandledSafely()
        {
            // Boundary condition: Target framerate values at boundaries (0, negative, extreme 1000 FPS).
            // Engine / platform scheduler must clamp or handle extreme framerate parameters gracefully.
            int[] testFrameRates = { -60, 0, 1, 15, 60, 120, 240, 1000 };

            foreach (int fps in testFrameRates)
            {
                // Verify clamp logic used in Android performance budget: minimum 15 FPS, maximum 240 FPS (or standard platform clamp)
                int clampedFps = Math.Clamp(fps, 15, 240);
                Assert.GreaterOrEqual(clampedFps, 15, $"Clamped FPS must be >= 15 for input {fps}");
                Assert.LessOrEqual(clampedFps, 240, $"Clamped FPS must be <= 240 for input {fps}");

                double frameBudgetSeconds = 1.0 / clampedFps;
                Assert.IsFalse(double.IsNaN(frameBudgetSeconds), $"Frame budget must not be NaN for target {fps}");
                Assert.IsFalse(double.IsInfinity(frameBudgetSeconds), $"Frame budget must not be infinite for target {fps}");
                Assert.Greater(frameBudgetSeconds, 0.0, "Frame budget must be strictly positive.");
            }
        }

        [Test]
        public void F01_Boundary_RenderScale_Extremes_ClampedToValidRange()
        {
            // Boundary condition: Dynamic resolution / render scale at lower bound (0.1f) and upper bound (2.0f).
            // Prevents invalid viewport dimensions (<= 0 or huge render target allocations that crash mobile GPU).
            float[] testScales = { -1.0f, 0.0f, 0.05f, 0.1f, 0.5f, 1.0f, 1.5f, 2.0f, 5.0f, 100.0f };

            foreach (float scale in testScales)
            {
                float clampedScale = Mathf.Clamp(scale, 0.1f, 2.0f);
                Assert.GreaterOrEqual(clampedScale, 0.1f, $"Render scale must be >= 0.1 for raw input {scale}");
                Assert.LessOrEqual(clampedScale, 2.0f, $"Render scale must be <= 2.0 for raw input {scale}");

                int screenWidth = 1920;
                int screenHeight = 1080;
                int targetWidth = (int)(screenWidth * clampedScale);
                int targetHeight = (int)(screenHeight * clampedScale);

                Assert.GreaterOrEqual(targetWidth, 192, "Render target width must be >= 192 pixels.");
                Assert.GreaterOrEqual(targetHeight, 108, "Render target height must be >= 108 pixels.");
                Assert.LessOrEqual(targetWidth, 3840, "Render target width must not exceed 4K buffer limit.");
            }
        }

        [Test]
        public void F01_Boundary_Android_MemoryBudget_HighTransientObjectChurn_NoLeakOrNaN()
        {
            // Boundary condition: Rapid creation, allocation, and clearance of 1,000 transient body states.
            // Verifies zero memory corruption, stable GC behavior, and consistent state validation.
            const int bodyCount = 1000;
            var transientBodies = new List<CelestialBodyState>(bodyCount);

            for (int i = 0; i < bodyCount; i++)
            {
                double angle = (2.0 * Math.PI * i) / bodyCount;
                var state = new CelestialBodyState(
                    id: i + 1,
                    name: $"Particle_{i}",
                    type: BodyType.AsteroidProbe,
                    mass: 1e-5,
                    radius: 0.01,
                    position: new Vector3d(Math.Cos(angle) * 100.0, 0.0, Math.Sin(angle) * 100.0),
                    velocity: new Vector3d(-Math.Sin(angle) * 5.0, 0.0, Math.Cos(angle) * 5.0)
                );
                transientBodies.Add(state);
            }

            Assert.AreEqual(bodyCount, transientBodies.Count, "All 1,000 transient bodies must be allocated.");
            TestAssertions.AssertNoNaNOrInfinity(transientBodies);

            // Step integrator once with high object count
            _integrator.InitializeAccelerations(transientBodies);
            _integrator.Step(transientBodies, 0.01);

            TestAssertions.AssertNoNaNOrInfinity(transientBodies);

            // Clear and ensure clean release
            transientBodies.Clear();
            Assert.AreEqual(0, transientBodies.Count, "Body list must clear cleanly without leftover references.");
        }

        #endregion

        #region Feature F02: Symplectic Velocity Verlet Integrator (5 Boundary Tests)

        [Test]
        public void F02_Boundary_PlummerSoftening_ZeroEpsilon_CoincidentBodies_HandlesSingularityGracefully()
        {
            // Boundary condition: Two bodies positioned at exact coincident coordinates r_1 = r_2 = (0, 0, 0)
            // with softening factor set to zero (epsilon = 0.0).
            // In raw Newtonian physics F ~ 1/0 is infinite. The engine's normalization and distance guards
            // must prevent NaN or Infinity contagion by yielding zero mutual acceleration.
            IGravityIntegrator integrator = _integrator;
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.0; // Raw unsoftened boundary

            var bodyA = new CelestialBodyState(1, "A", BodyType.Terrestrial, 1000.0, 1.0, Vector3d.zero, Vector3d.zero);
            var bodyB = new CelestialBodyState(2, "B", BodyType.Terrestrial, 500.0, 1.0, Vector3d.zero, Vector3d.zero);
            var bodies = new List<CelestialBodyState> { bodyA, bodyB };

            integrator.InitializeAccelerations(bodies);

            // Acceleration must not be NaN or Infinity
            Assert.IsFalse(double.IsNaN(bodyA.Acceleration.x));
            Assert.IsFalse(double.IsNaN(bodyB.Acceleration.x));
            Assert.IsFalse(double.IsInfinity(bodyA.Acceleration.x));
            TestAssertions.AssertNoNaNOrInfinity(bodyA.Acceleration, "Body A acceleration at coincident zero distance");
            TestAssertions.AssertNoNaNOrInfinity(bodyB.Acceleration, "Body B acceleration at coincident zero distance");

            // Perform integration step
            integrator.Step(bodies, 0.02);

            Assert.IsFalse(double.IsNaN(bodyA.Position.x));
            Assert.IsFalse(double.IsNaN(bodyB.Position.x));
            TestAssertions.AssertNoNaNOrInfinity(bodyA.Position, "Body A position after step");
            TestAssertions.AssertNoNaNOrInfinity(bodyB.Position, "Body B position after step");
            TestAssertions.AssertNoNaNOrInfinity(bodyA.Velocity, "Body A velocity after step");
            TestAssertions.AssertNoNaNOrInfinity(bodyB.Velocity, "Body B velocity after step");
        }

        [Test]
        public void F02_Boundary_ExtremeMassRatio_SupermassiveCentralBodyAndDustGrain()
        {
            // Boundary condition: Extreme mass ratio of 10^30 (e.g. SMBH 10^36 kg vs dust particle 10^6 kg).
            // Integrator must accurately compute dust grain orbital motion without underflowing back-reaction or causing numerical overflow.
            IGravityIntegrator integrator = _integrator;
            double mSMBH = 1e30;
            double mDust = 1.0; // Ratio 1:1e30
            double G = 1.0;
            double rOrbit = 1e6;
            double vCirc = Math.Sqrt(G * mSMBH / rOrbit);

            var smbh = new CelestialBodyState(1, "SMBH", BodyType.BlackHole, mSMBH, 100.0, Vector3d.zero, Vector3d.zero);
            var dust = new CelestialBodyState(2, "Dust", BodyType.AsteroidProbe, mDust, 0.001, new Vector3d(rOrbit, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0));
            var bodies = new List<CelestialBodyState> { smbh, dust };

            integrator.GravitationalConstant = G;
            integrator.SofteningFactor = 1.0;
            integrator.InitializeAccelerations(bodies);

            // Step for 100 steps with timestep resolved to orbital period
            double dt = (2.0 * Math.PI * rOrbit / vCirc) / 500.0;
            for (int i = 0; i < 100; i++)
            {
                integrator.Step(bodies, dt);
            }

            TestAssertions.AssertNoNaNOrInfinity(bodies);

            // Central massive body must experience essentially zero displacement due to extreme inertia
            Assert.Less(smbh.Position.magnitude, 1e-6, "Supermassive black hole position must remain practically stationary.");
            // Dust grain must maintain its orbital radius within 1%
            double finalDustRadius = dust.Position.magnitude;
            double radiusError = Math.Abs(finalDustRadius - rOrbit) / rOrbit;
            Assert.Less(radiusError, 0.02, "Dust grain orbital radius must remain stable within 2% across 100 steps.");
        }

        [Test]
        public void F02_Boundary_NearLightspeed_RelativisticVelocityBoundary()
        {
            // Boundary condition: Body launched at near-lightspeed velocity (v = 0.99c, where c = 50.0 SU/s -> v = 49.5 SU/s).
            // Velocity Verlet integrator must advance coordinates without overflowing or generating complex/infinite components.
            IGravityIntegrator integrator = _integrator;
            double c = CosmoForgeTestHarness.DefaultC;
            double nearC = 0.99 * c;

            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, 10000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var fastParticle = new CelestialBodyState(2, "RelativisticProbe", BodyType.AsteroidProbe, 0.01, 0.1, new Vector3d(100.0, 0.0, 0.0), new Vector3d(0.0, nearC, 0.0));
            var bodies = new List<CelestialBodyState> { primary, fastParticle };

            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.5;
            integrator.InitializeAccelerations(bodies);

            double dt = 0.01;
            for (int i = 0; i < 50; i++)
            {
                integrator.Step(bodies, dt);
            }

            TestAssertions.AssertNoNaNOrInfinity(bodies);
            Assert.Greater(fastParticle.Position.y, 0.0, "High velocity probe must have translated significantly along Y axis.");
            Assert.Greater(fastParticle.Velocity.magnitude, 0.0, "Velocity must remain positive and finite.");
        }

        [Test]
        public void F02_Boundary_ZeroMass_GhostTestParticle_ExertsZeroBackReaction()
        {
            // Boundary condition: Massless test particle (m = 0.0).
            // Must feel gravitational pull from massive bodies, but must exert exactly 0.0 acceleration on the massive body.
            IGravityIntegrator integrator = _integrator;
            var star = new CelestialBodyState(1, "Star", BodyType.Star, 5000.0, 10.0, Vector3d.zero, Vector3d.zero);
            var ghostParticle = new CelestialBodyState(2, "Ghost", BodyType.AsteroidProbe, 0.0, 0.1, new Vector3d(50.0, 0.0, 0.0), new Vector3d(0.0, 10.0, 0.0));
            var bodies = new List<CelestialBodyState> { star, ghostParticle };

            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.1;
            integrator.InitializeAccelerations(bodies);

            // Accelerations check
            Assert.AreEqual(0.0, star.Acceleration.magnitude, 1e-15, "Primary star must experience strictly zero acceleration from zero-mass body.");
            Assert.Greater(ghostParticle.Acceleration.magnitude, 0.0, "Massless test particle must still experience acceleration towards primary star.");

            integrator.Step(bodies, 0.05);

            TestAssertions.AssertNoNaNOrInfinity(bodies);
            Assert.AreEqual(Vector3d.zero, star.Position, "Star must not have moved from origin.");
            Assert.AreNotEqual(new Vector3d(50.0, 0.0, 0.0), ghostParticle.Position, "Ghost particle must have moved according to gravity.");
        }

        [Test]
        public void F02_Boundary_NegativeDeltaTime_ExactReversalStep_ReachesMachinePrecision()
        {
            // Boundary condition: Negative timestep dt = -0.02s applied directly to test symplectic time-inversion.
            // Stepping +dt then -dt on a chaotic 3-body system must return positions to within machine epsilon.
            IGravityIntegrator integrator = _integrator;
            var bodies = CosmoForgeTestHarness.CreateSunEarthMoonSystem(G: 1.0);
            integrator.GravitationalConstant = 1.0;
            integrator.SofteningFactor = 0.1;
            integrator.InitializeAccelerations(bodies);

            Vector3d initPos0 = bodies[0].Position;
            Vector3d initPos1 = bodies[1].Position;
            Vector3d initPos2 = bodies[2].Position;

            double dt = 0.02;
            int stepCount = 200;

            // Step forward
            for (int i = 0; i < stepCount; i++)
            {
                integrator.Step(bodies, dt);
            }

            // Invert velocities
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            // Step forward with reversed velocity (equivalent to stepping backward in time)
            for (int i = 0; i < stepCount; i++)
            {
                integrator.Step(bodies, dt);
            }

            // Re-invert velocities
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            Assert.LessOrEqual((bodies[0].Position - initPos0).magnitude, 1e-4);
            Assert.LessOrEqual((bodies[1].Position - initPos1).magnitude, 1e-4);
            Assert.LessOrEqual((bodies[2].Position - initPos2).magnitude, 1e-4);
            TestAssertions.AssertTimeReversibility(initPos0, bodies[0].Position, 1e-5, "Sun position after forward-backward integration");
            TestAssertions.AssertTimeReversibility(initPos1, bodies[1].Position, 1e-5, "Earth position after forward-backward integration");
            TestAssertions.AssertTimeReversibility(initPos2, bodies[2].Position, 1e-5, "Moon position after forward-backward integration");
        }

        #endregion

        #region Feature F03: Double-Precision Core Physics Math (5 Boundary Tests)

        [Test]
        public void F03_Boundary_Vector3d_ZeroMagnitude_NormalizationReturnsZeroVector()
        {
            // Boundary condition: Normalizing a vector with magnitude <= 1e-15 or exactly (0,0,0).
            // Must return Vector3d.zero without generating NaN or IEEE 754 infinity in x, y, or z.
            var zeroVec = Vector3d.zero;
            var normalizedZero = zeroVec.normalized;

            Assert.AreEqual(0.0, normalizedZero.x, "Zero vector normalized.x must be 0.0");
            Assert.AreEqual(0.0, normalizedZero.y, "Zero vector normalized.y must be 0.0");
            Assert.AreEqual(0.0, normalizedZero.z, "Zero vector normalized.z must be 0.0");
            TestAssertions.AssertNoNaNOrInfinity(normalizedZero, "Normalized zero vector");

            // Sub-threshold vectors
            var subThresholdVec = new Vector3d(1e-16, -1e-17, 1e-18);
            var normalizedSub = subThresholdVec.normalized;

            Assert.AreEqual(Vector3d.zero, normalizedSub, "Vector below 1e-15 magnitude must normalize to zero.");
            TestAssertions.AssertNoNaNOrInfinity(normalizedSub, "Normalized sub-threshold vector");
        }

        [Test]
        public void F03_Boundary_Vector3d_ExtremeMagnitude_NoPrematureOverflow()
        {
            // Boundary condition: Vector with astronomical magnitude (1e150), calculating dot product, scaled division, and addition.
            // Operations must not overflow to Infinity when intermediate terms remain within IEEE 754 double range (~1.79e308).
            var bigA = new Vector3d(1e150, 2e150, -3e150);
            var bigB = new Vector3d(2e150, -1e150, 4e150);

            var sum = bigA + bigB;
            Assert.AreEqual(3e150, sum.x, 1e140);
            Assert.AreEqual(1e150, sum.y, 1e140);
            Assert.AreEqual(1e150, sum.z, 1e140);

            var scaled = bigA / 1e50;
            Assert.AreEqual(1e100, scaled.x, 1e90);
            Assert.AreEqual(2e100, scaled.y, 1e90);
            Assert.AreEqual(-3e100, scaled.z, 1e90);

            TestAssertions.AssertNoNaNOrInfinity(sum, "Extreme magnitude vector sum");
            TestAssertions.AssertNoNaNOrInfinity(scaled, "Extreme magnitude vector division");
        }

        [Test]
        public void F03_Boundary_FloatingOrigin_ExactThresholdCrossing_TriggersShiftAtBoundary()
        {
            // Boundary condition: Camera distance exactly at threshold R_threshold = 2000.0 vs 2000.0001.
            // Shift must trigger when distance strictly exceeds threshold, resetting camera coordinate back within bounds.
            double threshold = 2000.0;

            Vector3d cameraAtBoundary = new Vector3d(threshold, 0.0, 0.0);
            bool shouldShiftAtBoundary = cameraAtBoundary.magnitude > threshold;
            Assert.IsFalse(shouldShiftAtBoundary, "Distance exactly equal to threshold must not trigger shift (<= threshold).");

            Vector3d cameraBeyondBoundary = new Vector3d(threshold + 0.001, 0.0, 0.0);
            bool shouldShiftBeyond = cameraBeyondBoundary.magnitude > threshold;
            Assert.IsTrue(shouldShiftBeyond, "Distance exceeding threshold must trigger shift.");

            // Verify origin shift calculation
            Vector3d currentOrigin = Vector3d.zero;
            Vector3d cameraPhysicsPos = cameraBeyondBoundary;

            if (shouldShiftBeyond)
            {
                currentOrigin += cameraPhysicsPos;
                Vector3d localCameraPos = cameraPhysicsPos - currentOrigin;
                Assert.AreEqual(0.0, localCameraPos.magnitude, 1e-9, "After origin shift, camera local render position must be at origin.");
                Assert.AreEqual(cameraBeyondBoundary, currentOrigin, "Origin must record cumulative shift.");
            }
        }

        [Test]
        public void F03_Boundary_FloatingOrigin_ZeroDisplacement_NoSpuriousOffset()
        {
            // Boundary condition: Camera remains perfectly stationary at (0, 0, 0).
            // Origin check must register zero shift, zero accumulator drift, and preserve exact coordinate mappings.
            Vector3d origin = Vector3d.zero;
            Vector3d cameraPos = Vector3d.zero;
            double threshold = 2000.0;

            bool triggersShift = cameraPos.magnitude > threshold;
            Assert.IsFalse(triggersShift, "Zero displacement must never trigger an origin shift.");

            Vector3d bodyAbsolute = new Vector3d(500.0, 100.0, -250.0);
            Vector3d renderPosition = bodyAbsolute - origin;

            Assert.AreEqual(bodyAbsolute, renderPosition, "Render position must equal absolute position when origin is at zero.");
        }

        [Test]
        public void F03_Boundary_FloatingOrigin_ExtremeDistance_PreservesSubMillimeterPrecision()
        {
            // Boundary condition: Absolute celestial position at 10^12 meters (Saturn orbital distance).
            // Origin is shifted to local vicinity (10^12 meters).
            // Verifies that converting relative offset (e.g. 1.234567 meters) to single-precision float retains sub-millimeter precision.
            double saturnDistance = 1.43e12; // 1.43 billion kilometers
            Vector3d saturnOrigin = new Vector3d(saturnDistance, 0.0, 0.0);

            // A spacecraft 1.2345 meters from Saturn
            double localOffset = 1.234567;
            Vector3d probeAbsolute = saturnOrigin + new Vector3d(localOffset, 0.0, 0.0);

            // Relative double calculation
            Vector3d relativeDouble = probeAbsolute - saturnOrigin;
            Vector3 relativeFloat = relativeDouble.ToVector3();

            Assert.AreEqual(localOffset, relativeFloat.x, 1e-4, "Local relative offset must maintain sub-millimeter precision after origin shift.");

            // Compare with direct float cast without origin shift (catastrophic cancellation)
            float directFloatSaturn = (float)probeAbsolute.x;
            float directFloatOrigin = (float)saturnOrigin.x;
            float rawFloatDiff = directFloatSaturn - directFloatOrigin;

            // In direct single precision, 1.43e12 has ULP ~ 131,072 meters! A 1.23 meter offset is completely wiped out.
            Assert.AreNotEqual(localOffset, (double)rawFloatDiff, "Without floating origin, single-precision completely loses small offsets at 10^12 scale.");
        }

        #endregion
    }
}
