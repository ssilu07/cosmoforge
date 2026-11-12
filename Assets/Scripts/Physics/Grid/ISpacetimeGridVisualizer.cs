// ============================================================================
// CosmoForge: Spacetime Grid Visualizer Contract
// Conforms to PROJECT.md § Architecture & Requirement F06
// ============================================================================

namespace CosmoForge.Physics.Grid
{
    using System.Collections.Generic;
    using CosmoForge.Celestial.Data;

    public interface ISpacetimeGridVisualizer
    {
        double CalculateWarpAt(double worldX, double worldZ, IList<CelestialBodyState> bodies);
        void UpdateGrid(IList<CelestialBodyState> bodies, double G, double c);
        void SetVisible(bool visible);
    }
}
