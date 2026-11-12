// ============================================================================
// CosmoForge Web: Gamified Orbital Puzzles & Missions Overlay
// Conforms to Technical Specifications Section 4: Gamified Challenges
// ============================================================================

import React, { useState } from 'react';
import { CelestialBodyData, PuzzleMission } from '../types';
import { Award, CheckCircle2, ChevronRight, RotateCcw, Star, Trophy, XCircle } from 'lucide-react';
import confetti from 'canvas-confetti';

interface PuzzleOverlayProps {
  onLoadMissionBodies: (bodies: CelestialBodyData[]) => void;
  activeMissionIndex: number;
  onSelectMissionIndex: (index: number) => void;
  evalResult: {
    status: 'in_progress' | 'victory' | 'defeat';
    stars: number;
    progress: number;
    feedback: string;
  };
  onResetMission: () => void;
}

export const PUZZLE_MISSIONS: PuzzleMission[] = [
  {
    id: 'm1_orbital_injection',
    title: '1. The Orbital Injection',
    description: 'Slingshot a space probe into a stable circular orbit around Earth without crashing or escaping.',
    targetObjective: 'Achieve circular orbit (e ≤ 0.08) and survive 5 seconds.',
    setup: () => [
      {
        id: 'earth_m1',
        name: 'Earth',
        type: 'terrestrial',
        mass: 1000.0,
        radius: 8.0,
        position: { x: 0, y: 0 },
        velocity: { x: 0, y: 0 },
        color: '#38bdf8',
        trail: [],
        temperature: 288,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.05,
        isDestroyed: false,
      },
    ],
    evaluate: (bodies, elapsed) => {
      if (bodies.length < 2) {
        return {
          status: 'in_progress',
          stars: 0,
          progress: 0,
          feedback: 'Use Slingshot mode to launch a probe into Earth orbit.',
        };
      }
      const earth = bodies[0];
      const probe = bodies[1];

      if (probe.isDestroyed) {
        return {
          status: 'defeat',
          stars: 0,
          progress: 0,
          feedback: 'Probe was destroyed by collision!',
        };
      }

      const dx = probe.position.x - earth.position.x;
      const dy = probe.position.y - earth.position.y;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist <= earth.radius) {
        probe.isDestroyed = true;
        return {
          status: 'defeat',
          stars: 0,
          progress: 0,
          feedback: 'Probe impacted Earth surface!',
        };
      }
      if (dist > 160) {
        return {
          status: 'defeat',
          stars: 0,
          progress: 0,
          feedback: 'Probe escaped Earth gravitational well!',
        };
      }

      // Orbital elements
      const vx = probe.velocity.x - earth.velocity.x;
      const vy = probe.velocity.y - earth.velocity.y;
      const v2 = vx * vx + vy * vy;
      const mu = 1.0 * (earth.mass + probe.mass);
      const h = dx * vy - dy * vx;
      const energy = 0.5 * v2 - mu / dist;
      const ecc = Math.sqrt(Math.max(0, 1.0 + (2.0 * energy * h * h) / (mu * mu)));

      const reqTime = 5.0;
      const progress = Math.min(1.0, elapsed / reqTime);

      if (elapsed >= reqTime) {
        const stars = ecc <= 0.08 ? 3 : ecc <= 0.2 ? 2 : 1;
        return {
          status: 'victory',
          stars,
          progress: 1.0,
          feedback: `Orbit Stable! Eccentricity: ${ecc.toFixed(3)}. You achieved ${stars} stars!`,
        };
      }

      return {
        status: 'in_progress',
        stars: 0,
        progress,
        feedback: `Holding orbit... Eccentricity: ${ecc.toFixed(3)}. Progress: ${(progress * 100).toFixed(0)}%`,
      };
    },
  },
  {
    id: 'm2_lagrange_parking',
    title: '2. Lagrange Parking',
    description: 'Park a space telescope into the L1 or L4/L5 gravitational equilibrium sweet spot of Earth-Moon.',
    targetObjective: 'Maintain station-keeping within equilibrium zone for 6 seconds.',
    setup: () => {
      const G = 1.0;
      const mEarth = 1000.0;
      const mMoon = 60.0;
      const totalM = mEarth + mMoon;
      const r = 60.0;
      const vRel = Math.sqrt((G * totalM) / r);

      return [
        {
          id: 'earth_m2',
          name: 'Earth',
          type: 'terrestrial',
          mass: mEarth,
          radius: 6.0,
          position: { x: -(mMoon / totalM) * r, y: 0 },
          velocity: { x: 0, y: -(mMoon / totalM) * vRel },
          color: '#38bdf8',
          trail: [],
          temperature: 288,
          coreFuel: 0,
          spinAngle: 0,
          spinSpeed: 0.05,
          isDestroyed: false,
        },
        {
          id: 'moon_m2',
          name: 'Moon',
          type: 'moon',
          mass: mMoon,
          radius: 2.2,
          position: { x: (mEarth / totalM) * r, y: 0 },
          velocity: { x: 0, y: (mEarth / totalM) * vRel },
          color: '#cbd5e1',
          trail: [],
          temperature: 250,
          coreFuel: 0,
          spinAngle: 0,
          spinSpeed: 0.05,
          isDestroyed: false,
        },
      ];
    },
    evaluate: (bodies, elapsed) => {
      if (bodies.length < 3) {
        return {
          status: 'in_progress',
          stars: 0,
          progress: 0,
          feedback: 'Deploy space telescope probe near the L1 or L4/L5 sweet spots.',
        };
      }
      const earth = bodies[0];
      const moon = bodies[1];
      const tele = bodies[2];

      if (tele.isDestroyed) {
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Telescope collided with celestial body!' };
      }

      const dx = moon.position.x - earth.position.x;
      const dy = moon.position.y - earth.position.y;
      const dEM = Math.sqrt(dx * dx + dy * dy);
      const gamma = Math.cbrt(moon.mass / (3.0 * earth.mass));

      // L1 target point
      const l1X = moon.position.x - (dx / dEM) * (dEM * gamma);
      const l1Y = moon.position.y - (dy / dEM) * (dEM * gamma);
      const distToL1 = Math.sqrt((tele.position.x - l1X) ** 2 + (tele.position.y - l1Y) ** 2);

      const tol = 12.0;
      if (distToL1 <= tol) {
        const reqTime = 6.0;
        const progress = Math.min(1.0, elapsed / reqTime);
        if (elapsed >= reqTime) {
          return { status: 'victory', stars: 3, progress: 1.0, feedback: 'Lagrange Station-Keeping Successful!' };
        }
        return { status: 'in_progress', stars: 0, progress, feedback: `Holding L1 point... ${(progress * 100).toFixed(0)}%` };
      }

      return { status: 'in_progress', stars: 0, progress: 0, feedback: `Distance to L1 point: ${distToL1.toFixed(1)} units.` };
    },
  },
  {
    id: 'm3_planetary_defense',
    title: '3. Planetary Defense',
    description: 'An extinction asteroid is on direct collision course with Earth. Deploy a gravity-tractor probe to deflect it.',
    targetObjective: 'Alter asteroid trajectory to achieve safe flyby distance > 30 units.',
    setup: () => [
      {
        id: 'earth_m3',
        name: 'Earth',
        type: 'terrestrial',
        mass: 2000.0,
        radius: 8.0,
        position: { x: 0, y: 0 },
        velocity: { x: 0, y: 0 },
        color: '#38bdf8',
        trail: [],
        temperature: 288,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.05,
        isDestroyed: false,
      },
      {
        id: 'asteroid_m3',
        name: 'Apophis',
        type: 'asteroid',
        mass: 30.0,
        radius: 1.5,
        position: { x: 130.0, y: 1.0 },
        velocity: { x: -11.5, y: 0 },
        color: '#94a3b8',
        trail: [],
        temperature: 200,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.1,
        isDestroyed: false,
      },
    ],
    evaluate: (bodies, elapsed) => {
      const earth = bodies[0];
      const ast = bodies[1];
      if (!ast || ast.isDestroyed) {
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Extinction Impact Occurred on Earth!' };
      }

      const dist = Math.sqrt((ast.position.x - earth.position.x) ** 2 + (ast.position.y - earth.position.y) ** 2);
      if (dist <= earth.radius + ast.radius) {
        ast.isDestroyed = true;
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Cataclysmic asteroid impact!' };
      }

      if (ast.position.x < -20.0) {
        if (dist >= 30.0) {
          return { status: 'victory', stars: 3, progress: 1.0, feedback: `Earth Saved! Deflected to ${dist.toFixed(1)} AU!` };
        } else {
          return { status: 'victory', stars: 1, progress: 1.0, feedback: `Close shave! Flyby distance: ${dist.toFixed(1)} AU.` };
        }
      }

      return { status: 'in_progress', stars: 0, progress: Math.min(1.0, elapsed / 8.0), feedback: `Asteroid inbound! Dist: ${dist.toFixed(1)} units.` };
    },
  },
  {
    id: 'm4_barycentric_ballet',
    title: '4. Barycentric Ballet',
    description: 'Two massive stars orbit their barycenter. Place an inhabited planet into a stable circumbinary orbit.',
    targetObjective: 'Circumbinary orbit radius 50-160 units stable for 8 seconds.',
    setup: () => {
      const G = 1.0;
      const m1 = 1500.0;
      const m2 = 1200.0;
      const totalM = m1 + m2;
      const sep = 25.0;
      const vRel = Math.sqrt((G * totalM) / sep);

      return [
        {
          id: 'starA',
          name: 'Sol A',
          type: 'star',
          mass: m1,
          radius: 5.0,
          position: { x: -(m2 / totalM) * sep, y: 0 },
          velocity: { x: 0, y: -(m2 / totalM) * vRel },
          color: '#f59e0b',
          trail: [],
          temperature: 5800,
          coreFuel: 1.0,
          spinAngle: 0,
          spinSpeed: 0.05,
          isDestroyed: false,
        },
        {
          id: 'starB',
          name: 'Sol B',
          type: 'star',
          mass: m2,
          radius: 4.2,
          position: { x: (m1 / totalM) * sep, y: 0 },
          velocity: { x: 0, y: (m1 / totalM) * vRel },
          color: '#ec4899',
          trail: [],
          temperature: 4600,
          coreFuel: 1.0,
          spinAngle: 0,
          spinSpeed: 0.05,
          isDestroyed: false,
        },
      ];
    },
    evaluate: (bodies, elapsed) => {
      if (bodies.length < 3) {
        return { status: 'in_progress', stars: 0, progress: 0, feedback: 'Launch inhabited planet into circumbinary orbit.' };
      }
      const planet = bodies[2];
      if (planet.isDestroyed) {
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Planet collided with binary star!' };
      }

      const dist = Math.sqrt(planet.position.x ** 2 + planet.position.y ** 2);
      if (dist < 40 || dist > 180) {
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Planet left stable circumbinary habitable zone!' };
      }

      const reqTime = 8.0;
      const progress = Math.min(1.0, elapsed / reqTime);
      if (elapsed >= reqTime) {
        return { status: 'victory', stars: 3, progress: 1.0, feedback: 'Circumbinary Ballet Mastered!' };
      }

      return { status: 'in_progress', stars: 0, progress, feedback: `Circumbinary orbit stable... ${(progress * 100).toFixed(0)}%` };
    },
  },
  {
    id: 'm5_ring_smith',
    title: '5. The Ring Smith',
    description: 'Carefully de-orbit an icy moon so that it crosses the gas giant Roche limit, forging a glorious ring system.',
    targetObjective: 'Cross fluid Roche limit without crashing into planet surface.',
    setup: () => [
      {
        id: 'chronos',
        name: 'Chronos',
        type: 'gas_giant',
        mass: 3000.0,
        radius: 12.0,
        position: { x: 0, y: 0 },
        velocity: { x: 0, y: 0 },
        color: '#fbbf24',
        trail: [],
        temperature: 165,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.05,
        isDestroyed: false,
      },
      {
        id: 'cryo',
        name: 'Cryo Moon',
        type: 'moon',
        mass: 10.0,
        radius: 2.0,
        position: { x: 55.0, y: 0 },
        velocity: { x: 0, y: Math.sqrt(3000.0 / 55.0) },
        color: '#e2e8f0',
        trail: [],
        temperature: 100,
        coreFuel: 0,
        spinAngle: 0,
        spinSpeed: 0.05,
        isDestroyed: false,
      },
    ],
    evaluate: (bodies, elapsed) => {
      const giant = bodies[0];
      const moon = bodies[1];

      if (giant.rings) {
        return {
          status: 'victory',
          stars: 3,
          progress: 1.0,
          feedback: 'Glorious Saturn-style planetary rings created! 85% particle retention!',
        };
      }

      if (!moon || moon.isDestroyed) {
        return { status: 'defeat', stars: 0, progress: 0, feedback: 'Moon crashed into planet core!' };
      }

      const dist = Math.sqrt(moon.position.x ** 2 + moon.position.y ** 2);
      return {
        status: 'in_progress',
        stars: 0,
        progress: Math.min(1.0, elapsed / 8.0),
        feedback: `Nudge moon inward towards Roche threshold... Distance: ${dist.toFixed(1)} units.`,
      };
    },
  },
];

export const PuzzleOverlay: React.FC<PuzzleOverlayProps> = ({
  onLoadMissionBodies,
  activeMissionIndex,
  onSelectMissionIndex,
  evalResult,
  onResetMission,
}) => {
  const mission = PUZZLE_MISSIONS[activeMissionIndex];

  const handleSelect = (idx: number) => {
    onSelectMissionIndex(idx);
    onLoadMissionBodies(PUZZLE_MISSIONS[idx].setup());
  };

  // Trigger Confetti on Victory
  React.useEffect(() => {
    if (evalResult.status === 'victory') {
      confetti({
        particleCount: 100,
        spread: 70,
        origin: { y: 0.6 },
        colors: ['#00f0ff', '#8b5cf6', '#f59e0b', '#ec4899'],
      });
    }
  }, [evalResult.status]);

  return (
    <div className="absolute top-20 left-1/2 -translate-x-1/2 z-20 w-[480px] bg-slate-950/85 backdrop-blur-md border border-slate-800/80 rounded-2xl p-4 shadow-2xl space-y-3">
      {/* Level Selector Tabs */}
      <div className="flex items-center justify-between pb-2 border-b border-slate-800">
        <div className="flex items-center gap-1.5 overflow-x-auto py-1">
          {PUZZLE_MISSIONS.map((m, idx) => (
            <button
              key={m.id}
              onClick={() => handleSelect(idx)}
              className={`px-2.5 py-1 text-xs font-bold rounded-lg transition-all ${
                activeMissionIndex === idx
                  ? 'bg-fuchsia-600 text-white shadow-md shadow-fuchsia-600/30'
                  : 'bg-slate-900/60 text-slate-400 hover:text-slate-200'
              }`}
            >
              L{idx + 1}
            </button>
          ))}
        </div>

        <button
          onClick={onResetMission}
          className="p-1 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          title="Restart Mission"
        >
          <RotateCcw className="w-4 h-4" />
        </button>
      </div>

      {/* Mission Objective & Title */}
      <div>
        <h3 className="text-sm font-bold text-slate-100 flex items-center gap-2">
          <Trophy className="w-4 h-4 text-amber-400" />
          {mission.title}
        </h3>
        <p className="text-xs text-slate-300 mt-1">{mission.description}</p>
        <p className="text-[11px] text-fuchsia-400 font-mono mt-1">
          Target: {mission.targetObjective}
        </p>
      </div>

      {/* Real-Time Mission Status & Progress */}
      <div className="bg-slate-900/60 p-3 rounded-xl border border-slate-800/60 space-y-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5 text-xs font-mono">
            {evalResult.status === 'victory' ? (
              <CheckCircle2 className="w-4 h-4 text-emerald-400" />
            ) : evalResult.status === 'defeat' ? (
              <XCircle className="w-4 h-4 text-rose-400" />
            ) : (
              <Award className="w-4 h-4 text-cyan-400 animate-pulse" />
            )}
            <span
              className={
                evalResult.status === 'victory'
                  ? 'text-emerald-400 font-bold'
                  : evalResult.status === 'defeat'
                  ? 'text-rose-400 font-bold'
                  : 'text-slate-300'
              }
            >
              {evalResult.status === 'victory'
                ? 'MISSION ACCOMPLISHED!'
                : evalResult.status === 'defeat'
                ? 'MISSION FAILED'
                : 'MISSION IN PROGRESS'}
            </span>
          </div>

          {/* Star Rating Display */}
          <div className="flex items-center gap-0.5">
            {[1, 2, 3].map((star) => (
              <Star
                key={star}
                className={`w-4 h-4 ${
                  star <= evalResult.stars
                    ? 'text-amber-400 fill-amber-400 drop-shadow-[0_0_6px_rgba(245,158,11,0.6)]'
                    : 'text-slate-700'
                }`}
              />
            ))}
          </div>
        </div>

        {/* Progress Bar */}
        <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
          <div
            className={`h-full transition-all duration-300 ${
              evalResult.status === 'victory'
                ? 'bg-emerald-400'
                : evalResult.status === 'defeat'
                ? 'bg-rose-500'
                : 'bg-gradient-to-r from-pulsar to-fuchsia-500'
            }`}
            style={{ width: `${evalResult.progress * 100}%` }}
          />
        </div>

        <p className="text-xs text-slate-300 font-mono">{evalResult.feedback}</p>
      </div>
    </div>
  );
};
