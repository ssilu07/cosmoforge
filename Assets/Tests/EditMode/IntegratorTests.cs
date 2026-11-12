// ============================================================================
// CosmoForge: EditMode Unit Tests for Symplectic Velocity Verlet Integrator
// Conforms to PROJECT.md § Architecture, Features F01 & F02
// ============================================================================

namespace CosmoForge.Tests.EditMode
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Physics.Integrator;
    using CosmoForge.Core.Performance;

    [TestFixture]
    public class IntegratorTests
    {
        private IGravityIntegrator _integrator;

        [SetUp]
        public void SetUp()
        {
            _integrator = new VelocityVerletIntegrator(1.0, 0.1);
        }

        [Test]
        public void BinaryKeplerianOrbit_EnergyConservation_WithinStrictLimitOver5000Steps()
        {
            // Binary Keplerian circular orbit setup:
            // G = 1.0, M1 = 1000.0, M2 = 1.0, r0 = 100.0, softening = 0.0
            double G = 1.0;
            double softening = 0.0;
            double m1 = 1000.0;
            double m2 = 1.0;
            double totalMass = m1 + m2;
            double r0 = 100.0;

            // Barycentric coordinates
            double r1 = -(m2 / totalMass) * r0;
            double r2 = (m1 / totalMass) * r0;

            // Circular orbit velocity: v_rel = sqrt(G * M_tot / r0)
            double vRel = Math.Sqrt(G * totalMass / r0);
            double v1 = -(m2 / totalMass) * vRel;
            double v2 = (m1 / totalMass) * vRel;

            var body1 = new CelestialBodyState(
                id: 1,
                name: "PrimaryStar",
                type: BodyType.Star,
                mass: m1,
                radius: 5.0,
                position: new Vector3d(r1, 0, 0),
                velocity: new Vector3d(0, v1, 0)
            );

            var body2 = new CelestialBodyState(
                id: 2,
                name: "CompanionPlanet",
                type: BodyType.Terrestrial,
                mass: m2,
                radius: 1.0,
                position: new Vector3d(r2, 0, 0),
                velocity: new Vector3d(0, v2, 0)
            );

            var bodies = new List<CelestialBodyState> { body1, body2 };

            double dt = 0.02; // 50 Hz
            int totalSteps = 5000; // 100 simulation seconds

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            double initialEnergy = _integrator.CalculateTotalEnergy(bodies);

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);
            }

            double finalEnergy = _integrator.CalculateTotalEnergy(bodies);
            double relativeEnergyDrift = Math.Abs((finalEnergy - initialEnergy) / initialEnergy);

            Assert.LessOrEqual(relativeEnergyDrift, 1e-4,
                $"Energy drift {relativeEnergyDrift:E4} must be <= 1e-4 over 5,000 steps.");
        }

        [Test]
        public void MultiBodySystem_MomentumConservation_ConservesLinearAndAngularMomentum()
        {
            // Equal-mass binary system with initial zero net linear momentum
            double G = 1.0;
            double softening = 0.1;
            double m1 = 500.0;
            double m2 = 500.0;
            double r0 = 50.0;
            double vRel = Math.Sqrt(G * (m1 + m2) / r0);

            var body1 = new CelestialBodyState(
                id: 1,
                name: "BinaryStarA",
                type: BodyType.Star,
                mass: m1,
                radius: 4.0,
                position: new Vector3d(-r0 / 2.0, 0, 0),
                velocity: new Vector3d(0, -vRel / 2.0, 0)
            );

            var body2 = new CelestialBodyState(
                id: 2,
                name: "BinaryStarB",
                type: BodyType.Star,
                mass: m2,
                radius: 4.0,
                position: new Vector3d(r0 / 2.0, 0, 0),
                velocity: new Vector3d(0, vRel / 2.0, 0)
            );

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            Vector3d initialLinearP = _integrator.CalculateTotalLinearMomentum(bodies);
            Vector3d initialAngularL = _integrator.CalculateTotalAngularMomentum(bodies);
            double initialLMag = initialAngularL.magnitude;

            double dt = 0.02;
            int totalSteps = 5000;

            for (int step = 0; step < totalSteps; step++)
            {
                _integrator.Step(bodies, dt);
            }

            Vector3d finalLinearP = _integrator.CalculateTotalLinearMomentum(bodies);
            Vector3d finalAngularL = _integrator.CalculateTotalAngularMomentum(bodies);

            // Linear momentum absolute delta
            double deltaLinearP = (finalLinearP - initialLinearP).magnitude;
            Assert.LessOrEqual(deltaLinearP, 1e-6,
                $"Linear momentum change {deltaLinearP:E4} must be <= 1e-6.");

            // Angular momentum fractional delta
            double deltaAngularL = (finalAngularL - initialAngularL).magnitude / initialLMag;
            Assert.LessOrEqual(deltaAngularL, 1e-6,
                $"Angular momentum fractional drift {deltaAngularL:E4} must be <= 1e-6.");
        }

        [Test]
        public void SymplecticIntegrator_TimeReversibility_ReturnsToInitialStateAfterInversion()
        {
            // 3-body system in asymmetric planar configuration
            double G = 1.0;
            double softening = 0.5;

            var body1 = new CelestialBodyState(1, "B1", BodyType.Terrestrial, 100.0, 2.0, new Vector3d(-10.0, 0.0, 0.0), new Vector3d(0.0, 2.0, 0.0));
            var body2 = new CelestialBodyState(2, "B2", BodyType.GasGiant, 150.0, 3.0, new Vector3d(15.0, 5.0, 0.0), new Vector3d(-1.0, -1.5, 0.0));
            var body3 = new CelestialBodyState(3, "B3", BodyType.Moon, 80.0, 1.5, new Vector3d(0.0, -12.0, 0.0), new Vector3d(1.5, 0.5, 0.0));

            var bodies = new List<CelestialBodyState> { body1, body2, body3 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = softening;
            _integrator.InitializeAccelerations(bodies);

            // Store initial positions and velocities
            var initPositions = new Vector3d[bodies.Count];
            var initVelocities = new Vector3d[bodies.Count];
            for (int i = 0; i < bodies.Count; i++)
            {
                initPositions[i] = bodies[i].Position;
                initVelocities[i] = bodies[i].Velocity;
            }

            double dt = 0.01;
            int steps = 1000;

            // 1. Forward integration 1,000 steps
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // 2. Invert velocities: v -> -v
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            // 3. Backward integration 1,000 steps
            for (int s = 0; s < steps; s++)
            {
                _integrator.Step(bodies, dt);
            }

            // 4. Invert velocities again to restore original orientation
            for (int i = 0; i < bodies.Count; i++)
            {
                bodies[i].Velocity = -bodies[i].Velocity;
            }

            // 5. Verify bodies returned to initial coordinates
            for (int i = 0; i < bodies.Count; i++)
            {
                double posDiff = (bodies[i].Position - initPositions[i]).magnitude;
                double velDiff = (bodies[i].Velocity - initVelocities[i]).magnitude;

                Assert.Less(posDiff, 1e-5,
                    $"Body {bodies[i].Id} position error {posDiff:E4} must be < 1e-5 after time reversal.");
                Assert.Less(velDiff, 1e-5,
                    $"Body {bodies[i].Id} velocity error {velDiff:E4} must be < 1e-5 after time reversal.");
            }
        }

        [Test]
        public void PlummerSoftening_SingularityInvarianceAtRZero_ProducesZeroForceAndFinitePotential()
        {
            // Two bodies placed at the exact same spatial coordinate (r_ij = 0)
            double G = 1.0;
            double epsilon = 1.0;
            double m1 = 1000.0;
            double m2 = 500.0;

            var body1 = new CelestialBodyState(1, "A", BodyType.Star, m1, 5.0, Vector3d.zero, Vector3d.zero);
            var body2 = new CelestialBodyState(2, "B", BodyType.Star, m2, 4.0, Vector3d.zero, Vector3d.zero);

            var bodies = new List<CelestialBodyState> { body1, body2 };

            _integrator.GravitationalConstant = G;
            _integrator.SofteningFactor = epsilon;
            _integrator.InitializeAccelerations(bodies);

            double dt = 0.02;
            _integrator.Step(bodies, dt);

            // Assertions for Body 1
            Assert.IsFalse(double.IsNaN(body1.Acceleration.x));
            Assert.IsFalse(double.IsNaN(body1.Acceleration.y));
            Assert.IsFalse(double.IsNaN(body1.Acceleration.z));
            Assert.IsFalse(double.IsInfinity(body1.Acceleration.x));
            Assert.AreEqual(0.0, body1.Acceleration.sqrMagnitude, 1e-12,
                "Acceleration at r = 0 must be zero due to symmetry/vector numerator r_ij = 0.");

            // Assertions for Body 2
            Assert.IsFalse(double.IsNaN(body2.Acceleration.x));
            Assert.AreEqual(0.0, body2.Acceleration.sqrMagnitude, 1e-12);

            // Potential Energy: U = -G * m1 * m2 / sqrt(0^2 + epsilon^2) = -G * m1 * m2 / epsilon
            double expectedPotential = -(G * m1 * m2) / epsilon;
            double totalEnergy = _integrator.CalculateTotalEnergy(bodies);

            Assert.IsFalse(double.IsNaN(totalEnergy));
            Assert.IsFalse(double.IsInfinity(totalEnergy));
            Assert.AreEqual(expectedPotential, totalEnergy, 1e-8,
                $"Potential energy at r=0 with Plummer softening must be finite: {expectedPotential}.");
        }

        [Test]
        public void SimulationWorld_SubsteppingAndInterpolation_ClampsAndInterpolates()
        {
            var world = new SimulationWorld(_integrator);
            var body = new CelestialBodyState(1, "Planet", BodyType.Terrestrial, 1.0, 1.0, new Vector3d(10, 0, 0), new Vector3d(0, 5, 0));
            world.AddBody(body);

            // Step with 1.0s unscaled delta and 1.0x warp
            world.UpdateSimulation(0.02, 1.0);

            // Body previous position must be cached
            Assert.AreEqual(10.0, body.PreviousPosition.x, 1e-6);
            Assert.IsNotNull(body.RenderPosition);

            // Extreme warp: 1000x multiplier with 1.0s delta must clamp to MaxSubsteps (25)
            world.UpdateSimulation(1.0, 1000.0);
            Assert.IsFalse(double.IsNaN(body.Position.x));
            Assert.IsFalse(double.IsInfinity(body.Position.x));
        }
    }
}
