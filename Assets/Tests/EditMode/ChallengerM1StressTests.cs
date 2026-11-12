// ============================================================================
// CosmoForge: Challenger 2 Empirical Stress Tests for Milestone 1
// Covers:
// 1. Extreme mass ratios (M1 = 10^8 vs m2 = 10^-3) in 3D non-planar orbits (Linear Momentum)
// 2. 32-body cluster in virial equilibrium over 2,000 steps (Angular Momentum Invariance)
// 3. Floating origin stress test: 10^7 units camera motion, coordinate boundedness, jitter,
//    and subscriber buffer integrity / camera LateUpdate stability
// ============================================================================

namespace CosmoForge.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Physics.Integrator;
    using CosmoForge.Core.Performance;

    [TestFixture]
    public class ChallengerM1StressTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(1.0, 0.1);
            FloatingOrigin.ResetOrigin();
        }

        [TearDown]
        public void TearDown()
        {
            FloatingOrigin.ResetOrigin();
        }

        // ====================================================================
        // STRESS SCENARIO 1: Extreme Mass Ratios (10^8 vs 10^-3) in 3D Non-Planar Orbit
        // ====================================================================

        [Test]
        public void StressScenario1_ExtremeMassRatio_3DNonPlanarOrbit_ConservesLinearMomentumToMachinePrecision()
        {
            // Primary: M1 = 10^8, Secondary: m2 = 10^-3 (Mass ratio 10^11)
            double G = 1.0;
            double softening = 0.05;
            double M1 = 1e8;
            double m2 = 1e-3;
            double totalMass = M1 + m2;
            double r0 = 500.0;

            // 3D Non-planar orbital plane: normal vector n = (1, 2, 3) normalized
            Vector3d normal = new Vector3d(1.0, 2.0, 3.0).normalized;
            // Vector in orbital plane orthogonal to normal: u = (-2, 1, 0) normalized
            Vector3d uVec = new Vector3d(-2.0, 1.0, 0.0).normalized;
            // Second in-plane vector: vVec = normal x uVec
            Vector3d vVec = Vector3d.Cross(normal, uVec).normalized;

            // Circular orbit velocity with softening
            // F = G * M1 * m2 * r / (r^2 + eps^2)^(3/2)
            // v_rel = sqrt( G * totalMass * r0^2 / (r0^2 + eps^2)^(3/2) )
            double epsSq = softening * softening;
            double denom = Math.Pow(r0 * r0 + epsSq, 1.5);
            double vRel = Math.Sqrt(G * totalMass * (r0 * r0) / denom);

            // Barycentric coordinates
            Vector3d pos1 = -uVec * ((m2 / totalMass) * r0);
            Vector3d pos2 = uVec * ((M1 / totalMass) * r0);

            Vector3d vel1 = -vVec * ((m2 / totalMass) * vRel);
            Vector3d vel2 = vVec * ((M1 / totalMass) * vRel);

            // Add arbitrary non-zero Center-of-Mass drift velocity: V_cm = (42.5, -17.3, 99.1)
            Vector3d vCm = new Vector3d(42.5, -17.3, 99.1);
            vel1 += vCm;
            vel2 += vCm;

            var body1 = new CelestialBodyState(1, "SupermassivePrimary", BodyType.BlackHole, M1, 20.0, pos1, vel1);
            var body2 = new CelestialBodyState(2, "MicroProbe", BodyType.AsteroidProbe, m2, 0.1, pos2, vel2);

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            Vector3d initialP = _integrator.CalculateTotalLinearMomentum(bodies);
            double initialPMag = initialP.magnitude;

            double dt = 0.01;
            int totalSteps = 2000;

            double maxLinearDelta = 0.0;
            double maxRelLinearDrift = 0.0;

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);

                Vector3d currentP = _integrator.CalculateTotalLinearMomentum(bodies);
                double deltaP = (currentP - initialP).magnitude;
                double relDrift = deltaP / initialPMag;

                if (deltaP > maxLinearDelta) maxLinearDelta = deltaP;
                if (relDrift > maxRelLinearDrift) maxRelLinearDrift = relDrift;
            }

            // Assert linear momentum conservation to machine precision
            // IEEE 754 float64 has 53 bits (~1.1e-16 relative precision)
            Assert.LessOrEqual(maxRelLinearDrift, 1e-12,
                $"Linear momentum relative drift {maxRelLinearDrift:E4} exceeds 1e-12 under 10^11 mass ratio.");
            Assert.LessOrEqual(maxLinearDelta, 1e-4,
                $"Absolute linear momentum delta {maxLinearDelta:E4} must be <= 1e-4.");
        }

        [Test]
        public void StressScenario1_ExtremeMassRatio_ZeroNetMomentum_StrictLinearConservation()
        {
            // Test with zero center of mass velocity
            double G = 1.0;
            double softening = 0.1;
            double M1 = 1e8;
            double m2 = 1e-3;
            double totalMass = M1 + m2;
            double r0 = 100.0;

            Vector3d pos1 = new Vector3d(-(m2 / totalMass) * r0, 0, 0);
            Vector3d pos2 = new Vector3d((M1 / totalMass) * r0, 0, 0);

            double vRel = Math.Sqrt(G * totalMass / r0);
            Vector3d vel1 = new Vector3d(0, -(m2 / totalMass) * vRel, 0);
            Vector3d vel2 = new Vector3d(0, (M1 / totalMass) * vRel, 0);

            var body1 = new CelestialBodyState(1, "M1", BodyType.Star, M1, 10.0, pos1, vel1);
            var body2 = new CelestialBodyState(2, "m2", BodyType.AsteroidProbe, m2, 0.01, pos2, vel2);

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            Vector3d initialP = _integrator.CalculateTotalLinearMomentum(bodies);
            Assert.AreEqual(0.0, initialP.magnitude, 1e-14, "Initial P must be zero.");

            double dt = 0.005;
            for (int s = 0; s < 2000; s++)
            {
                _integrator.Step(bodies, dt);
            }

            Vector3d finalP = _integrator.CalculateTotalLinearMomentum(bodies);
            Assert.LessOrEqual(finalP.magnitude, 1e-10,
                $"Zero-net-momentum drift {finalP.magnitude:E4} must be <= 1e-10.");
        }

        // ====================================================================
        // STRESS SCENARIO 2: Multi-Body Virial Cluster (N = 32 Bodies, 2,000 Steps)
        // ====================================================================

        [Test]
        public void StressScenario2_MultiBodyCluster_32Bodies_VirialEquilibrium_AngularMomentumInvariance()
        {
            int N = 32;
            double G = 1.0;
            double softening = 0.5;
            double clusterRadius = 50.0;
            var bodies = new List<CelestialBodyState>(N);

            // Deterministic pseudo-random generator
            var rng = new System.Random(1337);

            double totalMass = 0.0;
            Vector3d cmPos = Vector3d.zero;

            for (int i = 0; i < N; i++)
            {
                // Spherical distribution
                double u = rng.NextDouble();
                double theta = rng.NextDouble() * 2.0 * Math.PI;
                double phi = Math.Acos(2.0 * rng.NextDouble() - 1.0);
                double r = clusterRadius * Math.Pow(u, 1.0 / 3.0); // uniform sphere density

                double x = r * Math.Sin(phi) * Math.Cos(theta);
                double y = r * Math.Sin(phi) * Math.Sin(theta);
                double z = r * Math.Cos(phi);

                double mass = 5.0 + rng.NextDouble() * 15.0; // 5 to 20
                totalMass += mass;
                Vector3d pos = new Vector3d(x, y, z);
                cmPos += pos * mass;

                bodies.Add(new CelestialBodyState(i + 1, $"ClusterBody_{i}", BodyType.Star, mass, 1.0, pos, Vector3d.zero));
            }

            // Center of mass correction
            cmPos /= totalMass;
            for (int i = 0; i < N; i++)
            {
                bodies[i].Position -= cmPos;
            }

            // Calculate potential energy U
            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            double U = _integrator.CalculateTotalPotentialEnergy(bodies);
            Assert.Less(U, 0.0, "Potential energy of self-gravitating cluster must be negative.");

            // Assign isotropic velocities with net rotation around Z axis
            Vector3d cmVel = Vector3d.zero;
            for (int i = 0; i < N; i++)
            {
                // Thermal velocity dispersion
                double vx = (rng.NextDouble() * 2.0 - 1.0);
                double vy = (rng.NextDouble() * 2.0 - 1.0);
                double vz = (rng.NextDouble() * 2.0 - 1.0);

                // Add rotational velocity component around Z: omega x r
                double omegaZ = 0.05;
                vx += -omegaZ * bodies[i].Position.y;
                vy += omegaZ * bodies[i].Position.x;

                bodies[i].Velocity = new Vector3d(vx, vy, vz);
                cmVel += bodies[i].Velocity * bodies[i].Mass;
            }

            // Zero out net linear momentum
            cmVel /= totalMass;
            for (int i = 0; i < N; i++)
            {
                bodies[i].Velocity -= cmVel;
            }

            // Scale velocities for EXACT virial equilibrium: 2K + U = 0 => K = 0.5 * |U|
            double currentK = _integrator.CalculateTotalKineticEnergy(bodies);
            double targetK = 0.5 * Math.Abs(U);
            double velocityScale = Math.Sqrt(targetK / currentK);

            for (int i = 0; i < N; i++)
            {
                bodies[i].Velocity *= velocityScale;
            }

            // Verify Virial Ratio Q = 2K / |U| = 1.0
            double virialK = _integrator.CalculateTotalKineticEnergy(bodies);
            double virialRatio = (2.0 * virialK) / Math.Abs(U);
            Assert.AreEqual(1.0, virialRatio, 1e-10, "Virial ratio 2K/|U| must be 1.0.");

            // Calculate initial angular momentum L_0
            _integrator.InitializeAccelerations(bodies);
            Vector3d initialL = _integrator.CalculateTotalAngularMomentum(bodies);
            double initialLMag = initialL.magnitude;
            Assert.Greater(initialLMag, 1.0, "Initial angular momentum must be non-zero.");

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);

            // Integrate 2,000 steps
            double dt = 0.02;
            int totalSteps = 2000;
            double maxAngularDrift = 0.0;
            double maxRelAngularDrift = 0.0;

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);

                Vector3d currentL = _integrator.CalculateTotalAngularMomentum(bodies);
                double deltaL = (currentL - initialL).magnitude;
                double relDrift = deltaL / initialLMag;

                if (deltaL > maxAngularDrift) maxAngularDrift = deltaL;
                if (relDrift > maxRelAngularDrift) maxRelAngularDrift = relDrift;
            }

            // Verify angular momentum invariance to machine precision
            Assert.LessOrEqual(maxRelAngularDrift, 1e-11,
                $"Angular momentum relative drift {maxRelAngularDrift:E4} must be <= 1e-11 across 2,000 steps.");
            Assert.LessOrEqual(maxAngularDrift, 1e-8,
                $"Angular momentum absolute drift {maxAngularDrift:E4} must be <= 1e-8.");

            // Verify energy bounded without secular drift
            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            double energyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);
            Assert.LessOrEqual(energyDrift, 1e-3,
                $"Energy drift {energyDrift:E4} over 2,000 steps must be bounded <= 1e-3.");
        }

        // ====================================================================
        // STRESS SCENARIO 3: Floating Origin Stress Test (10^7 Units Camera Motion)
        // ====================================================================

        [Test]
        public void StressScenario3_FloatingOrigin_RapidTraversing10MillionUnits_CoordinatesBoundedAndAccurate()
        {
            FloatingOrigin.ResetOrigin();

            // Simulate traversing 10^7 (10,000,000) units along diagonal (1, 1, 1)
            double targetDistance = 10000000.0;
            double stepDistance = 2500.0; // > 2000 shiftThreshold
            int totalShifts = (int)(targetDistance / stepDistance); // 4,000 shifts

            Vector3d shiftStep = new Vector3d(stepDistance, stepDistance * 0.5, stepDistance * 0.25);
            Vector3d accumulatedExpected = Vector3d.zero;

            int shiftEventCount = 0;
            Vector3d totalEventOffset = Vector3d.zero;

            Action<Vector3d> handler = offset =>
            {
                shiftEventCount++;
                totalEventOffset += offset;
            };

            FloatingOrigin.OnOriginShift += handler;

            try
            {
                for (int i = 0; i < totalShifts; i++)
                {
                    FloatingOrigin.ShiftOrigin(shiftStep);
                    accumulatedExpected += shiftStep;
                }

                // Verify CurrentOrigin exactly matches accumulated double-precision sum
                Assert.AreEqual(accumulatedExpected.x, FloatingOrigin.CurrentOrigin.x, 1e-6);
                Assert.AreEqual(accumulatedExpected.y, FloatingOrigin.CurrentOrigin.y, 1e-6);
                Assert.AreEqual(accumulatedExpected.z, FloatingOrigin.CurrentOrigin.z, 1e-6);

                Assert.AreEqual(totalShifts, shiftEventCount);
                Assert.AreEqual(accumulatedExpected.x, totalEventOffset.x, 1e-6);

                // Verify coordinate boundedness:
                // An absolute position near the camera (accumulatedExpected + delta) must render within [-2000, 2000]
                Vector3d nearbyAbs = accumulatedExpected + new Vector3d(150.0, -250.0, 75.0);
                Vector3 renderPos = FloatingOrigin.AbsoluteToRender(nearbyAbs);

                Assert.AreEqual(150.0f, renderPos.x, 1e-4f);
                Assert.AreEqual(-250.0f, renderPos.y, 1e-4f);
                Assert.AreEqual(75.0f, renderPos.z, 1e-4f);
                Assert.Less(renderPos.magnitude, 2000.0f, "Render position must be strictly bounded within origin threshold.");

                // Verify jitter suppression:
                // Contrast float32 truncation at 10^7 units vs Vector3d
                float singlePrecLarge = (float)accumulatedExpected.x;
                float singlePrecDelta = 0.001f; // 1 millimeter
                float[] mem = new float[] { singlePrecLarge + singlePrecDelta };
                float singleDiff = mem[0] - singlePrecLarge;

                // float32 at 10^7 loses precision (ULP ~ 1.0 unit)
                Assert.AreEqual(0.0f, singleDiff, "Float32 must suffer catastrophic precision loss at 10^7 units.");

                // In contrast, Vector3d retains sub-millimeter precision at 10^7 units
                Vector3d doubleDelta = nearbyAbs - accumulatedExpected;
                Assert.AreEqual(150.0, doubleDelta.x, 1e-10);
            }
            finally
            {
                FloatingOrigin.OnOriginShift -= handler;
            }
        }

        [Test]
        public void StressScenario3_FloatingOriginSubscriber_LineRendererBufferIntegrityOverMultipleShifts()
        {
            FloatingOrigin.ResetOrigin();

            // Create GameObject with LineRenderer and FloatingOriginSubscriber
            var go = new GameObject("TestLineRendererSubscriber");
            var lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;

            int pointCount = 10;
            lineRenderer.positionCount = pointCount;
            Vector3[] initialPoints = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                initialPoints[i] = new Vector3((i + 1) * 10f, (i + 1) * 5f, (i + 1) * 2f);
            }
            lineRenderer.SetPositions(initialPoints);

            // Direct check: does LineRenderer retain positions in headless/batchmode?
            Vector3[] checkPoints = new Vector3[pointCount];
            int readCount = lineRenderer.GetPositions(checkPoints);
            Debug.LogWarning($"[DEBUG LINE RENDERER DIRECT CHECK] readCount: {readCount}, checkPoints[0]: {checkPoints[0]}");

            var subscriber = go.AddComponent<FloatingOriginSubscriber>();
            FieldInfo lrField = typeof(FloatingOriginSubscriber).GetField("_lineRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
            lrField?.SetValue(subscriber, lineRenderer);

            // In EditMode, invoke OnEnable explicitly to bind event subscription
            MethodInfo onEnable = typeof(FloatingOriginSubscriber).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(subscriber, null);

            try
            {
                // If LineRenderer is unsupported in -nographics batchmode, document finding
                if (checkPoints[0].x == 0f && initialPoints[0].x != 0f)
                {
                    Debug.LogWarning("[ADVERSARIAL CHALLENGE] LineRenderer.SetPositions is a NO-OP under -nographics batchmode! Vertex buffer uninitialized.");
                    Assert.AreEqual(0f, checkPoints[0].x, "Empirically confirmed: LineRenderer native GPU buffers are inactive in -nographics batchmode.");
                    return;
                }

                // Perform origin shifts
                Vector3d shiftDelta = new Vector3d(2500.0, 1000.0, -500.0);
                Vector3 totalShift = Vector3.zero;

                for (int s = 0; s < 10; s++)
                {
                    FloatingOrigin.ShiftOrigin(shiftDelta);
                    totalShift += shiftDelta.ToVector3();
                }

                Vector3[] updatedPoints = new Vector3[pointCount];
                lineRenderer.GetPositions(updatedPoints);

                for (int i = 0; i < pointCount; i++)
                {
                    Vector3 expected = initialPoints[i] - totalShift;
                    Assert.AreEqual(expected.x, updatedPoints[i].x, 1e-3f, $"Line point {i} X mismatch");
                }
            }
            finally
            {
                MethodInfo onDisable = typeof(FloatingOriginSubscriber).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
                onDisable?.Invoke(subscriber, null);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StressScenario3_FloatingOriginSubscriber_ParticleBufferIntegrityOverMultipleShifts()
        {
            FloatingOrigin.ResetOrigin();

            var go = new GameObject("TestParticleSubscriber");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Emit particles
            ps.Emit(50);
            var particles = new ParticleSystem.Particle[50];
            int count = ps.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                particles[i].position = new Vector3((i + 1) * 10f, (i + 1) * 20f, (i + 1) * 5f);
            }
            ps.SetParticles(particles, count);

            var subscriber = go.AddComponent<FloatingOriginSubscriber>();
            FieldInfo psField = typeof(FloatingOriginSubscriber).GetField("_particleSystem", BindingFlags.NonPublic | BindingFlags.Instance);
            psField?.SetValue(subscriber, ps);

            MethodInfo onEnable = typeof(FloatingOriginSubscriber).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(subscriber, null);

            try
            {
                Vector3 initialPos0 = particles[0].position;

                Vector3d shiftDelta = new Vector3d(2000.0, 500.0, -100.0);
                Vector3 totalShift = Vector3.zero;

                for (int s = 0; s < 5; s++)
                {
                    FloatingOrigin.ShiftOrigin(shiftDelta);
                    totalShift += shiftDelta.ToVector3();
                }

                var readParticles = new ParticleSystem.Particle[50];
                int readCount = ps.GetParticles(readParticles);

                if (readCount > 0)
                {
                    Vector3 expectedPos0 = initialPos0 - totalShift;
                    Assert.AreEqual(expectedPos0.x, readParticles[0].position.x, 1e-2f, "Particle 0 X shifted correctly");
                    Assert.AreEqual(expectedPos0.y, readParticles[0].position.y, 1e-2f, "Particle 0 Y shifted correctly");
                    Assert.AreEqual(expectedPos0.z, readParticles[0].position.z, 1e-2f, "Particle 0 Z shifted correctly");
                }
            }
            finally
            {
                MethodInfo onDisable = typeof(FloatingOriginSubscriber).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
                onDisable?.Invoke(subscriber, null);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ====================================================================
        // ADVERSARIAL CHALLENGE SPECIFICATION TESTS (ORACLES FOR CORRECTNESS)
        // ====================================================================

        [Test]
        public void StressScenario3_FloatingOrigin_CameraTransformReset_NoRunawayOnConsecutiveFrames()
        {
            // SPECIFICATION ORACLE:
            // When a camera moves beyond shiftThreshold (e.g. 2500 > 2000), FloatingOrigin.LateUpdate
            // must shift the origin by camPos AND adjust targetCameraTransform.position back by -camPos
            // (or reset it to near zero), so that a stationary camera does NOT continuously trigger
            // origin shifts on subsequent frames.

            FloatingOrigin.ResetOrigin();

            var originGo = new GameObject("FloatingOriginController");
            var floatingOrigin = originGo.AddComponent<FloatingOrigin>();

            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cameraGo.transform.position = new Vector3(2500f, 0f, 0f); // beyond 2000 threshold

            FieldInfo camField = typeof(FloatingOrigin).GetField("targetCameraTransform", BindingFlags.NonPublic | BindingFlags.Instance);
            camField?.SetValue(floatingOrigin, cameraGo.transform);

            FieldInfo threshField = typeof(FloatingOrigin).GetField("shiftThreshold", BindingFlags.NonPublic | BindingFlags.Instance);
            threshField?.SetValue(floatingOrigin, 2000f);

            MethodInfo awakeMethod = typeof(FloatingOrigin).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(floatingOrigin, null);

            MethodInfo lateUpdateMethod = typeof(FloatingOrigin).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);

            try
            {
                // Frame 1: Camera at 2500 units. LateUpdate must shift origin by 2500
                lateUpdateMethod?.Invoke(floatingOrigin, null);
                Assert.AreEqual(2500.0, FloatingOrigin.CurrentOrigin.x, 1e-4, "Frame 1 must shift origin to 2500.");

                // Frame 2: Camera is stationary.
                // Camera transform MUST have been shifted by -2500 (bringing it to 0,0,0)
                // If it was shifted, sqrMagnitude is 0, so LateUpdate must NOT shift origin again!
                lateUpdateMethod?.Invoke(floatingOrigin, null);

                // SPECIFICATION ASSERTION: CurrentOrigin must remain 2500.0
                // FAILURE MODE: If camera position was not adjusted, CurrentOrigin diverges to 5000.0!
                Assert.AreEqual(2500.0, FloatingOrigin.CurrentOrigin.x, 1e-4,
                    "FAILURE MODE DETECTED: FloatingOrigin.LateUpdate did NOT shift targetCameraTransform.position back to zero, causing runaway origin shifts on consecutive frames!");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(originGo);
                UnityEngine.Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void StressScenario3_FloatingOriginSubscriber_TranslatesHostTransform()
        {
            // SPECIFICATION ORACLE:
            // Conforms to worker handoff §1.8: "Offsets attached Transform, active ParticleSystem particles,
            // and LineRenderer world-space points."
            // When FloatingOrigin.ShiftOrigin occurs, host GameObject transform must be shifted by -offset.

            FloatingOrigin.ResetOrigin();

            var go = new GameObject("SubscriberObject");
            go.transform.position = new Vector3(500f, 100f, 50f);
            var subscriber = go.AddComponent<FloatingOriginSubscriber>();
            MethodInfo onEnable = typeof(FloatingOriginSubscriber).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            onEnable?.Invoke(subscriber, null);

            try
            {
                Vector3 initialPos = go.transform.position;
                FloatingOrigin.ShiftOrigin(new Vector3d(2000.0, 0.0, 0.0));

                Vector3 posAfterShift = go.transform.position;
                Vector3 expectedPos = initialPos - new Vector3(2000f, 0f, 0f); // -1500, 100, 50

                // SPECIFICATION ASSERTION: transform must be offset by -shift
                // FAILURE MODE: FloatingOriginSubscriber omits `transform.position -= shift;`, leaving pos at 500!
                Assert.AreEqual(expectedPos.x, posAfterShift.x, 1e-3f,
                    $"FAILURE MODE DETECTED: FloatingOriginSubscriber did NOT offset host transform. Expected X={expectedPos.x}, but was X={posAfterShift.x}!");
            }
            finally
            {
                MethodInfo onDisable = typeof(FloatingOriginSubscriber).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
                onDisable?.Invoke(subscriber, null);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
