// ============================================================================
// CosmoForge: Live Orbital Telemetry Inspector (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F26
// ============================================================================

namespace CosmoForge.UI.Telemetry
{
    using UnityEngine;
    using UnityEngine.UI;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Physics.Trajectory;

    /// <summary>
    /// Displays real-time live orbital telemetry for the currently selected body:
    /// Name, Type, Mass (M_earth), Radius (R_earth), Velocity (km/s),
    /// Orbital Period, Eccentricity (e), Semi-Major Axis (a), and Surface Temp (K).
    /// </summary>
    public class TelemetryInspector : MonoBehaviour
    {
        [Header("UI Text Fields")]
        [SerializeField] private Text bodyNameText;
        [SerializeField] private Text bodyTypeText;
        [SerializeField] private Text massText;
        [SerializeField] private Text radiusText;
        [SerializeField] private Text velocityText;
        [SerializeField] private Text periodText;
        [SerializeField] private Text eccentricityText;
        [SerializeField] private Text semiMajorAxisText;
        [SerializeField] private Text surfaceTempText;

        private readonly ITrajectoryPredictor _predictor = new KeplerianPredictor();

        public void DisplayTelemetry(CelestialBodyState selectedBody, CelestialBodyState dominantAttractor, double G)
        {
            if (selectedBody == null)
            {
                ClearTelemetry();
                return;
            }

            if (bodyNameText != null) bodyNameText.text = selectedBody.Name;
            if (bodyTypeText != null) bodyTypeText.text = selectedBody.Type.ToString();
            if (massText != null) massText.text = $"{selectedBody.Mass:N2} M⊕";
            if (radiusText != null) radiusText.text = $"{selectedBody.Radius:N2} R⊕";
            if (velocityText != null) velocityText.text = $"{selectedBody.Velocity.magnitude:N2} km/s";
            if (surfaceTempText != null) surfaceTempText.text = $"{selectedBody.SurfaceTemperature:N0} K";

            if (dominantAttractor != null && dominantAttractor.Id != selectedBody.Id)
            {
                var conic = _predictor.CalculateOsculatingElements(selectedBody, dominantAttractor, G);
                if (eccentricityText != null) eccentricityText.text = $"{conic.Eccentricity:F4}";
                if (semiMajorAxisText != null) semiMajorAxisText.text = double.IsInfinity(conic.SemiMajorAxis) ? "∞" : $"{conic.SemiMajorAxis:N2} AU";
                if (periodText != null) periodText.text = double.IsInfinity(conic.OrbitalPeriod) ? "Hyperbolic Flyby" : $"{conic.OrbitalPeriod:N1} d";
            }
            else
            {
                if (eccentricityText != null) eccentricityText.text = "N/A";
                if (semiMajorAxisText != null) semiMajorAxisText.text = "N/A";
                if (periodText != null) periodText.text = "N/A";
            }
        }

        public void ClearTelemetry()
        {
            if (bodyNameText != null) bodyNameText.text = "No Body Selected";
            if (bodyTypeText != null) bodyTypeText.text = "—";
            if (massText != null) massText.text = "—";
            if (radiusText != null) radiusText.text = "—";
            if (velocityText != null) velocityText.text = "—";
            if (periodText != null) periodText.text = "—";
            if (eccentricityText != null) eccentricityText.text = "—";
            if (semiMajorAxisText != null) semiMajorAxisText.text = "—";
            if (surfaceTempText != null) surfaceTempText.text = "—";
        }
    }
}
