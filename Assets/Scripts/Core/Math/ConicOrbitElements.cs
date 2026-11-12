// ============================================================================
// CosmoForge: Conic Orbit Elements & Conic Types
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Core.Math
{
    public enum ConicType
    {
        Circular,
        Elliptical,
        Parabolic,
        Hyperbolic
    }

    public struct ConicOrbitElements
    {
        public double SemiMajorAxis;
        public double Eccentricity;
        public double PeriapsisDistance;
        public double ApoapsisDistance;
        public double OrbitalPeriod;
        public double SpecificEnergy;
        public ConicType Type;
    }
}
