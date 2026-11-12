# 🌌 CosmoForge — The Gravity & Cosmos Sandbox

[![Unity 6](https://img.shields.io/badge/Unity-6000.0.6f1-black?logo=unity)](https://unity.com/)
[![Universal Render Pipeline](https://img.shields.io/badge/URP-17.2.6-blue?logo=unity)](https://unity.com/)
[![React](https://img.shields.io/badge/React-18.3.1-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.5.3-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-5.4.2-646CFF?logo=vite)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4.4-38B2AC?logo=tailwind-css)](https://tailwindcss.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**CosmoForge** is an interactive, scientifically rigorous astrophysics sandbox and orbital mechanics laboratory. Built with dual implementations—a **Unity 6 URP 3D Engine** and a high-performance **React 18 + TypeScript Web Canvas Simulation**—CosmoForge simulates N-body gravitational dynamics with symplectic integrators, relativistic visuals, cataclysmic astrophysical events, procedural cosmic audio, and gamified orbital puzzles.

---

## 🌟 Key Features

### 1. 🪐 Precision N-Body Physics & Conservation Laws
- **Symplectic Velocity Verlet Integrator**: Delivers time-reversible, energy-conserving orbital integration with zero secular energy drift over thousands of orbits.
- **Double-Precision (`Vector3d`) Core**: 64-bit floating-point coordinates with floating-origin camera translation to eliminate floating-point jitter across planetary and interstellar distances.
- **Plummer Softening ($\epsilon$)**: Prevents non-physical infinite acceleration singularities during close gravitational encounters and hyperbolic slingshots.
- **Dynamic Orbital Element Solver**: Real-time analytical computation of osculating Keplerian orbital elements: semi-major axis ($a$), eccentricity ($e$), orbital period ($T$), periapsis ($r_p$), and apoapsis ($r_a$).
- **500-Step Ghost Trajectory Predictor**: Multi-body forward numerical projection rendering projected orbital paths, encounter markers, and escape trajectories in real time.

### 2. 🌀 Spacetime Curvature & Relativistic Shaders
- **GPU Spacetime Curvature Grid**: Dynamic vertex-displacement mesh visualizing gravitational potential wells, deformed proportional to mass and proximity according to General Relativity principles.
- **Schwarzschild Supermassive Black Holes**: Computes exact event horizon radii ($R_s = \frac{2GM}{c^2}$), gravitational lensing (Einstein ring distortions), and Doppler-beamed accretion disk radiation.
- **Pulsar Synchrotron Radiation**: High-speed spinning neutron stars with precessing magnetic dipoles and collimated relativistic synchrotron particle beams.
- **Atmospheric Scattering & Gas Giant Storms**: Rayleigh/Mie atmospheric scattering on terrestrial worlds and procedural animated zonal wind bands with cyclonic storm vortices on gas giants.

### 3. 💥 Cataclysms & Celestial Collisions
- **Inelastic Momentum-Conserving Mergers**: Accurate mass accumulation and vector momentum conservation:
  $$\vec{v}_{\text{merged}} = \frac{m_1\vec{v}_1 + m_2\vec{v}_2}{m_1 + m_2}$$
- **Fluid Roche Limit Tidal Disruption**: Dynamic evaluation of tidal stress:
  $$d_{\text{Roche}} \approx 2.44 R_M \left(\frac{\rho_M}{\rho_m}\right)^{1/3}$$
  Moons crossing within this threshold violently disintegrate into planar planetary ring particle systems.
- **Thermonuclear Supernovae**: Chandrasekhar mass limit ($1.44 M_\odot$) triggers Type Ia thermonuclear bursts and Type II core collapse explosions, leaving behind neutron star/black hole remnants.
- **Gravitational Waves & Coalescence**: Compact binary mergers trigger expanding spacetime ripples accompanied by relativistic chirp frequency sweeps.

### 4. 🚀 5 Gamified Orbital Puzzle Missions
Self-evaluating educational flight and mechanics challenges with 1-to-3 star ratings, live telemetry diagnostics, and victory confetti:
1. **The Orbital Injection**: Slingshot a scientific satellite from Earth into a stable circular orbit ($e < 0.1$) without surface collision or hyperbolic escape.
2. **Lagrange Parking**: Navigate and park a deep-space telescope into the gravitational equilibrium saddle points ($L_1$ or $L_4/L_5$) of a binary planetary system.
3. **Planetary Defense**: Deploy a gravity-tractor probe on an intercept trajectory to safely divert a hazardous asteroid away from an inhabited world.
4. **Barycentric Ballet**: Coordinate a stable circumbinary orbit for an exoplanet orbiting around a chaotic mutual binary star system.
5. **The Ring Smith**: Deliberately decelerate an icy moon past a gas giant's fluid Roche limit to forge a dense, stable planetary ring system.

### 5. 🎛️ Tactile Workbench, Telemetry & Time-Warp
- **Universal Slingshot Controls**: Drag-and-release slingshot vector targeting with instant trajectory preview.
- **Time Warp & Reverse Rewind**: Variable time progression (0.1x to 1000x speed) and a circular memory history buffer allowing smooth reverse-time rewinds.
- **Live Orbital Telemetry Inspector**: Displays real-time mass, velocity, kinetic energy, potential energy, specific orbital energy, and conic section classification.
- **Celestial Body Drawer**: Instantiate terrestrial planets, moons, gas giants, main-sequence stars, pulsars, or black holes on the fly.

### 6. 🔊 Procedural Cosmic Audio DSP
- Pure procedural sound synthesizer (Unity `OnAudioFilterRead` + Web Audio API).
- Sub-bass spacetime curvature gravitational drone.
- Pulsar radio click trains synchronized to rotation period.
- Kinetic slingshot release bursts and thermonuclear supernova shockwave sweeps.

---

## 📐 Mathematical Foundations

| Phenomenon | Formula / Implementation |
|---|---|
| **Gravitational Acceleration** | $\vec{a}_i = \sum_{j \neq i} \frac{G m_j (\vec{r}_j - \vec{r}_i)}{(|\vec{r}_j - \vec{r}_i|^2 + \epsilon^2)^{3/2}}$ |
| **Velocity Verlet Step** | $\vec{r}(t + \Delta t) = \vec{r}(t) + \vec{v}(t)\Delta t + \frac{1}{2}\vec{a}(t)\Delta t^2$<br>$\vec{v}(t + \Delta t) = \vec{v}(t) + \frac{1}{2}(\vec{a}(t) + \vec{a}(t + \Delta t))\Delta t$ |
| **Vis-Viva Equation** | $v^2 = G(M + m)\left(\frac{2}{r} - \frac{1}{a}\right)$ |
| **Orbital Eccentricity** | $e = \sqrt{1 + \frac{2\varepsilon h^2}{\mu^2}}$ |
| **Schwarzschild Radius** | $R_s = \frac{2GM}{c^2}$ |
| **Fluid Roche Limit** | $d_{\text{Roche}} \approx 2.44 R_M \left(\frac{\rho_M}{\rho_m}\right)^{1/3}$ |

---

## 📁 Repository Structure

```
CosmoForge/
├── Assets/                      # Unity 6 URP 3D Project
│   ├── Editor/                  # Custom Scene & Project Builders
│   ├── Scenes/                  # MainSandbox.unity
│   ├── Scripts/
│   │   ├── Core/                # Vector3d, KeplerMath, FloatingOrigin, SimulationWorld
│   │   ├── Physics/             # VelocityVerletIntegrator, TrajectoryPredictor, SpacetimeCurvatureGrid
│   │   ├── Celestial/           # CelestialBody, BlackHoleController, PulsarController, StellarLifecycle
│   │   ├── Puzzles/             # PuzzleEngine & 5 Orbital Puzzle Missions
│   │   ├── UI/                  # WorkbenchController, TelemetryInspector, TouchGestureHandler
│   │   └── Audio/               # ProceduralAudioSynthesizer (OnAudioFilterRead DSP)
│   ├── Shaders/                 # Spacetime Curvature, Lensing, Accretion Disk, Pulsar Beams
│   └── Tests/                   # Automated E2E & EditMode Test Suites (Tiers 1-4)
├── Packages/                    # Unity Package Manifest & Dependencies
├── ProjectSettings/             # Unity Project Configuration
├── src/                         # React 18 + TypeScript Web Simulation
│   ├── components/              # SpacetimeCanvas, CelestialDrawer, TelemetryInspector, PuzzleOverlay
│   ├── engine/                  # GravityEngine.ts, CosmicAudioSynth.ts
│   ├── App.tsx                  # Master React Application Container
│   ├── main.tsx                 # Web Entrypoint
│   └── types.ts                 # TypeScript Data Models
├── index.html                   # Web Application Shell
├── vite.config.ts               # Vite Build Configuration
├── tailwind.config.js           # Tailwind CSS Styling Configuration
├── tsconfig.json                # TypeScript Compiler Configuration
├── PROJECT.md                   # Full Architectural Specification & Feature Inventory
├── TEST_INFRA.md                # E2E Test Suite Framework Documentation
└── README.md                    # Project Documentation
```

---

## 🚀 Getting Started

### 🌐 Running the Web Simulation

Prerequisites: **Node.js** (v18 or later) & **npm**.

1. **Clone the repository**:
   ```bash
   git clone https://github.com/ssilu07/cosmoforge.git
   cd cosmoforge
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Start the development server**:
   ```bash
   npm run dev
   ```
   Open your browser at `http://localhost:5173`.

4. **Build for production**:
   ```bash
   npm run build
   ```

---

### 🎮 Running the Unity 3D Engine

Prerequisites: **Unity 6 (6000.0.6f1 or later)** with **Universal Render Pipeline (URP)** and **Android Build Support** (optional for mobile deployment).

1. Launch **Unity Hub**.
2. Click **Open** -> **Add project from disk**.
3. Select the `CosmoForge` root folder.
4. Open the main scene located at `Assets/Scenes/MainSandbox.unity`.
5. Press **Play** in the Unity Editor to interact with the full 3D simulation.

---

## 🎮 Controls & Interaction Guide

| Control | Action |
|---|---|
| **Left Click / Tap & Drag Canvas** | Pan camera across spacetime coordinates |
| **Scroll Wheel / Pinch Zoom** | Exponential log-scale zoom in and out |
| **Click Celestial Body** | Select body to inspect live orbital telemetry |
| **Slingshot Mode (Drag from point)** | Pull back and release to launch body with custom velocity vector |
| **Time Warp Slider / Buttons** | Accelerate time (0.1x – 1000x) or Pause |
| **Rewind Button** | Step back through circular history buffer to undo cataclysms |
| **Presets (Top Header)** | Switch between Free Sandbox, Solar System Preset, and Puzzles |
| **Mute Toggle** | Enable / disable procedural cosmic audio synthesizer |

---

## 🧪 Testing & Quality Assurance

CosmoForge features a multi-tiered automated test suite covering physics invariants, orbital mathematics, cataclysms, and UI behaviors:
- **Tier 1 (Feature Coverage)**: Gravity integration, Kepler osculating elements, ghost trajectories, body states.
- **Tier 2 (Boundary & Corner Cases)**: Extreme mass ratios ($10^6:1$), zero distance softening, near-c velocities.
- **Tier 3 (Cross-Feature Pairwise)**: Collisions during time warp, Roche disruptions under relativistic curvature.
- **Tier 4 (Real-World Scenarios)**: Earth-Moon-Sun 3-body stability, Lagrange point station-keeping, Hohmann transfers.

---

## 📜 License

This project is licensed under the [MIT License](LICENSE). Contributions, feedback, and astrophysics discussions are warmly welcome!
