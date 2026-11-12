// ============================================================================
// CosmoForge: Workbench Controller & Mode Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.UI.Workbench
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Celestial.Data;

    public enum WorkbenchMode
    {
        FreeSandbox = 0,
        SolarSystemPreset = 1,
        OrbitalPuzzles = 2
    }

    public interface IWorkbenchController
    {
        WorkbenchMode ActiveMode { get; }
        bool IsPaused { get; set; }
        double TimeScale { get; set; }
        int SelectedBodyId { get; set; }
        int ModeChangeCount { get; }
        void SwitchMode(WorkbenchMode newMode, List<CelestialBodyState> activeBodies);
        void StepSimulation(List<CelestialBodyState> bodies, double dt);
        event Action<WorkbenchMode> OnModeChanged;
    }
}
