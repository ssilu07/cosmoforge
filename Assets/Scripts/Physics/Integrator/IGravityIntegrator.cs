// ============================================================================
// CosmoForge: Gravity Integrator Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Physics.Integrator
{
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Symplectic N-body gravity integrator contract.
    /// Defines conserved physical quantities, Plummer softening, and stepping.
    /// </summary>
    public interface IGravityIntegrator
    {
        double GravitationalConstant { get; set; }
        double SofteningFactor { get; set; } // Plummer epsilon

        void Step(List<CelestialBodyState> bodies, double deltaTime);
        void InitializeAccelerations(List<CelestialBodyState> bodies);
        double CalculateTotalKineticEnergy(List<CelestialBodyState> bodies);
        double CalculateTotalPotentialEnergy(List<CelestialBodyState> bodies);
        double CalculateTotalEnergy(List<CelestialBodyState> bodies);
        Vector3d CalculateTotalLinearMomentum(List<CelestialBodyState> bodies);
        Vector3d CalculateTotalAngularMomentum(List<CelestialBodyState> bodies);
    }
}
