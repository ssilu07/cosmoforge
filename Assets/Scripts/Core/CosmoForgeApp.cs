// ============================================================================
// CosmoForge: Master Application Coordinator (MonoBehaviour)
// Production Integration conforming to User Specifications & Requirements F01 - F28
// ============================================================================

namespace CosmoForge.Core
{
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Celestial.Controllers;
    using CosmoForge.Physics.Integrator;
    using CosmoForge.Physics.Trajectory;
    using CosmoForge.Physics.Grid;
    using CosmoForge.Physics.Cataclysms;
    using CosmoForge.Puzzles.Core;
    using CosmoForge.Puzzles.Missions;
    using CosmoForge.UI.Workbench;
    using CosmoForge.UI.Telemetry;
    using CosmoForge.UI.Input;
    using CosmoForge.UI.Feedback;
    using CosmoForge.Audio;

    /// <summary>
    /// Master application coordinator for CosmoForge: The Gravity &amp; Cosmos Sandbox.
    /// Wires the symplectic physics integrator, celestial body lifecycle,
    /// 500-step trajectory predictor, 2.5D spacetime grid, collision cataclysms,
    /// puzzle missions, 3D tactile UI, and pure procedural audio DSP.
    /// </summary>
    public class CosmoForgeApp : MonoBehaviour
    {
        [Header("Physics Configuration")]
        [SerializeField] private double gravitationalConstant = 1.0;
        [SerializeField] private double softeningFactor = 0.05;
        [SerializeField] private double speedOfLight = 50.0;

        [Header("Subsystem References")]
        [SerializeField] private FloatingOrigin floatingOrigin;
        [SerializeField] private SpacetimeCurvatureGrid spacetimeGrid;
        [SerializeField] private TrajectoryRenderer trajectoryRenderer;
        [SerializeField] private WorkbenchController workbench;
        [SerializeField] private TouchGestureHandler touchHandler;
        [SerializeField] private SlingshotController slingshot;
        [SerializeField] private TelemetryInspector telemetryInspector;
        [SerializeField] private PuzzleEngine puzzleEngine;
        [SerializeField] private ProceduralAudioSynthesizer audioSynth;
        [SerializeField] private ConfettiCelebration celebration;
        [SerializeField] private GameObject celestialBodyPrefab;

        private SimulationWorld _world;
        private VelocityVerletIntegrator _integrator;
        private CollisionEngine _collisionEngine;
        private readonly Dictionary<int, CelestialBody> _bodyControllers = new Dictionary<int, CelestialBody>();

        public SimulationWorld World => _world;
        public IGravityIntegrator Integrator => _integrator;

        private void Awake()
        {
            // Lock target frame rate to 60 FPS for smooth mobile performance
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // Initialize Symplectic Velocity Verlet Integrator
            _integrator = new VelocityVerletIntegrator(gravitationalConstant, softeningFactor);
            _world = new SimulationWorld(_integrator);
            _collisionEngine = new CollisionEngine();

            InitializeSubsystems();
        }

        private void Start()
        {
            // Register all 5 gamified puzzle missions
            if (puzzleEngine != null)
            {
                puzzleEngine.BindSimulationWorld(_world);
                puzzleEngine.RegisterMission(new OrbitalInjectionMission());
                puzzleEngine.RegisterMission(new LagrangeParkingMission());
                puzzleEngine.RegisterMission(new PlanetaryDefenseMission());
                puzzleEngine.RegisterMission(new BarycentricBalletMission());
                puzzleEngine.RegisterMission(new RingSmithMission());
            }

            // Start in Solar System Preset mode by default
            if (workbench != null)
            {
                workbench.SwitchMode(WorkbenchMode.SolarSystemPreset, _world.Bodies);
                SyncBodyControllers();
            }
        }

        private void InitializeSubsystems()
        {
            if (slingshot != null)
            {
                slingshot.OnBodyLaunched += HandleBodyLaunched;
            }

            if (workbench != null)
            {
                workbench.OnModeChanged += HandleModeChanged;
            }

            if (puzzleEngine != null)
            {
                puzzleEngine.OnMissionCompleted += HandleMissionCompleted;
            }
        }

        private void FixedUpdate()
        {
            if (workbench == null || workbench.IsPaused || workbench.TimeScale == 0.0) return;

            // Step physical simulation
            _world.UpdateSimulation(Time.fixedDeltaTime, workbench.TimeScale);

            // Evaluate collisions and mergers
            _collisionEngine.CheckAndResolveCollisions(_world.Bodies, SimulationWorld.FixedDeltaTime, OnBodyCollisionMerged);

            // Evaluate fluid Roche limit tidal disruptions
            _collisionEngine.EvaluateRocheLimits(_world.Bodies, OnRocheLimitDisrupted);

            // Evaluate active puzzle mission
            if (workbench.ActiveMode == WorkbenchMode.OrbitalPuzzles && puzzleEngine != null)
            {
                puzzleEngine.StepTick(SimulationWorld.FixedDeltaTime);
            }
        }

        private void Update()
        {
            SyncBodyControllers();

            // Update Spacetime Curvature Grid
            if (spacetimeGrid != null)
            {
                spacetimeGrid.UpdateGrid(_world.Bodies, gravitationalConstant, speedOfLight);
            }

            // Update Audio Synthesizer metric warp
            if (audioSynth != null && _world.Bodies.Count > 0)
            {
                double centerWarp = spacetimeGrid != null
                    ? spacetimeGrid.CalculateWarpAt(0.0, 0.0, _world.Bodies)
                    : -10.0;
                audioSynth.ActiveMetricWarp = centerWarp;
            }

            // Update Forward Trajectory Preview for selected body
            UpdateTrajectoryPreview();

            // Update live telemetry card
            UpdateTelemetry();
        }

        private void HandleBodyLaunched(CelestialBodyState newBody)
        {
            _world.AddBody(newBody);
            SyncBodyControllers();

            if (audioSynth != null)
            {
                audioSynth.TriggerLaunchSound();
            }
        }

        private void HandleModeChanged(WorkbenchMode mode)
        {
            SyncBodyControllers();

            if (mode == WorkbenchMode.OrbitalPuzzles && puzzleEngine != null)
            {
                puzzleEngine.LoadMission(0); // Load first mission
            }
        }

        private void HandleMissionCompleted(IPuzzleMission mission, int stars)
        {
            if (celebration != null)
            {
                celebration.TriggerCelebration();
            }
        }

        private void OnBodyCollisionMerged(CollisionEvent evt)
        {
            if (audioSynth != null)
            {
                audioSynth.TriggerSupernovaSound();
            }
        }

        private void OnRocheLimitDisrupted(RocheDisruptionEvent evt)
        {
            if (audioSynth != null)
            {
                audioSynth.TriggerLaunchSound();
            }
        }

        private void SyncBodyControllers()
        {
            var activeBodies = _world.Bodies;

            // Update existing or spawn new body controllers
            for (int i = 0; i < activeBodies.Count; i++)
            {
                var state = activeBodies[i];
                if (state == null) continue;

                if (state.IsDestroyed)
                {
                    if (_bodyControllers.TryGetValue(state.Id, out var deadController))
                    {
                        Destroy(deadController.gameObject);
                        _bodyControllers.Remove(state.Id);
                    }
                    continue;
                }

                if (!_bodyControllers.TryGetValue(state.Id, out var controller))
                {
                    GameObject go = celestialBodyPrefab != null
                        ? Instantiate(celestialBodyPrefab)
                        : GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    controller = go.GetComponent<CelestialBody>();
                    if (controller == null) controller = go.AddComponent<CelestialBody>();

                    controller.Initialize(state);
                    _bodyControllers[state.Id] = controller;
                }

                controller.SyncTransform();
                controller.UpdateThermodynamics(activeBodies);
            }
        }

        private void UpdateTrajectoryPreview()
        {
            if (trajectoryRenderer == null || _world.Bodies.Count == 0) return;

            int selectedId = workbench != null ? workbench.SelectedBodyId : -1;
            CelestialBodyState target = null;
            CelestialBodyState dominantAttractor = _world.Bodies[0];

            for (int i = 0; i < _world.Bodies.Count; i++)
            {
                if (_world.Bodies[i].Id == selectedId)
                {
                    target = _world.Bodies[i];
                    break;
                }
            }

            if (target == null && _world.Bodies.Count > 1)
            {
                target = _world.Bodies[1]; // Default to first planet/satellite
            }

            if (target != null)
            {
                trajectoryRenderer.UpdateTrajectory(target, _world.Bodies, gravitationalConstant, dominantAttractor);
            }
            else
            {
                trajectoryRenderer.Clear();
            }
        }

        private void UpdateTelemetry()
        {
            if (telemetryInspector == null || _world.Bodies.Count == 0) return;

            int selectedId = workbench != null ? workbench.SelectedBodyId : -1;
            CelestialBodyState selected = null;
            for (int i = 0; i < _world.Bodies.Count; i++)
            {
                if (_world.Bodies[i].Id == selectedId)
                {
                    selected = _world.Bodies[i];
                    break;
                }
            }

            if (selected == null && _world.Bodies.Count > 1) selected = _world.Bodies[1];
            CelestialBodyState attractor = _world.Bodies[0];

            telemetryInspector.DisplayTelemetry(selected, attractor, gravitationalConstant);
        }
    }
}
