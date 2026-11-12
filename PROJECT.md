# Project: CosmoForge — The Gravity & Cosmos Sandbox

## Architecture Overview
CosmoForge is built on Unity 6 (6000.6.8f1) utilizing the Universal Render Pipeline (URP 17.2.6), double-precision vector mathematics (`double3` / `Vector3d`), a Symplectic Velocity Verlet gravitational integrator with Plummer softening, analytical and numerical Keplerian trajectory prediction, GPU vertex-displacement spacetime curvature visualization, relativistic shaders, cataclysmic inelastic collisions, tidal Roche disruptions, 5 self-evaluating orbital puzzles, a mobile tactile workbench UI with time warp and reverse time, and pure procedural cosmic audio DSP.

### Subsystem Boundaries
1. **Core Math & Physics Integrator**: Pure C# double-precision symplectic N-body gravity engine, conserved quantities ($E, \vec{P}, \vec{L}$), Plummer softening factor $\epsilon$.
2. **Trajectory & Spacetime Visualization**: Analytical two-body conic section solver + 500-step numerical ghost trajectory predictor + URP GPU vertex-displacement spacetime curvature grid.
3. **Celestial Body Catalog & Relativistic Shaders**: Terrestrial planets, moons, gas giants, main sequence stars, pulsars, and supermassive black holes. Custom shaders for Schwarzschild event horizon, Einstein ring lensing, Doppler-beamed accretion disk, and synchrotron beams.
4. **Cataclysm & Collision Engine**: Inelastic mergers with linear momentum conservation, Roche limit tidal disruption into particle rings, Chandrasekhar limit thermonuclear supernovae, and compact binary merger gravitational wave ripples.
5. **Puzzle Engine & Gamification**: 5 self-evaluating orbital mechanics puzzle missions with 1-to-3 star evaluations, diagnostic feedback, and confetti victory celebrations.
6. **Mobile Workbench UI, Audio & Controls**: Android touch gestures (pan, pinch zoom, slingshot drag), time warp (0.1x to 1000x) with reverse time rewind ring buffer, live orbital telemetry inspector, and procedural cosmic audio DSP (`OnAudioFilterRead`).
7. **E2E Testing & Quality Assurance**: Comprehensive multi-tiered automated test suite validating physics accuracy, conservation laws, shader rendering, puzzle win conditions, and UI responsiveness.

---

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F01 | Project Scaffold & Android 60 FPS Setup | Unity 6 URP Android project structure, Package manifest, performance budgets | M1 | Survey E1 |
| F02 | Symplectic Velocity Verlet Integrator | Plummer softening factor $\epsilon$, mutual gravity force calculation, zero secular energy drift | M1 | Survey E1 |
| F03 | Double-Precision Core Physics Math | `Vector3d` / `double3` math structs, floating-origin camera translation system | M1 | Survey E1 |
| F04 | Analytical Keplerian Conic Solver | Osculating conic elements ($e, a, r_p, r_a, T$), ellipse/parabola/hyperbola classification | M2 | Survey E1 |
| F05 | 500-Step Numerical Ghost Trajectory Predictor | Multi-body forward projection, dynamic periapsis & apoapsis markers, collision/Roche warnings | M2 | Survey E1 |
| F06 | Spacetime Curvature Grid Visualizer | Procedural mesh deformation, GPU vertex displacement shader for local mass depression | M2 | Survey E1 |
| F07 | Terrestrial Planets & Moons | Procedural surface textures, atmospheric scattering rim, blackbody temperature $T=(L_*/(16\pi\sigma d^2))^{1/4}$ | M3 | Survey E2 |
| F08 | Gas Giants & Atmospheric Vortices | Procedural animated zonal wind bands, cyclonic storm vortices shader | M3 | Survey E2 |
| F09 | Stellar Fusion Lifecycle Engine | Core hydrogen fuel depletion, mass-luminosity relation, Red Dwarf, Sun-like, Blue Supergiant | M3 | Survey E2 |
| F10 | Pulsar Synchrotron Beams | High-speed rotation, precessing magnetic dipole, collimated synchrotron radiation beam shader & radio pulses | M3 | Survey E2 |
| F11 | Supermassive Black Hole Visuals | Schwarzschild radius $R_s=2GM/c^2$, Einstein ring lensing shader, Doppler-beamed accretion disk | M3 | Survey E2 |
| F12 | Inelastic Collision & Merger Engine | Linear momentum conservation $\vec{v}_{\text{merged}}=(m_1\vec{v}_1+m_2\vec{v}_2)/(m_1+m_2)$, mass accumulation | M4 | Survey E2 |
| F13 | Collision Cataclysm Visual Effects | White flash VFX, expanding thermal shockwave rings, molten orbital debris particle system | M4 | Survey E2 |
| F14 | Fluid Roche Limit Tidal Disruption | Dynamic $d_{\text{Roche}}=2.44 R_M (\rho_M/\rho_m)^{1/3}$, moon shattering into planar planetary ring particle system | M4 | Survey E2 |
| F15 | Stellar Supernova Explosions | Chandrasekhar limit $1.44 M_\odot$ Type Ia trigger and Type II core collapse with remnant generation | M4 | Survey E2 |
| F16 | Compact Binary Mergers & Gravitational Waves | Expanding spacetime ripple visualizer and audio frequency sweep upon compact binary coalescences | M4 | Survey E2 |
| F17 | Puzzle Mission 1: "The Orbital Injection" | Slingshot probe into stable circular orbit around Earth without crashing or escaping | M5 | Survey E3 |
| F18 | Puzzle Mission 2: "Lagrange Parking" | Position space telescope into $L_1$ or $L_4/L_5$ equilibrium points of Earth-Moon system | M5 | Survey E3 |
| F19 | Puzzle Mission 3: "Planetary Defense" | Deploy gravity-tractor probe to deflect collision-course asteroid into safe flyby | M5 | Survey E3 |
| F20 | Puzzle Mission 4: "Barycentric Ballet" | Place two massive stars in mutual orbit and find stable circumbinary orbit for inhabited planet | M5 | Survey E3 |
| F21 | Puzzle Mission 5: "The Ring Smith" | De-orbit icy moon across gas giant's Roche limit to create ring system with $\ge 80\%$ particle retention | M5 | Survey E3 |
| F22 | Gamified Puzzle Engine & Confetti Celebration | Objective win/loss evaluation, 1-3 star ratings, educational diagnostics, victory confetti | M5 | Survey E3 |
| F23 | Workbench UI Mode Switcher | Mode toggle: Free Sandbox, Solar System Preset, Orbital Puzzles | M6 | Survey E3 |
| F24 | Time-Warp Controls & Reverse Time Buffer | Simulation time-warp (0.1x to 1000x, pause) + deterministic reverse time rewind ring buffer | M6 | Survey E3 |
| F25 | Celestial Body Placement Drawer & Slingshot | Body catalog drawer, drag-to-slingshot touch launch vector with real-time trajectory preview | M6 | Survey E3 |
| F26 | Live Orbital Telemetry Inspector | Real-time telemetry inspector: mass, radius, velocity, period, semi-major axis, eccentricity, temperature | M6 | Survey E3 |
| F27 | Android Tactile Touch Controls & 3D Icons | Multi-touch gestures (pan, log-scale pinch zoom, selection, tracking), tactile 3D UI icons | M6 | Survey E3 |
| F28 | Procedural Cosmic Audio Synthesizer | Pure C# DSP `OnAudioFilterRead` synthesizer (spacetime hums, pulsar clicks, launch bursts, supernova blasts) | M6 | Survey E3 |
| F29 | E2E Automated Test Suite & Coverage Hardening | Headless test runner, Tiers 1-4 test verification, Tier 5 adversarial stress testing | M7 / Test Track | Survey E1, E3 |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Project Setup & Symplectic Gravity Engine | F01, F02, F03: Unity 6 URP project scaffold, `Vector3d` math, Velocity Verlet integrator with Plummer softening, floating origin | None | DONE |
| M2 | Keplerian Trajectory Predictor & Spacetime Grid | F04, F05, F06: Analytical osculating solver, 500-step numerical ghost trajectory predictor, GPU spacetime curvature grid shader | M1 | DONE |
| M3 | Celestial Body Catalog & Relativistic Shaders | F07, F08, F09, F10, F11: Terrestrial planets, gas giants, stellar lifecycle, pulsars, supermassive black holes with lensing & accretion disk | M1 | DONE |
| M4 | Cataclysmic Phenomena & Inelastic Collision Engine | F12, F13, F14, F15, F16: Momentum conservation mergers, molten debris VFX, Roche limit ring disruption, supernovae, binary mergers | M1, M2, M3 | DONE |
| M5 | Gamified Orbital Mechanics Puzzle Missions | F17, F18, F19, F20, F21, F22: 5 puzzle missions, objective win/loss state evaluator, star ratings, confetti celebrations | M1, M2, M4 | DONE |
| M6 | Android Tactile Workbench UI, Controls & Audio | F23, F24, F25, F26, F27, F28: Mode switcher, time warp & rewind buffer, slingshot placement drawer, telemetry inspector, touch gestures, procedural audio DSP | M1, M2, M3, M4, M5 | DONE |
| M7 | Final Milestone: E2E Test Pass & Coverage Hardening | F29: Pass 100% of E2E Test Suite (Tiers 1-4) published by E2E Testing Track, followed by Tier 5 adversarial coverage hardening | M1-M6, E2E Test Suite | DONE |

---

## E2E Testing Track
Running in parallel with the Implementation Track:
- **E2E Testing Orchestrator**: Designs requirement-driven, opaque-box test runner and test cases across Tiers 1-4 covering all 29 features.
- Publishes `TEST_INFRA.md` and `TEST_READY.md`.

---

## Interface Contracts

### 1. Core Physics Math (`Vector3d`)
```csharp
public struct Vector3d {
    public double x, y, z;
    public Vector3d(double x, double y, double z);
    public static Vector3d operator +(Vector3d a, Vector3d b);
    public static Vector3d operator -(Vector3d a, Vector3d b);
    public static Vector3d operator *(Vector3d a, double d);
    public static Vector3d operator /(Vector3d a, double d);
    public double sqrMagnitude => x * x + y * y + z * z;
    public double magnitude => System.Math.Sqrt(sqrMagnitude);
    public Vector3d normalized => magnitude > 1e-15 ? this / magnitude : Vector3d.zero;
    public static double Dot(Vector3d a, Vector3d b);
    public static Vector3d Cross(Vector3d a, Vector3d b);
    public UnityEngine.Vector3 ToVector3();
}
```

### 2. Celestial Body State (`CelestialBodyState`)
```csharp
public class CelestialBodyState {
    public int Id;
    public string Name;
    public BodyType Type; // Terrestrial, Moon, GasGiant, Star, Pulsar, BlackHole
    public double Mass;
    public double Radius;
    public Vector3d Position;
    public Vector3d Velocity;
    public Vector3d Acceleration;
    public double SurfaceTemperature;
    public double CoreFuel; // 0.0 - 1.0 for stars
    public bool IsDestroyed;
}
```

### 3. Gravity Integrator Contract (`IGravityIntegrator`)
```csharp
public interface IGravityIntegrator {
    double GravitationalConstant { get; set; }
    double SofteningFactor { get; set; } // Plummer epsilon
    void Step(List<CelestialBodyState> bodies, double deltaTime);
    double CalculateTotalKineticEnergy(List<CelestialBodyState> bodies);
    double CalculateTotalPotentialEnergy(List<CelestialBodyState> bodies);
    Vector3d CalculateTotalLinearMomentum(List<CelestialBodyState> bodies);
    Vector3d CalculateTotalAngularMomentum(List<CelestialBodyState> bodies);
}
```

### 4. Trajectory Predictor Contract (`ITrajectoryPredictor`)
```csharp
public struct ConicOrbitElements {
    public double SemiMajorAxis;
    public double Eccentricity;
    public double PeriapsisDistance;
    public double ApoapsisDistance;
    public double OrbitalPeriod;
    public double SpecificEnergy;
    public ConicType Type; // Circular, Elliptical, Parabolic, Hyperbolic
}

public interface ITrajectoryPredictor {
    ConicOrbitElements CalculateOsculatingElements(CelestialBodyState body, CelestialBodyState dominantAttractor, double G);
    Vector3d[] PredictForwardGhostTrajectory(CelestialBodyState target, List<CelestialBodyState> allBodies, int maxSteps, double stepDeltaTime, double G, double softening);
}
```

### 5. Cataclysm & Collision Engine Contract (`ICollisionEngine`)
```csharp
public interface ICollisionEngine {
    void CheckAndResolveCollisions(List<CelestialBodyState> bodies, double deltaTime, System.Action<CollisionEvent> onMerged);
    void EvaluateRocheLimits(List<CelestialBodyState> bodies, System.Action<RocheDisruptionEvent> onDisrupted);
}
```

### 6. Puzzle Mission Contract (`IPuzzleMission`)
```csharp
public interface IPuzzleMission {
    string MissionId { get; }
    string DisplayTitle { get; }
    string Description { get; }
    void Initialize(SimulationWorld world);
    PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime);
    void ResetMission(SimulationWorld world);
}
```

---

## Code Layout
```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Math/ (Vector3d.cs, KeplerMath.cs, ConicOrbitElements.cs)
│   │   └── Performance/ (FloatingOrigin.cs, SimulationWorld.cs)
│   ├── Physics/
│   │   ├── Integrator/ (VelocityVerletIntegrator.cs, IGravityIntegrator.cs)
│   │   ├── Trajectory/ (KeplerianPredictor.cs, ITrajectoryPredictor.cs, TrajectoryRenderer.cs)
│   │   ├── Grid/ (SpacetimeCurvatureGrid.cs, ISpacetimeGridVisualizer.cs)
│   │   └── Cataclysms/ (CollisionEngine.cs, RocheDisruptor.cs, SupernovaEngine.cs)
│   ├── Celestial/
│   │   ├── Data/ (CelestialBodyData.cs, BodyType.cs, StellarLifecycle.cs)
│   │   └── Controllers/ (CelestialBody.cs, BlackHoleController.cs, PulsarController.cs)
│   ├── Puzzles/
│   │   ├── Core/ (PuzzleEngine.cs, IPuzzleMission.cs, PuzzleEvaluationResult.cs)
│   │   └── Missions/ (OrbitalInjectionMission.cs, LagrangeParkingMission.cs, PlanetaryDefenseMission.cs, BarycentricBalletMission.cs, RingSmithMission.cs)
│   ├── UI/
│   │   ├── Workbench/ (WorkbenchController.cs, ModeSwitcher.cs, TimeWarpController.cs, BodyPlacementDrawer.cs)
│   │   ├── Telemetry/ (TelemetryInspector.cs, OrbitInfoCard.cs)
│   │   ├── Feedback/ (ConfettiCelebration.cs, VictoryModal.cs)
│   │   └── Input/ (TouchGestureHandler.cs, SlingshotController.cs)
│   └── Audio/
│       └── ProceduralAudioSynthesizer.cs
├── Shaders/
│   ├── SpacetimeCurvatureGrid.shader
│   ├── GravitationalLensing.shader
│   ├── AccretionDisk.shader
│   ├── AtmosphericScattering.shader
│   ├── GasGiantBands.shader
│   └── PulsarSynchrotronBeam.shader
├── Materials/
├── Prefabs/
├── Scenes/
│   └── MainSandbox.unity
└── Tests/
    ├── EditMode/ (PhysicsMathTests.cs, ConicTests.cs, IntegratorTests.cs, PuzzleLogicTests.cs)
    └── PlayMode/ (E2EPhysicsTests.cs, CollisionE2ETests.cs, PuzzleE2ETests.cs)
```
