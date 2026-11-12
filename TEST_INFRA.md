# E2E Test Infra: CosmoForge

## Test Philosophy
- **Opaque-Box & Requirement-Driven**: Tests are derived strictly from user requirements in `ORIGINAL_REQUEST.md` and public interface contracts in `PROJECT.md`. Tests do not inspect internal private fields or mirror implementation milestones.
- **Methodology**: Systematic 4-tier testing hierarchy combining:
  1. *Category-Partition Testing* (Tier 1: Feature Isolation Happy Paths)
  2. *Boundary Value Analysis & Extreme Inputs* (Tier 2: Boundary & Corner Cases)
  3. *Pairwise Combinatorial Testing* (Tier 3: Cross-Feature Interactions)
  4. *Real-World Application Workloads* (Tier 4: End-to-End Mission & System Scenarios)
- **Progressive Testability**: Verification mechanisms do not depend on features more complex than what is being tested.
- **Robustness**: Rejection of invalid inputs produces well-defined graceful handling rather than unhandled fatal exceptions.

---

## Feature Inventory & Test Tier Mapping (F01 - F28)

| # | Feature Key | Requirement Source | Tier 1 (Happy) | Tier 2 (Boundary) | Tier 3 (Pairwise) | Tier 4 (Scenario) |
|---|-------------|-------------------|:--------------:|:-----------------:|:-----------------:|:-----------------:|
| 1 | F01_Scaffold_Android | ORIGINAL_REQUEST §R1 | 5 | 5 | ✓ | ✓ |
| 2 | F02_Velocity_Verlet | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |
| 3 | F03_Double_Precision_Math | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |
| 4 | F04_Keplerian_Conic_Solver | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |
| 5 | F05_Ghost_Trajectory_Predictor | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |
| 6 | F06_Spacetime_Curvature_Grid | ORIGINAL_REQUEST §R2 | 5 | 5 | ✓ | ✓ |
| 7 | F07_Terrestrial_Planets_Moons | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 8 | F08_Gas_Giants_Atmosphere | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 9 | F09_Stellar_Fusion_Lifecycle | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 10 | F10_Pulsar_Synchrotron | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 11 | F11_Supermassive_Black_Holes | ORIGINAL_REQUEST §R3 | 5 | 5 | ✓ | ✓ |
| 12 | F12_Inelastic_Collisions | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 13 | F13_Collision_VFX_Molten_Debris | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 14 | F14_Fluid_Roche_Tidal_Disruption | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 15 | F15_Stellar_Supernovae | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 16 | F16_Compact_Binary_Mergers | ORIGINAL_REQUEST §R4 | 5 | 5 | ✓ | ✓ |
| 17 | F17_Puzzle_Orbital_Injection | ORIGINAL_REQUEST §R5.1 | 5 | 5 | ✓ | ✓ |
| 18 | F18_Puzzle_Lagrange_Parking | ORIGINAL_REQUEST §R5.2 | 5 | 5 | ✓ | ✓ |
| 19 | F19_Puzzle_Planetary_Defense | ORIGINAL_REQUEST §R5.3 | 5 | 5 | ✓ | ✓ |
| 20 | F20_Puzzle_Barycentric_Ballet | ORIGINAL_REQUEST §R5.4 | 5 | 5 | ✓ | ✓ |
| 21 | F21_Puzzle_Ring_Smith | ORIGINAL_REQUEST §R5.5 | 5 | 5 | ✓ | ✓ |
| 22 | F22_Puzzle_Engine_Celebration | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |
| 23 | F23_Workbench_Mode_Switcher | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |
| 24 | F24_TimeWarp_Rewind_Buffer | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |
| 25 | F25_Placement_Drawer_Slingshot | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |
| 26 | F26_Telemetry_Inspector | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |
| 27 | F27_Android_Touch_Controls | ORIGINAL_REQUEST §R1, §R5 | 5 | 5 | ✓ | ✓ |
| 28 | F28_Procedural_Audio_DSP | ORIGINAL_REQUEST §R5 | 5 | 5 | ✓ | ✓ |

---

## Test Architecture

### 1. Test Directory Layout
```
Assets/
└── Tests/
    ├── E2E/
    │   ├── Harness/
    │   │   ├── CosmoForgeTestHarness.cs
    │   │   ├── TestAssertions.cs
    │   │   └── HeadlessTestRunner.cs
    │   ├── Tier1_FeatureCoverage/
    │   │   ├── Tier1_PhysicsMathTests.cs          (F01-F03: 15 tests)
    │   │   ├── Tier1_TrajectoryGridTests.cs       (F04-F06: 15 tests)
    │   │   ├── Tier1_CelestialBodiesTests.cs      (F07-F11: 25 tests)
    │   │   ├── Tier1_CataclysmsCollisionsTests.cs (F12-F16: 25 tests)
    │   │   ├── Tier1_PuzzleMissionsTests.cs       (F17-F22: 30 tests)
    │   │   └── Tier1_WorkbenchControlsTests.cs    (F23-F28: 30 tests)
    │   ├── Tier2_BoundaryCorner/
    │   │   ├── Tier2_PhysicsMathBoundaryTests.cs          (F01-F03: 15 tests)
    │   │   ├── Tier2_TrajectoryGridBoundaryTests.cs       (F04-F06: 15 tests)
    │   │   ├── Tier2_CelestialBodiesBoundaryTests.cs      (F07-F11: 25 tests)
    │   │   ├── Tier2_CataclysmsBoundaryTests.cs           (F12-F16: 25 tests)
    │   │   ├── Tier2_PuzzleMissionsBoundaryTests.cs       (F17-F22: 30 tests)
    │   │   └── Tier2_WorkbenchControlsBoundaryTests.cs    (F23-F28: 30 tests)
    │   ├── Tier3_CrossFeaturePairwise/
    │   │   ├── Tier3_PairwiseInteractionTests.cs         (28 pairwise interaction tests)
    │   └── Tier4_RealWorldScenarios/
    │       ├── Tier4_RealWorldScenariosTests.cs          (14 multi-system application scenarios)
    │
```

### 2. Standalone & Headless Test Runner Architecture
Tests are implemented in pure, robust C# using NUnit/UnityTest attributes, but can also run headlessly via:
1. **Unity BatchMode Test Runner**:
   ```cmd
   "C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -nographics -runTests -projectPath "d:\apps_and_games\EDUCATION APPS\CosmoForge" -testPlatform EditMode -testResults "TestResults.xml"
   ```
2. **Headless CLI Harness**:
   `HeadlessTestRunner.cs` provides a self-contained execution loop that can execute all test suites directly from Unity editor menus or standalone batch scripts (`-executeMethod CosmoForge.Tests.HeadlessTestRunner.RunAllTestsHeadless`), outputting detailed console logs and writing `TestResults.json` / `TestResults.xml`.

---

## Real-World Application Scenarios (Tier 4)

| # | Scenario Title | Features Exercised | Description |
|---|----------------|-------------------|-------------|
| S01 | Full Solar System Stability Run | F01, F02, F03, F04, F23, F24 | Initialize Sun and 8 planets; simulate 10 orbits at 1000x time-warp; verify angular momentum & energy conservation. |
| S02 | Mission 1 Orbital Injection Lifecycle | F02, F04, F05, F17, F22, F25 | Slingshot probe into Earth orbit; verify 500-step ghost trajectory match with actual flight path and victory trigger. |
| S03 | Lagrange Point L1 & L4/L5 Stability | F02, F03, F18, F22, F26 | Position James Webb-type telescope at Earth-Moon L1 and L4/L5; run 10 lunar months; verify station-keeping. |
| S04 | Asteroid Gravity Tractor Deflection | F02, F05, F12, F19, F22, F26 | Rendezvous tractor probe with Earth-crossing asteroid; verify gentle gravitational pull alters trajectory to miss Earth. |
| S05 | Circumbinary Planetary Orbit | F02, F04, F20, F22 | Binary star barycenter orbit; place terrestrial planet in P-type orbit; verify long-term stability (>5 binary orbits). |
| S06 | Lunar Roche Disruption & Ring Formation | F02, F08, F14, F21, F22 | Moon crosses fluid Roche limit of Gas Giant; shatters into ring particle system with >= 80% particle retention. |
| S07 | Stellar Collapse to Supernova & Pulsar | F02, F09, F10, F15, F28 | Star exhausts hydrogen fuel; undergoes core-collapse supernova; emits blast VFX/audio; leaves high-spin pulsar remnant. |
| S08 | Black Hole Gravitational Lensing & Infall | F02, F03, F11, F12, F26 | Test particle spirals toward event horizon $R_s$; verify Schwarzschild radius threshold and Doppler-beamed accretion disk properties. |
| S09 | Binary Black Hole Merger & Gravitational Waves | F02, F11, F12, F16, F28 | Binary black holes spiral in, merge, emit gravitational wave ripples, and trigger procedural audio frequency sweep. |
| S10 | Time-Warp Reversibility & Conservation | F02, F03, F24, F26 | Simulate complex 4-body system for 1000 steps; trigger reverse-time rewind; verify bodies return to exact initial states. |
| S11 | Slingshot Interactive Placement Workflow | F04, F05, F23, F25, F27 | Touch drag creates velocity vector; forward trajectory updates dynamically; releasing spawns body into predicted path. |
| S12 | Telemetry Inspector Dynamic Accuracy | F02, F04, F07, F26 | High eccentricity orbit; verify telemetry inspector updates $a, e, r_p, r_a, v, T$ dynamically with zero NaN/divergence. |
| S13 | Android Multi-Touch Pan-Zoom & Audio Sync | F01, F27, F28 | Simulate simultaneous multi-touch pan, pinch-zoom, and body selection while procedural audio responds to speed/events. |
| S14 | Catastrophic Inelastic Planetary Collision | F02, F12, F13, F26, F28 | Two rocky planets collide head-on; verify momentum conservation vector, thermal shockwave VFX, debris creation, and audio blast. |

---

## Minimum Coverage Thresholds

| Test Tier | Formula | Minimum Count | Verification Focus |
|-----------|---------|:-------------:|-------------------|
| **Tier 1: Feature Coverage** | $5 \times 28$ features | **140** | Happy-path isolation tests for every feature |
| **Tier 2: Boundary & Corner Cases** | $5 \times 28$ features | **140** | Extreme values, zeroes, infinity, overflow, limits, invalid inputs |
| **Tier 3: Cross-Feature Pairwise** | $1 \times 28$ feature pairs | **28** | Interactions between coupled subsystems |
| **Tier 4: Real-World Scenarios** | $\max(5, 28/2)$ scenarios | **14** | Complete end-to-end sandbox & puzzle workloads |
| **TOTAL MINIMUM TEST CASES** | | **322** | **Full 100% test pass required for M7** |
