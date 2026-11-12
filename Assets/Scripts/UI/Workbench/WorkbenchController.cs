// ============================================================================
// CosmoForge: Workbench Controller & Time Warp Manager (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F23, F24
// ============================================================================

namespace CosmoForge.UI.Workbench
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Coordinates workbench modes (Free Sandbox, Solar System Preset, Orbital Puzzles),
    /// time-warp stepping (0.1x to 1000x), and reverse time rewind buffer.
    /// </summary>
    public class WorkbenchController : MonoBehaviour, IWorkbenchController
    {
        [Header("State")]
        [SerializeField] private WorkbenchMode activeMode = WorkbenchMode.FreeSandbox;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private double timeScale = 1.0;
        [SerializeField] private int selectedBodyId = -1;

        public event Action<WorkbenchMode> OnModeChanged;

        public WorkbenchMode ActiveMode => activeMode;
        public bool IsPaused { get => isPaused; set => isPaused = value; }
        public double TimeScale { get => timeScale; set => timeScale = value; }
        public int SelectedBodyId { get => selectedBodyId; set => selectedBodyId = value; }
        public int ModeChangeCount { get; private set; } = 0;

        // Circular buffer for reverse time rewind
        private const int MaxRewindFrames = 600; // 10-12 seconds of history at 50Hz
        private readonly LinkedList<List<BodySnapshot>> _rewindHistory = new LinkedList<List<BodySnapshot>>();

        public struct BodySnapshot
        {
            public int Id;
            public Vector3d Position;
            public Vector3d Velocity;
            public double Mass;
            public double Radius;
            public bool IsDestroyed;
        }

        public void SwitchMode(WorkbenchMode newMode, List<CelestialBodyState> activeBodies)
        {
            activeMode = newMode;
            ModeChangeCount++;
            _rewindHistory.Clear();

            if (activeBodies != null)
            {
                activeBodies.Clear();

                if (newMode == WorkbenchMode.SolarSystemPreset)
                {
                    PopulateSolarSystem(activeBodies);
                }
            }

            if (newMode == WorkbenchMode.OrbitalPuzzles)
            {
                isPaused = true;
                timeScale = 0.0;
            }
            else
            {
                isPaused = false;
                timeScale = 1.0;
            }

            OnModeChanged?.Invoke(newMode);
        }

        private void PopulateSolarSystem(List<CelestialBodyState> bodies)
        {
            double G = 1.0;
            double mSun = 10000.0;

            // Sun
            var sun = new CelestialBodyState(1, "Sun", BodyType.Star, mSun, 12.0, Vector3d.zero, Vector3d.zero)
            {
                SurfaceTemperature = 5778.0,
                CoreFuel = 1.0
            };
            bodies.Add(sun);

            // Mercury
            AddOrbitingPlanet(bodies, 2, "Mercury", BodyType.Terrestrial, 0.055, 0.8, 35.0, G, mSun, 440.0);
            // Venus
            AddOrbitingPlanet(bodies, 3, "Venus", BodyType.Terrestrial, 0.815, 1.2, 55.0, G, mSun, 737.0);
            // Earth
            var earth = AddOrbitingPlanet(bodies, 4, "Earth", BodyType.Terrestrial, 1.0, 1.5, 80.0, G, mSun, 288.0);
            // Moon
            double rMoon = 3.0;
            double vMoon = Math.Sqrt(G * earth.Mass / rMoon);
            bodies.Add(new CelestialBodyState(5, "Moon", BodyType.Moon, 0.0123, 0.4, earth.Position + new Vector3d(rMoon, 0, 0), earth.Velocity + new Vector3d(0, vMoon, 0)) { SurfaceTemperature = 250.0 });
            // Mars
            AddOrbitingPlanet(bodies, 6, "Mars", BodyType.Terrestrial, 0.107, 1.0, 115.0, G, mSun, 210.0);
            // Jupiter
            AddOrbitingPlanet(bodies, 7, "Jupiter", BodyType.GasGiant, 317.8, 4.5, 180.0, G, mSun, 165.0);
            // Saturn
            AddOrbitingPlanet(bodies, 8, "Saturn", BodyType.GasGiant, 95.2, 3.8, 260.0, G, mSun, 134.0);
        }

        private CelestialBodyState AddOrbitingPlanet(List<CelestialBodyState> bodies, int id, string name, BodyType type, double mass, double radius, double dist, double G, double mSun, double temp)
        {
            double vCirc = Math.Sqrt(G * mSun / dist);
            var planet = new CelestialBodyState(id, name, type, mass, radius, new Vector3d(dist, 0.0, 0.0), new Vector3d(0.0, vCirc, 0.0))
            {
                SurfaceTemperature = temp,
                CoreFuel = 0.0
            };
            bodies.Add(planet);
            return planet;
        }

        public void StepSimulation(List<CelestialBodyState> bodies, double dt)
        {
            if (isPaused || timeScale == 0.0 || bodies == null) return;

            if (timeScale < 0.0)
            {
                // Reverse time rewind from history buffer
                if (_rewindHistory.Count > 0)
                {
                    var snapshot = _rewindHistory.Last.Value;
                    _rewindHistory.RemoveLast();

                    for (int i = 0; i < bodies.Count; i++)
                    {
                        var b = bodies[i];
                        for (int s = 0; s < snapshot.Count; s++)
                        {
                            if (snapshot[s].Id == b.Id)
                            {
                                b.Position = snapshot[s].Position;
                                b.Velocity = snapshot[s].Velocity;
                                b.Mass = snapshot[s].Mass;
                                b.Radius = snapshot[s].Radius;
                                b.IsDestroyed = snapshot[s].IsDestroyed;
                                break;
                            }
                        }
                    }
                }
                return;
            }

            // Normal or fast-forward simulation: record snapshot before integration
            RecordSnapshot(bodies);

            double effectiveDt = dt * timeScale;
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == null || b.IsDestroyed) continue;
                b.Position += b.Velocity * effectiveDt;
            }
        }

        private void RecordSnapshot(List<CelestialBodyState> bodies)
        {
            var snap = new List<BodySnapshot>(bodies.Count);
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == null) continue;
                snap.Add(new BodySnapshot
                {
                    Id = b.Id,
                    Position = b.Position,
                    Velocity = b.Velocity,
                    Mass = b.Mass,
                    Radius = b.Radius,
                    IsDestroyed = b.IsDestroyed
                });
            }

            _rewindHistory.AddLast(snap);
            if (_rewindHistory.Count > MaxRewindFrames)
            {
                _rewindHistory.RemoveFirst();
            }
        }
    }
}
