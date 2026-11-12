// ============================================================================
// CosmoForge Web: Master Application Component
// ============================================================================

import React, { useState, useEffect, useRef, useMemo } from 'react';
import { CelestialBodyData, WorkbenchMode, BodyType, Vector2D } from './types';
import { GravityEngine } from './engine/GravityEngine';
import { CosmicAudioSynth } from './engine/CosmicAudioSynth';
import { SpacetimeCanvas } from './components/SpacetimeCanvas';
import { TopHeader } from './components/TopHeader';
import { CelestialDrawer } from './components/CelestialDrawer';
import { TelemetryInspector } from './components/TelemetryInspector';
import { PuzzleOverlay, PUZZLE_MISSIONS } from './components/PuzzleOverlay';

export const App: React.FC = () => {
  const engine = useMemo(() => new GravityEngine(), []);
  const audio = useMemo(() => new CosmicAudioSynth(), []);

  // Application State
  const [mode, setMode] = useState<WorkbenchMode>('solar_system');
  const [bodies, setBodies] = useState<CelestialBodyData[]>([]);
  const [selectedBodyId, setSelectedBodyId] = useState<string | null>(null);
  const [selectedPreset, setSelectedPreset] = useState<BodyType>('terrestrial');
  const [isSlingshotActive, setIsSlingshotActive] = useState<boolean>(false);
  const [isPaused, setIsPaused] = useState<boolean>(false);
  const [timeScale, setTimeScale] = useState<number>(1.0);
  const [showTrajectories, setShowTrajectories] = useState<boolean>(true);
  const [showSpacetimeGrid, setShowSpacetimeGrid] = useState<boolean>(true);
  const [showVelocityVectors, setShowVelocityVectors] = useState<boolean>(false);
  const [isMuted, setIsMuted] = useState<boolean>(false);

  // Puzzle State
  const [puzzleIndex, setPuzzleIndex] = useState<number>(0);
  const [puzzleElapsed, setPuzzleElapsed] = useState<number>(0);
  const [puzzleResult, setPuzzleResult] = useState<{
    status: 'in_progress' | 'victory' | 'defeat';
    stars: number;
    progress: number;
    feedback: string;
  }>({
    status: 'in_progress',
    stars: 0,
    progress: 0,
    feedback: 'Select or launch bodies to begin.',
  });

  // Circular history buffer for reverse time rewind
  const historyBufferRef = useRef<CelestialBodyData[][]>([]);

  // Initialize Standard Solar System
  const initSolarSystem = () => {
    const G = 1.0;
    const mSun = 10000.0;

    const createPlanet = (
      id: string,
      name: string,
      type: BodyType,
      mass: number,
      radius: number,
      dist: number,
      color: string,
      temp: number
    ): CelestialBodyData => {
      const vCirc = Math.sqrt((G * mSun) / dist);
      return {
        id,
        name,
        type,
        mass,
        radius,
        position: { x: dist, y: 0 },
        velocity: { x: 0, y: vCirc },
        color,
        trail: [],
        temperature: temp,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.04,
        isDestroyed: false,
      };
    };

    const initialBodies: CelestialBodyData[] = [
      // Sun
      {
        id: 'sun',
        name: 'Sun',
        type: 'star',
        mass: mSun,
        radius: 8.0,
        position: { x: 0, y: 0 },
        velocity: { x: 0, y: 0 },
        color: '#f59e0b',
        trail: [],
        temperature: 5778,
        coreFuel: 1.0,
        spinAngle: 0,
        spinSpeed: 0.01,
        isDestroyed: false,
      },
      createPlanet('mercury', 'Mercury', 'terrestrial', 0.055, 0.8, 30.0, '#94a3b8', 440),
      createPlanet('venus', 'Venus', 'terrestrial', 0.815, 1.2, 50.0, '#e2e8f0', 737),
      createPlanet('earth', 'Earth', 'terrestrial', 1.0, 1.4, 75.0, '#38bdf8', 288),
      createPlanet('mars', 'Mars', 'terrestrial', 0.107, 1.0, 105.0, '#ef4444', 210),
      createPlanet('jupiter', 'Jupiter', 'gas_giant', 318.0, 4.0, 160.0, '#fbbf24', 165),
      createPlanet('saturn', 'Saturn', 'gas_giant', 95.0, 3.2, 230.0, '#fde047', 134),
    ];

    // Add Moon to Earth
    const earth = initialBodies[3];
    const rMoon = 3.2;
    const vMoon = Math.sqrt((G * earth.mass) / rMoon);
    initialBodies.push({
      id: 'moon',
      name: 'Moon',
      type: 'moon',
      mass: 0.0123,
      radius: 0.45,
      position: { x: earth.position.x + rMoon, y: 0 },
      velocity: { x: 0, y: earth.velocity.y + vMoon },
      color: '#cbd5e1',
      trail: [],
      temperature: 250,
      coreFuel: 0,
      spinAngle: 0,
      spinSpeed: 0.02,
      isDestroyed: false,
    });

    // Add Saturn Rings
    const saturn = initialBodies[6];
    saturn.rings = {
      innerRadius: saturn.radius * 1.4,
      outerRadius: saturn.radius * 2.8,
      color: '#fef08a',
    };

    setBodies(initialBodies);
    setSelectedBodyId('earth');
  };

  // Initialize on mount
  useEffect(() => {
    initSolarSystem();
    audio.init();
  }, []);

  // Handle Mode Change
  const handleModeChange = (newMode: WorkbenchMode) => {
    setMode(newMode);
    setSelectedBodyId(null);
    historyBufferRef.current = [];

    if (newMode === 'solar_system') {
      initSolarSystem();
      setIsPaused(false);
      setTimeScale(1.0);
    } else if (newMode === 'sandbox') {
      setBodies([]);
      setIsPaused(false);
      setTimeScale(1.0);
    } else if (newMode === 'puzzles') {
      setPuzzleIndex(0);
      setPuzzleElapsed(0);
      setBodies(PUZZLE_MISSIONS[0].setup());
      setIsPaused(false);
      setTimeScale(1.0);
    }
  };

  // Slingshot Launch Handler
  const handleSlingshotLaunch = (pos: Vector2D, vel: Vector2D) => {
    let mass = 1.0;
    let radius = 1.0;
    let color = '#38bdf8';
    let temp = 288;
    let fuel = 0;

    switch (selectedPreset) {
      case 'asteroid':
        mass = 0.001;
        radius = 0.4;
        color = '#94a3b8';
        temp = 180;
        break;
      case 'moon':
        mass = 0.012;
        radius = 0.55;
        color = '#cbd5e1';
        temp = 220;
        break;
      case 'terrestrial':
        mass = 1.0;
        radius = 1.2;
        color = '#38bdf8';
        temp = 288;
        break;
      case 'gas_giant':
        mass = 318.0;
        radius = 3.6;
        color = '#fbbf24';
        temp = 160;
        break;
      case 'star':
        mass = 1000.0;
        radius = 6.0;
        color = '#f59e0b';
        temp = 5778;
        fuel = 1.0;
        break;
      case 'hypergiant':
        mass = 25000.0;
        radius = 10.0;
        color = '#3b82f6';
        temp = 28000;
        fuel = 1.0;
        break;
      case 'pulsar':
        mass = 1800.0;
        radius = 1.2;
        color = '#00f0ff';
        temp = 1e6;
        break;
      case 'black_hole':
        mass = 50000.0;
        radius = 3.0;
        color = '#8b5cf6';
        temp = 10;
        break;
    }

    const id = `body_${Date.now()}`;
    const newBody: CelestialBodyData = {
      id,
      name: `${selectedPreset.replace('_', ' ').toUpperCase()}_${bodies.length + 1}`,
      type: selectedPreset,
      mass,
      radius,
      position: { x: pos.x, y: pos.y },
      velocity: { x: vel.x, y: vel.y },
      color,
      trail: [],
      temperature: temp,
      coreFuel: fuel,
      spinAngle: 0,
      spinSpeed: selectedPreset === 'pulsar' ? 12.0 : 0.05,
      isDestroyed: false,
    };

    setBodies((prev) => [...prev, newBody]);
    setSelectedBodyId(id);
    audio.playLaunchBurst();
  };

  // Fixed Physics & Audio Tick (60 FPS)
  useEffect(() => {
    let animId: number;
    let lastTime = performance.now();

    const tick = (now: number) => {
      const dt = Math.min((now - lastTime) / 1000.0, 0.05);
      lastTime = now;

      if (!isPaused && bodies.length > 0) {
        if (timeScale < 0) {
          // Reverse Time Rewind
          if (historyBufferRef.current.length > 0) {
            const prevSnapshot = historyBufferRef.current.pop()!;
            setBodies(prevSnapshot);
          }
        } else {
          // Record Snapshot for Rewind
          historyBufferRef.current.push(
            bodies.map((b) => ({
              ...b,
              position: { ...b.position },
              velocity: { ...b.velocity },
              trail: [...b.trail],
            }))
          );
          if (historyBufferRef.current.length > 300) {
            historyBufferRef.current.shift();
          }

          // Advance Physics Integration
          const effectiveDt = dt * timeScale;
          engine.stepVelocityVerlet(bodies, effectiveDt);

          // Evaluate Collisions & Mergers
          engine.resolveCollisions(bodies, () => {
            audio.playSupernovaBoom();
          });

          // Evaluate Roche Limit Tidal Disruptions
          engine.evaluateRocheDisruptions(bodies, () => {
            audio.playLaunchBurst();
          });

          // Update Spacetime Hum
          const centerWarp = engine.calculateSpacetimeCurvature(0, 0, bodies);
          audio.updateSpacetimeHum(centerWarp);

          // Evaluate Puzzle Mission (if in puzzles mode)
          if (mode === 'puzzles') {
            setPuzzleElapsed((prev) => {
              const next = prev + effectiveDt;
              const res = PUZZLE_MISSIONS[puzzleIndex].evaluate(bodies, next);
              setPuzzleResult(res);
              return next;
            });
          }

          // Trigger state update
          setBodies([...bodies]);
        }
      }

      animId = requestAnimationFrame(tick);
    };

    animId = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(animId);
  }, [bodies, isPaused, timeScale, engine, audio, mode, puzzleIndex]);

  // Selected Body & Attractor Data
  const selectedBody = bodies.find((b) => b.id === selectedBodyId) || null;
  const dominantAttractor = bodies.length > 0 ? bodies[0] : null;

  const conic =
    selectedBody && dominantAttractor && selectedBody.id !== dominantAttractor.id
      ? engine.calculateOsculatingElements(selectedBody, dominantAttractor)
      : null;

  return (
    <div className="relative w-screen h-screen overflow-hidden bg-[#02040a]">
      {/* 1. Dual-Layer Canvas / WebGL Simulation Viewport */}
      <SpacetimeCanvas
        bodies={bodies}
        selectedBodyId={selectedBodyId}
        onSelectBody={setSelectedBodyId}
        engine={engine}
        showTrajectories={showTrajectories}
        showSpacetimeGrid={showSpacetimeGrid}
        showVelocityVectors={showVelocityVectors}
        isSlingshotActive={isSlingshotActive}
        onSlingshotLaunch={handleSlingshotLaunch}
      />

      {/* 2. Top Header Navigation & Controls */}
      <TopHeader
        mode={mode}
        onModeChange={handleModeChange}
        isPaused={isPaused}
        onTogglePause={() => setIsPaused((prev) => !prev)}
        timeScale={timeScale}
        onTimeScaleChange={setTimeScale}
        showTrajectories={showTrajectories}
        onToggleTrajectories={() => setShowTrajectories((prev) => !prev)}
        showSpacetimeGrid={showSpacetimeGrid}
        onToggleSpacetimeGrid={() => setShowSpacetimeGrid((prev) => !prev)}
        showVelocityVectors={showVelocityVectors}
        onToggleVelocityVectors={() => setShowVelocityVectors((prev) => !prev)}
        isMuted={isMuted}
        onToggleMute={() => {
          setIsMuted((prev) => {
            const next = !prev;
            audio.setMuted(next);
            return next;
          });
        }}
        onResetToBarycenter={() => {
          // Compute center of mass
          let totalM = 0;
          let cmX = 0;
          let cmY = 0;
          for (const b of bodies) {
            if (b.isDestroyed) continue;
            totalM += b.mass;
            cmX += b.position.x * b.mass;
            cmY += b.position.y * b.mass;
          }
          if (totalM > 0) {
            cmX /= totalM;
            cmY /= totalM;
          }
        }}
      />

      {/* 3. Left Celestial Drawer with Tactile 3D Icons */}
      <CelestialDrawer
        selectedPreset={selectedPreset}
        onSelectPreset={(type) => {
          setSelectedPreset(type);
          setIsSlingshotActive(true);
        }}
        isSlingshotActive={isSlingshotActive}
        onToggleSlingshot={() => setIsSlingshotActive((prev) => !prev)}
        onClearUniverse={() => setBodies([])}
      />

      {/* 4. Right Orbital Telemetry Inspector */}
      <TelemetryInspector
        selectedBody={selectedBody}
        dominantAttractor={dominantAttractor}
        conic={conic}
        onClose={() => setSelectedBodyId(null)}
      />

      {/* 5. Orbital Puzzles Overlay (when in Puzzles Mode) */}
      {mode === 'puzzles' && (
        <PuzzleOverlay
          activeMissionIndex={puzzleIndex}
          onSelectMissionIndex={(idx) => {
            setPuzzleIndex(idx);
            setPuzzleElapsed(0);
          }}
          onLoadMissionBodies={(newBodies) => {
            setBodies(newBodies);
            setPuzzleElapsed(0);
          }}
          evalResult={puzzleResult}
          onResetMission={() => {
            setPuzzleElapsed(0);
            setBodies(PUZZLE_MISSIONS[puzzleIndex].setup());
          }}
        />
      )}
    </div>
  );
};
