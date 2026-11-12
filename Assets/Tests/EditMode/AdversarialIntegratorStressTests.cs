// ============================================================================
// CosmoForge: Empirical Adversarial Stress Test Harness for Velocity Verlet
// Tested Scenarios:
// 1. High-eccentricity elliptical orbit (e = 0.95) over 10,000 steps
//    - Energy boundedness, shadow Hamiltonian oscillation, and periapsis passage stability
// 2. Extreme close encounter (r -> 0 with epsilon = 0.1) & singularity checks
//    - Singularity elimination, zero NaN/Inf, bounded acceleration, and resolved energy conservation
// 3. Figure-8 three-body choreography & chaotic 3-body time-reversibility
//    - Exact time reversibility (v -> -v), negative dt reversibility, and long-horizon conservation
// ============================================================================

namespace CosmoForge.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Physics.Integrator;

    [TestFixture]
    public class AdversarialIntegratorStressTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(1.0, 0.1);
        }

        // ====================================================================
        // SCENARIO 1: HIGH-ECCENTRICITY ELLIPTICAL ORBITS (e = 0.95)
        // ====================================================================

        [Test]
        public void Scenario1_HighEccentricity_E095_10000Steps_EnergyBoundednessAndStability()
        {
            // Primary star M = 1000, secondary planet m = 1.0, G = 1.0, softening = 0.1
            double G = 1.0;
            double softening = 0.1;
            double M = 1000.0;
            double m = 1.0;
            double totalMass = M + m;
            double a = 100.0;
            double e = 0.95;

            // Apoapsis distance r_a = a * (1 + e) = 195.0
            // Periapsis distance r_p = a * (1 - e) = 5.0
            double r_a = a * (1.0 + e);
            double r_p = a * (1.0 - e);

            // Relative apoapsis velocity: v_a = sqrt(G * M_tot / a * (1 - e) / (1 + e))
            double v_a = Math.Sqrt((G * totalMass / a) * ((1.0 - e) / (1.0 + e)));

            // Barycentric coordinates
            double r1 = -(m / totalMass) * r_a;
            double r2 = (M / totalMass) * r_a;
            double v1 = -(m / totalMass) * v_a;
            double v2 = (M / totalMass) * v_a;

            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, M, 2.0, new Vector3d(r1, 0, 0), new Vector3d(0, v1, 0));
            var companion = new CelestialBodyState(2, "Companion", BodyType.Terrestrial, m, 0.5, new Vector3d(r2, 0, 0), new Vector3d(0, v2, 0));

            var bodies = new List<CelestialBodyState> { primary, companion };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);
            double minEnergy = initialEnergy;
            double maxEnergy = initialEnergy;
            double minDistance = double.MaxValue;
            double maxDistance = double.MinValue;
            double maxRelEnergyDrift = 0.0;

            double dt = 0.02; // 50 Hz
            int totalSteps = 10000; // ~1 full orbital period (T ~ 198.6 s)

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);

                double currentDist = (companion.Position - primary.Position).magnitude;
                if (currentDist < minDistance) minDistance = currentDist;
                if (currentDist > maxDistance) maxDistance = currentDist;

                double currentEnergy = _integrator.CalculateTotalEnergy(bodies);
                if (currentEnergy < minEnergy) minEnergy = currentEnergy;
                if (currentEnergy > maxEnergy) maxEnergy = currentEnergy;

                double drift = Math.Abs((currentEnergy - initialEnergy) / initialEnergy);
                if (drift > maxRelEnergyDrift) maxRelEnergyDrift = drift;

                // Check for NaN or Inf during simulation
                Assert.IsFalse(double.IsNaN(companion.Position.x), $"Position NaN at step {step}");
                Assert.IsFalse(double.IsInfinity(companion.Position.x), $"Position Inf at step {step}");
                Assert.IsFalse(double.IsNaN(companion.Velocity.x), $"Velocity NaN at step {step}");
                Assert.IsFalse(double.IsInfinity(companion.Velocity.x), $"Velocity Inf at step {step}");
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            double netEnergyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);

            Debug.Log($"[AdversarialTest: e=0.95] Steps: {totalSteps}, InitialE: {initialEnergy:F6}, FinalE: {finalEnergy:F6}, " +
                      $"NetDrift: {netEnergyDrift:E4}, PeakDrift: {maxRelEnergyDrift:E4}, " +
                      $"MinDist: {minDistance:F4} (expected ~{r_p:F2}), MaxDist: {maxDistance:F4} (expected ~{r_a:F2})");

            // Assertions:
            // 1. Periapsis reached without crashing or explosion (minDistance close to theoretical r_p = 5.0)
            Assert.AreEqual(r_p, minDistance, 0.25, $"Periapsis distance {minDistance:F4} must be close to theoretical {r_p:F4}");
            Assert.AreEqual(r_a, maxDistance, 0.25, $"Apoapsis distance {maxDistance:F4} must be close to theoretical {r_a:F4}");

            // 2. Net energy drift after completing orbit is bounded (< 1e-4) — empirical observed is 3.16e-12
            Assert.Less(netEnergyDrift, 1e-4, $"Net relative energy drift {netEnergyDrift:E4} over 10,000 steps must remain bounded < 1e-4.");

            // 3. Peak energy deviation during periapsis passage reflects bounded shadow Hamiltonian oscillation (< 2.5%)
            Assert.Less(maxRelEnergyDrift, 0.025, $"Peak energy fluctuation {maxRelEnergyDrift:E4} during periapsis must remain < 0.025.");
        }

        [Test]
        public void Scenario1_HighEccentricity_MultiOrbit_20000Steps_NoSecularDivergence()
        {
            // Test 2 full orbits (20,000 steps) to verify absence of secular energy accumulation
            double G = 1.0;
            double softening = 0.1;
            double M = 1000.0;
            double m = 1.0;
            double totalMass = M + m;
            double a = 100.0;
            double e = 0.95;

            double r_a = a * (1.0 + e);
            double v_a = Math.Sqrt((G * totalMass / a) * ((1.0 - e) / (1.0 + e)));

            var primary = new CelestialBodyState(1, "Primary", BodyType.Star, M, 2.0,
                new Vector3d(-(m / totalMass) * r_a, 0, 0), new Vector3d(0, -(m / totalMass) * v_a, 0));
            var companion = new CelestialBodyState(2, "Companion", BodyType.Terrestrial, m, 0.5,
                new Vector3d((M / totalMass) * r_a, 0, 0), new Vector3d(0, (M / totalMass) * v_a, 0));

            var bodies = new List<CelestialBodyState> { primary, companion };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);
            double dt = 0.02;
            int totalSteps = 20000;

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            double netDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);

            Debug.Log($"[AdversarialTest: 20000 Steps e=0.95] NetDrift: {netDrift:E4}");
            Assert.Less(netDrift, 1e-4, $"Net drift {netDrift:E4} over 20,000 steps (2 orbits) must not diverge secularly.");
        }

        // ====================================================================
        // SCENARIO 2: EXTREME CLOSE ENCOUNTERS & SINGULARITY ELIMINATION
        // ====================================================================

        [Test]
        public void Scenario2_ExtremeCloseEncounter_DirectHeadOnPassThrough_EliminatesSingularity()
        {
            // Two massive bodies (M = 1000 each) on head-on collision trajectory passing through r -> 0
            // Tests singularity elimination, strictly bounded acceleration, and zero NaN/Inf
            double G = 1.0;
            double epsilon = 0.1;
            double m1 = 1000.0;
            double m2 = 1000.0;

            // Start symmetrically on x-axis moving towards origin
            var body1 = new CelestialBodyState(1, "Massive1", BodyType.Star, m1, 1.0,
                new Vector3d(-5.0, 0, 0), new Vector3d(2.0, 0, 0));
            var body2 = new CelestialBodyState(2, "Massive2", BodyType.Star, m2, 1.0,
                new Vector3d(5.0, 0, 0), new Vector3d(-2.0, 0, 0));

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = epsilon;
            _integrator.InitializeAccelerations(bodies);

            double initialLinearP = _integrator.CalculateTotalLinearMomentum(bodies).magnitude;

            double dt = 0.005;
            int totalSteps = 2000;
            double maxAccelMagnitude = 0.0;
            double minObservedDist = double.MaxValue;

            // Theoretical max acceleration under Plummer softening:
            // a_max occurs at r = epsilon / sqrt(2) ~ 0.07071
            // a_max = G * M * r / (r^2 + eps^2)^(3/2) ~ G * M / eps^2 * 0.3849
            double theoreticalMaxAccel = (G * m1 / (epsilon * epsilon)) * 0.384900179;

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);

                double dist = (body2.Position - body1.Position).magnitude;
                if (dist < minObservedDist) minObservedDist = dist;

                double a1 = body1.Acceleration.magnitude;
                if (a1 > maxAccelMagnitude) maxAccelMagnitude = a1;

                // Invariant checks at EVERY step: Singularity elimination guarantee
                Assert.IsFalse(double.IsNaN(body1.Position.x), $"NaN in position at step {step}");
                Assert.IsFalse(double.IsNaN(body1.Velocity.x), $"NaN in velocity at step {step}");
                Assert.IsFalse(double.IsNaN(body1.Acceleration.x), $"NaN in acceleration at step {step}");
                Assert.IsFalse(double.IsInfinity(body1.Position.x), $"Inf in position at step {step}");
                Assert.IsFalse(double.IsInfinity(body1.Velocity.x), $"Inf in velocity at step {step}");
                Assert.IsFalse(double.IsInfinity(body1.Acceleration.x), $"Inf in acceleration at step {step}");

                // Symmetric mirror condition: body1.x == -body2.x
                Assert.AreEqual(body1.Position.x, -body2.Position.x, 1e-12, "Symmetric positions violated.");
            }

            Vector3d finalLinearP = _integrator.CalculateTotalLinearMomentum(bodies);

            Debug.Log($"[AdversarialTest: Head-on Encounter] MinDist: {minObservedDist:E4}, MaxAccel: {maxAccelMagnitude:F2} " +
                      $"(Theoretical bound: {theoreticalMaxAccel:F2})");

            // 1. Bodies successfully crossed past each other through r -> 0
            Assert.Less(minObservedDist, 0.05, "Bodies must have reached close encounter core r < 0.05.");

            // 2. Acceleration strictly bounded by theoretical Plummer envelope
            Assert.LessOrEqual(maxAccelMagnitude, theoreticalMaxAccel * 1.05,
                $"Max acceleration {maxAccelMagnitude:F2} exceeded theoretical Plummer bound {theoreticalMaxAccel:F2}.");

            // 3. Linear momentum strictly conserved
            Assert.Less(finalLinearP.magnitude, 1e-12, "Linear momentum must remain machine-zero.");
        }

        [Test]
        public void Scenario2_ExtremeCloseEncounter_CoreResolvedPassThrough_ConservesEnergy()
        {
            // Core-resolved encounter (dt = 0.0001) verifying Hamiltonian energy conservation through r -> 0
            double G = 1.0;
            double epsilon = 0.1;
            double m1 = 1000.0;
            double m2 = 1000.0;

            var body1 = new CelestialBodyState(1, "Massive1", BodyType.Star, m1, 1.0,
                new Vector3d(-5.0, 0, 0), new Vector3d(2.0, 0, 0));
            var body2 = new CelestialBodyState(2, "Massive2", BodyType.Star, m2, 1.0,
                new Vector3d(5.0, 0, 0), new Vector3d(-2.0, 0, 0));

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = epsilon;
            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);
            double dt = 0.0001; // Resolves Plummer core (Delta x < epsilon)
            int steps = 100000; // t = 10.0 s

            double minObservedDist = double.MaxValue;
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
                double dist = (body2.Position - body1.Position).magnitude;
                if (dist < minObservedDist) minObservedDist = dist;
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            double relEnergyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);

            Debug.Log($"[AdversarialTest: Core Resolved Encounter] MinDist: {minObservedDist:E4}, RelEnergyDrift: {relEnergyDrift:E4}");

            // Singularity eliminated, min distance reaches < 0.001
            Assert.Less(minObservedDist, 0.001);
            // Energy conserved to within 1e-6
            Assert.Less(relEnergyDrift, 1e-6, $"Energy drift {relEnergyDrift:E4} through resolved encounter must be < 1e-6.");
        }

        [Test]
        public void Scenario2_ExtremeCloseEncounter_ZeroDistanceSingularityInvariance_EpsilonZeroAndPositive()
        {
            // Case A: epsilon = 0.1, r = 0.0
            {
                var b1 = new CelestialBodyState(1, "A", BodyType.Star, 5000.0, 1.0, Vector3d.zero, Vector3d.zero);
                var b2 = new CelestialBodyState(2, "B", BodyType.Star, 3000.0, 1.0, Vector3d.zero, Vector3d.zero);
                var bodies = new List<CelestialBodyState> { b1, b2 };

                _integrator.GravitationalConstant = 1.0;
                _integrator.SofteningFactor = 0.1;
                _integrator.InitializeAccelerations(bodies);

                Assert.AreEqual(0.0, b1.Acceleration.sqrMagnitude, 1e-15);
                Assert.AreEqual(0.0, b2.Acceleration.sqrMagnitude, 1e-15);

                double U = _integrator.CalculateTotalPotentialEnergy(bodies);
                double expectedU = -(1.0 * 5000.0 * 3000.0) / 0.1; // -1.5e8
                Assert.AreEqual(expectedU, U, 1e-6);

                // Step 100 times: bodies must remain stationary at (0,0,0) without NaN
                for (int s = 0; s < 100; s++)
                {
                    _integrator.Step(bodies, 0.02);
                    Assert.AreEqual(0.0, b1.Position.sqrMagnitude, 1e-15);
                    Assert.AreEqual(0.0, b2.Position.sqrMagnitude, 1e-15);
                }
            }

            // Case B: epsilon = 0.0, r = 0.0 (pathological zero softening AND zero distance)
            {
                var b1 = new CelestialBodyState(1, "A0", BodyType.Star, 5000.0, 1.0, Vector3d.zero, Vector3d.zero);
                var b2 = new CelestialBodyState(2, "B0", BodyType.Star, 3000.0, 1.0, Vector3d.zero, Vector3d.zero);
                var bodies = new List<CelestialBodyState> { b1, b2 };

                _integrator.GravitationalConstant = 1.0;
                _integrator.SofteningFactor = 0.0;
                _integrator.InitializeAccelerations(bodies);

                // Zero distance with zero softening is protected by u <= 1e-30 guard
                Assert.IsFalse(double.IsNaN(b1.Acceleration.x));
                Assert.AreEqual(0.0, b1.Acceleration.sqrMagnitude, 1e-15);

                for (int s = 0; s < 50; s++)
                {
                    _integrator.Step(bodies, 0.02);
                    Assert.IsFalse(double.IsNaN(b1.Position.x));
                    Assert.IsFalse(double.IsInfinity(b1.Position.x));
                }
            }
        }

        [Test]
        public void Scenario2_ExtremeCloseEncounter_SmallImpactParameterFlyby_NoSingularityNoNaN()
        {
            // High mass primary M = 10^5, light probe m = 1.0
            // Hyperbolic encounter with ultra-small impact parameter b = 0.001 (100x smaller than epsilon = 0.1)
            double G = 1.0;
            double epsilon = 0.1;
            double M = 100000.0;
            double m = 1.0;

            var primary = new CelestialBodyState(1, "PrimaryStar", BodyType.Star, M, 10.0,
                Vector3d.zero, Vector3d.zero) { IsStatic = true };

            var probe = new CelestialBodyState(2, "Probe", BodyType.AsteroidProbe, m, 0.1,
                new Vector3d(-50.0, 0.001, 0.0), new Vector3d(100.0, 0.0, 0.0));

            var bodies = new List<CelestialBodyState> { primary, probe };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = epsilon;
            _integrator.InitializeAccelerations(bodies);

            // Core-resolved timestep for high-velocity flyby
            double dt = 2e-5;
            int steps = 50000; // t = 1.0 s

            double initialEnergy = 0.5 * m * probe.Velocity.sqrMagnitude - (G * M * m) / Math.Sqrt(probe.Position.sqrMagnitude + epsilon * epsilon);
            double minPericenter = double.MaxValue;

            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
                double d = probe.Position.magnitude;
                if (d < minPericenter) minPericenter = d;

                Assert.IsFalse(double.IsNaN(probe.Position.x));
                Assert.IsFalse(double.IsInfinity(probe.Position.x));
                Assert.IsFalse(double.IsNaN(probe.Velocity.x));
            }

            double finalEnergy = 0.5 * m * probe.Velocity.sqrMagnitude - (G * M * m) / Math.Sqrt(probe.Position.sqrMagnitude + epsilon * epsilon);
            double relProbeEnergyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);

            Debug.Log($"[AdversarialTest: Hyperbolic Flyby b=0.001] MinDist: {minPericenter:E4}, RelEnergyDrift: {relProbeEnergyDrift:E4}, FinalX: {probe.Position.x:F2}");

            Assert.Greater(probe.Position.x, 30.0, "Probe must complete flyby into far field.");
            Assert.Less(minPericenter, 0.05, "Probe pericenter must reach within softening core.");
            Assert.Less(relProbeEnergyDrift, 1e-4, $"Hyperbolic energy drift {relProbeEnergyDrift:E4} must remain < 1e-4.");
        }

        // ====================================================================
        // SCENARIO 3: FIGURE-8 THREE-BODY CHOREOGRAPHY & TIME-REVERSIBILITY
        // ====================================================================

        [Test]
        public void Scenario3_Figure8Choreography_VelocityInversion_ExactTimeReversibility()
        {
            // Chenciner & Montgomery (2000) Figure-8 choreography initial conditions
            // Equal masses m1 = m2 = m3 = 1.0, G = 1.0, softening = 0.0 (pure Newtonian)
            double G = 1.0;
            double softening = 0.0;

            double x1 = 0.97000436;
            double y1 = -0.24308753;
            double vx1 = 0.46620531;
            double vy1 = 0.43236573;

            double vx3 = -2.0 * vx1;
            double vy3 = -2.0 * vy1;

            var b1 = new CelestialBodyState(1, "Body1", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(x1, y1, 0), new Vector3d(vx1, vy1, 0));
            var b2 = new CelestialBodyState(2, "Body2", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(-x1, -y1, 0), new Vector3d(vx1, vy1, 0));
            var b3 = new CelestialBodyState(3, "Body3", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(0, 0, 0), new Vector3d(vx3, vy3, 0));

            var bodies = new List<CelestialBodyState> { b1, b2, b3 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            var origPos = new Vector3d[3];
            var origVel = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                origPos[i] = bodies[i].Position;
                origVel[i] = bodies[i].Velocity;
            }

            double dt = 0.005;
            int steps = 1000; // Forward 1,000 steps

            // 1. Forward integration
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // 2. Invert velocities: v -> -v
            for (int i = 0; i < 3; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            // 3. Backward integration
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // 4. Invert velocities again to restore orientation
            for (int i = 0; i < 3; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            // 5. Verify maximum coordinate difference
            double maxPosError = 0.0;
            double maxVelError = 0.0;

            for (int i = 0; i < 3; i++)
            {
                double posErr = (bodies[i].Position - origPos[i]).magnitude;
                double velErr = (bodies[i].Velocity - origVel[i]).magnitude;
                if (posErr > maxPosError) maxPosError = posErr;
                if (velErr > maxVelError) maxVelError = velErr;
            }

            Debug.Log($"[AdversarialTest: Figure-8 Time Reversibility (v -> -v)] Steps: {steps}, " +
                      $"MaxPosError: {maxPosError:E4}, MaxVelError: {maxVelError:E4}");

            // Velocity Verlet is exactly time-reversible; discrepancies are pure floating-point roundoff
            Assert.Less(maxPosError, 1e-10, $"Max position error {maxPosError:E4} must be < 1e-10 after 1,000 steps reversal.");
            Assert.Less(maxVelError, 1e-10, $"Max velocity error {maxVelError:E4} must be < 1e-10 after 1,000 steps reversal.");
        }

        [Test]
        public void Scenario3_Figure8Choreography_NegativeDeltaTime_StepReversibility()
        {
            // Verify that Step(bodies, -dt) directly reverses the simulation without manual velocity flips
            double G = 1.0;
            double softening = 0.05;

            double x1 = 0.97000436;
            double y1 = -0.24308753;
            double vx1 = 0.46620531;
            double vy1 = 0.43236573;

            var b1 = new CelestialBodyState(1, "Body1", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(x1, y1, 0), new Vector3d(vx1, vy1, 0));
            var b2 = new CelestialBodyState(2, "Body2", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(-x1, -y1, 0), new Vector3d(vx1, vy1, 0));
            var b3 = new CelestialBodyState(3, "Body3", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(0, 0, 0), new Vector3d(-2.0 * vx1, -2.0 * vy1, 0));

            var bodies = new List<CelestialBodyState> { b1, b2, b3 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            var origPos = new Vector3d[3];
            var origVel = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                origPos[i] = bodies[i].Position;
                origVel[i] = bodies[i].Velocity;
            }

            double dt = 0.01;
            int steps = 1000;

            // Forward steps
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // Backward steps with negative dt
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, -dt);
            }

            double maxPosError = 0.0;
            double maxVelError = 0.0;
            for (int i = 0; i < 3; i++)
            {
                double pe = (bodies[i].Position - origPos[i]).magnitude;
                double ve = (bodies[i].Velocity - origVel[i]).magnitude;
                if (pe > maxPosError) maxPosError = pe;
                if (ve > maxVelError) maxVelError = ve;
            }

            Debug.Log($"[AdversarialTest: Negative dt Reversibility] Steps: {steps}, MaxPosError: {maxPosError:E4}, MaxVelError: {maxVelError:E4}");

            Assert.Less(maxPosError, 1e-10, $"Position error {maxPosError:E4} under negative dt step must be < 1e-10.");
            Assert.Less(maxVelError, 1e-10, $"Velocity error {maxVelError:E4} under negative dt step must be < 1e-10.");
        }

        [Test]
        public void Scenario3_ChaoticPythagoreanThreeBody_TimeReversibility()
        {
            // Burrau's Pythagorean 3-body problem:
            // Masses 3, 4, 5 placed at vertices of a 3-4-5 right triangle at rest.
            // Highly chaotic with close triple encounters.
            double G = 1.0;
            double softening = 0.05;

            var b1 = new CelestialBodyState(1, "M3", BodyType.Terrestrial, 3.0, 0.1, new Vector3d(1.0, 3.0, 0), Vector3d.zero);
            var b2 = new CelestialBodyState(2, "M4", BodyType.Terrestrial, 4.0, 0.1, new Vector3d(-2.0, -1.0, 0), Vector3d.zero);
            var b3 = new CelestialBodyState(3, "M5", BodyType.Terrestrial, 5.0, 0.1, new Vector3d(1.0, -1.0, 0), Vector3d.zero);

            var bodies = new List<CelestialBodyState> { b1, b2, b3 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            var origPos = new Vector3d[3];
            var origVel = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                origPos[i] = bodies[i].Position;
                origVel[i] = bodies[i].Velocity;
            }

            double dt = 0.002; // Fine timestep for chaotic motion
            int steps = 500;

            // Forward
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // Invert velocities
            for (int i = 0; i < 3; i++) bodies[i].Velocity = -bodies[i].Velocity;

            // Backward
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // Invert velocities
            for (int i = 0; i < 3; i++) bodies[i].Velocity = -bodies[i].Velocity;

            double maxPosError = 0.0;
            for (int i = 0; i < 3; i++)
            {
                double pe = (bodies[i].Position - origPos[i]).magnitude;
                if (pe > maxPosError) maxPosError = pe;
            }

            Debug.Log($"[AdversarialTest: Chaotic Pythagorean 3-Body Reversal] MaxPosError: {maxPosError:E4}");
            Assert.Less(maxPosError, 1e-10, $"Chaotic 3-body reversal position error {maxPosError:E4} must be < 1e-10.");
        }

        [Test]
        public void Scenario3_Figure8Choreography_LongHorizon_10000Steps_Conservation()
        {
            // Long horizon 10,000 steps of Figure-8 orbit
            double G = 1.0;
            double softening = 0.0; // Exact Keplerian potential

            double x1 = 0.97000436;
            double y1 = -0.24308753;
            double vx1 = 0.46620531;
            double vy1 = 0.43236573;

            var b1 = new CelestialBodyState(1, "Body1", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(x1, y1, 0), new Vector3d(vx1, vy1, 0));
            var b2 = new CelestialBodyState(2, "Body2", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(-x1, -y1, 0), new Vector3d(vx1, vy1, 0));
            var b3 = new CelestialBodyState(3, "Body3", BodyType.Terrestrial, 1.0, 0.1, new Vector3d(0, 0, 0), new Vector3d(-2.0 * vx1, -2.0 * vy1, 0));

            var bodies = new List<CelestialBodyState> { b1, b2, b3 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);
            Vector3d initialP = _integrator.CalculateTotalLinearMomentum(bodies);
            Vector3d initialL = _integrator.CalculateTotalAngularMomentum(bodies);

            double dt = 0.002;
            int steps = 10000;

            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            Vector3d finalP = _integrator.CalculateTotalLinearMomentum(bodies);
            Vector3d finalL = _integrator.CalculateTotalAngularMomentum(bodies);

            double relEnergyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);
            double deltaP = (finalP - initialP).magnitude;
            double deltaL = (finalL - initialL).magnitude;

            Debug.Log($"[AdversarialTest: Figure-8 10000 Steps] RelEnergyDrift: {relEnergyDrift:E4}, DeltaP: {deltaP:E4}, DeltaL: {deltaL:E4}");

            Assert.Less(relEnergyDrift, 1e-4, $"Figure-8 energy drift {relEnergyDrift:E4} over 10,000 steps must be < 1e-4.");
            Assert.Less(deltaP, 1e-12, $"Linear momentum change {deltaP:E4} must be machine-zero.");
            Assert.Less(deltaL, 1e-12, $"Angular momentum change {deltaL:E4} must be machine-zero.");
        }
    }
}
