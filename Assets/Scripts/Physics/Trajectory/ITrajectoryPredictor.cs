// ============================================================================
// CosmoForge: Trajectory Predictor Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Physics.Trajectory
{
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    public interface ITrajectoryPredictor
    {
        ConicOrbitElements CalculateOsculatingElements(CelestialBodyState body, CelestialBodyState dominantAttractor, double G);
        Vector3d[] PredictForwardGhostTrajectory(CelestialBodyState target, List<CelestialBodyState> allBodies, int maxSteps, double stepDeltaTime, double G, double softening);
    }
}
