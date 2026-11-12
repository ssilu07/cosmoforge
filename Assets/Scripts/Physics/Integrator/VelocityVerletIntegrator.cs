// ============================================================================
// CosmoForge: Symplectic Velocity Verlet Integrator with Plummer Softening
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Physics.Integrator
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Symplectic 2nd-order Velocity Verlet N-body gravitational integrator.
    /// Incorporates Plummer softening factor epsilon to eliminate point-mass singularities.
    /// Preserves phase-space volume (symplectic 2-form), ensuring zero secular energy drift,
    /// strict linear and angular momentum conservation, and exact time-reversibility.
    /// </summary>
    public class VelocityVerletIntegrator : IGravityIntegrator
    {
        public double GravitationalConstant { get; set; } = 1.0;
        public double SofteningFactor { get; set; } = 0.1;

        public VelocityVerletIntegrator() { }

        public VelocityVerletIntegrator(double gravitationalConstant, double softeningFactor)
        {
            GravitationalConstant = gravitationalConstant;
            SofteningFactor = softeningFactor;
        }

        /// <summary>
        /// Computes gravitational accelerations at current positions for all bodies.
        /// Employs Newton's 3rd Law pairwise optimization (N*(N-1)/2 evaluations).
        /// </summary>
        public void ComputeAccelerations(List<CelestialBodyState> bodies)
        {
            if (bodies == null) return;
            int count = bodies.Count;
            double epsSq = SofteningFactor * SofteningFactor;
            double G = GravitationalConstant;

            // Reset accelerations
            for (int i = 0; i < count; i++)
            {
                bodies[i].Acceleration = Vector3d.zero;
            }

            // Symmetric pairwise evaluation: F_ji = -F_ij
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState bi = bodies[i];
                if (bi.IsDestroyed) continue;

                for (int j = i + 1; j < count; j++)
                {
                    CelestialBodyState bj = bodies[j];
                    if (bj.IsDestroyed) continue;
                    if (bi.IsStatic && bj.IsStatic) continue;

                    Vector3d r_ij = bj.Position - bi.Position;
                    double r2 = r_ij.sqrMagnitude;
                    double u = r2 + epsSq;
                    if (u <= 1e-30) continue;
                    double invDist = 1.0 / Math.Sqrt(u);
                    double invDist3 = invDist * invDist * invDist;
                    double factor = G * invDist3;

                    if (!bi.IsStatic)
                    {
                        bi.Acceleration += r_ij * (factor * bj.Mass);
                    }
                    if (!bj.IsStatic)
                    {
                        bj.Acceleration -= r_ij * (factor * bi.Mass);
                    }
                }
            }
        }

        /// <summary>
        /// Bootstraps initial accelerations prior to step 0.
        /// </summary>
        public void InitializeAccelerations(List<CelestialBodyState> bodies)
        {
            ComputeAccelerations(bodies);
        }

        /// <summary>
        /// Advances the simulation by deltaTime using the 4-stage Symplectic Velocity Verlet algorithm.
        /// Preserves phase space volume and shadow Hamiltonian, ensuring zero secular energy drift.
        /// Fully reversible under deltaTime &lt; 0.
        /// </summary>
        public void Step(List<CelestialBodyState> bodies, double deltaTime)
        {
            if (bodies == null || bodies.Count == 0) return;
            int count = bodies.Count;
            double halfDt = 0.5 * deltaTime;

            // Stage 1: Half-step velocity update (Kick)
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                b.Velocity += b.Acceleration * halfDt;
            }

            // Stage 2: Full-step position update (Drift)
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                b.Position += b.Velocity * deltaTime;
            }

            // Stage 3: Re-evaluate accelerations at new positions
            ComputeAccelerations(bodies);

            // Stage 4: Second half-step velocity update (Kick)
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                b.Velocity += b.Acceleration * halfDt;
            }
        }

        /// <summary>
        /// Convenience overload supporting argument ordering (deltaTime, bodies, G, softening).
        /// </summary>
        public void Step(double deltaTime, List<CelestialBodyState> bodies, double G, double softening)
        {
            GravitationalConstant = G;
            SofteningFactor = softening;
            Step(bodies, deltaTime);
        }

        /// <summary>
        /// Total Kinetic Energy: K = 0.5 * sum(m_i * |v_i|^2)
        /// </summary>
        public double CalculateTotalKineticEnergy(List<CelestialBodyState> bodies)
        {
            if (bodies == null) return 0.0;
            double totalK = 0.0;
            int count = bodies.Count;
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                totalK += 0.5 * b.Mass * b.Velocity.sqrMagnitude;
            }
            return totalK;
        }

        /// <summary>
        /// Total Softened Potential Energy: U = - sum_{i &lt; j} (G * m_i * m_j / sqrt(r_ij^2 + eps^2))
        /// Conforms strictly to the negative gradient of the Plummer softened force field.
        /// </summary>
        public double CalculateTotalPotentialEnergy(List<CelestialBodyState> bodies)
        {
            if (bodies == null) return 0.0;
            double totalU = 0.0;
            int count = bodies.Count;
            double epsSq = SofteningFactor * SofteningFactor;
            double G = GravitationalConstant;

            for (int i = 0; i < count; i++)
            {
                CelestialBodyState bi = bodies[i];
                if (bi.IsDestroyed) continue;

                for (int j = i + 1; j < count; j++)
                {
                    CelestialBodyState bj = bodies[j];
                    if (bj.IsDestroyed) continue;

                    Vector3d r_ij = bj.Position - bi.Position;
                    double r2 = r_ij.sqrMagnitude;
                    double denom = Math.Sqrt(r2 + epsSq);
                    if (denom > 1e-15)
                    {
                        totalU -= (G * bi.Mass * bj.Mass) / denom;
                    }
                }
            }
            return totalU;
        }

        public double CalculateTotalEnergy(List<CelestialBodyState> bodies)
        {
            return CalculateTotalKineticEnergy(bodies) + CalculateTotalPotentialEnergy(bodies);
        }

        /// <summary>
        /// Convenience overload supporting explicit G and softening parameters.
        /// </summary>
        public double CalculateTotalEnergy(List<CelestialBodyState> bodies, double G, double softening)
        {
            GravitationalConstant = G;
            SofteningFactor = softening;
            return CalculateTotalEnergy(bodies);
        }

        /// <summary>
        /// Total Linear Momentum: P = sum(m_i * v_i)
        /// Conserved to machine precision (zero internal net force).
        /// </summary>
        public Vector3d CalculateTotalLinearMomentum(List<CelestialBodyState> bodies)
        {
            if (bodies == null) return Vector3d.zero;
            Vector3d totalP = Vector3d.zero;
            int count = bodies.Count;
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                totalP += b.Velocity * b.Mass;
            }
            return totalP;
        }

        /// <summary>
        /// Total Angular Momentum: L = sum(m_i * (r_i x v_i))
        /// Conserved due to zero net internal central torque.
        /// </summary>
        public Vector3d CalculateTotalAngularMomentum(List<CelestialBodyState> bodies)
        {
            if (bodies == null) return Vector3d.zero;
            Vector3d totalL = Vector3d.zero;
            int count = bodies.Count;
            for (int i = 0; i < count; i++)
            {
                CelestialBodyState b = bodies[i];
                if (b.IsDestroyed || b.IsStatic) continue;
                totalL += Vector3d.Cross(b.Position, b.Velocity) * b.Mass;
            }
            return totalL;
        }
    }
}
