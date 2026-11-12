// ============================================================================
// CosmoForge Web: Top Header Bar & Simulation Controls
// Conforms to Technical Specifications Section 6
// ============================================================================

import React from 'react';
import {
  Play,
  Pause,
  FastForward,
  RotateCcw,
  Volume2,
  VolumeX,
  Orbit,
  Grid,
  Navigation,
  Compass,
  Sparkles,
} from 'lucide-react';
import { WorkbenchMode } from '../types';

interface TopHeaderProps {
  mode: WorkbenchMode;
  onModeChange: (mode: WorkbenchMode) => void;
  isPaused: boolean;
  onTogglePause: () => void;
  timeScale: number;
  onTimeScaleChange: (scale: number) => void;
  showTrajectories: boolean;
  onToggleTrajectories: () => void;
  showSpacetimeGrid: boolean;
  onToggleSpacetimeGrid: () => void;
  showVelocityVectors: boolean;
  onToggleVelocityVectors: () => void;
  isMuted: boolean;
  onToggleMute: () => void;
  onResetToBarycenter: () => void;
}

export const TopHeader: React.FC<TopHeaderProps> = ({
  mode,
  onModeChange,
  isPaused,
  onTogglePause,
  timeScale,
  onTimeScaleChange,
  showTrajectories,
  onToggleTrajectories,
  showSpacetimeGrid,
  onToggleSpacetimeGrid,
  showVelocityVectors,
  onToggleVelocityVectors,
  isMuted,
  onToggleMute,
  onResetToBarycenter,
}) => {
  return (
    <header className="absolute top-3 left-3 right-3 z-30 flex items-center justify-between gap-3 px-4 py-2.5 bg-slate-950/80 backdrop-blur-md border border-slate-800/80 rounded-2xl shadow-2xl">
      {/* Brand & Logo */}
      <div className="flex items-center gap-3">
        <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-horizon to-pulsar flex items-center justify-center shadow-lg shadow-pulsar/20">
          <Sparkles className="w-5 h-5 text-slate-950 font-bold" />
        </div>
        <div>
          <h1 className="text-sm font-bold tracking-tight bg-gradient-to-r from-slate-100 via-slate-200 to-pulsar bg-clip-text text-transparent">
            CosmoForge
          </h1>
          <p className="text-[10px] text-slate-400 font-mono leading-none">
            The Gravity & Cosmos Sandbox
          </p>
        </div>
      </div>

      {/* Mode Switcher */}
      <div className="flex items-center bg-slate-900/80 p-1 rounded-xl border border-slate-800">
        <button
          onClick={() => onModeChange('sandbox')}
          className={`px-3 py-1 text-xs font-semibold rounded-lg transition-all ${
            mode === 'sandbox'
              ? 'bg-gradient-to-r from-pulsar to-cyan-500 text-slate-950 shadow-md shadow-pulsar/30'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          Free Sandbox
        </button>
        <button
          onClick={() => onModeChange('solar_system')}
          className={`px-3 py-1 text-xs font-semibold rounded-lg transition-all ${
            mode === 'solar_system'
              ? 'bg-gradient-to-r from-pulsar to-cyan-500 text-slate-950 shadow-md shadow-pulsar/30'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          Solar System
        </button>
        <button
          onClick={() => onModeChange('puzzles')}
          className={`px-3 py-1 text-xs font-semibold rounded-lg transition-all ${
            mode === 'puzzles'
              ? 'bg-gradient-to-r from-horizon to-fuchsia-500 text-slate-100 shadow-md shadow-horizon/30'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          Orbital Puzzles
        </button>
      </div>

      {/* Time-Warp Controls */}
      <div className="flex items-center gap-1.5 bg-slate-900/80 px-2 py-1 rounded-xl border border-slate-800">
        <button
          onClick={onTogglePause}
          className="p-1.5 text-slate-300 hover:text-white rounded-lg hover:bg-slate-800 transition"
          title={isPaused ? 'Resume Simulation' : 'Pause'}
        >
          {isPaused ? <Play className="w-4 h-4 text-emerald-400" /> : <Pause className="w-4 h-4" />}
        </button>

        {[0.1, 1, 10, 100].map((scale) => (
          <button
            key={scale}
            onClick={() => onTimeScaleChange(scale)}
            className={`px-2 py-0.5 text-xs font-mono rounded transition ${
              timeScale === scale
                ? 'bg-cyan-500/20 text-pulsar font-bold border border-pulsar/40'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            {scale}x
          </button>
        ))}

        <button
          onClick={() => onTimeScaleChange(-1)}
          className={`p-1.5 rounded-lg transition ${
            timeScale < 0
              ? 'bg-fuchsia-500/20 text-fuchsia-400 border border-fuchsia-400/40'
              : 'text-slate-400 hover:text-slate-200'
          }`}
          title="Reverse Time Rewind"
        >
          <RotateCcw className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Visual Overlays & Audio */}
      <div className="flex items-center gap-1 bg-slate-900/80 p-1 rounded-xl border border-slate-800">
        <button
          onClick={onToggleTrajectories}
          className={`p-1.5 rounded-lg transition ${
            showTrajectories ? 'bg-cyan-500/20 text-pulsar' : 'text-slate-500 hover:text-slate-300'
          }`}
          title="Toggle Forward Trajectory"
        >
          <Orbit className="w-4 h-4" />
        </button>
        <button
          onClick={onToggleSpacetimeGrid}
          className={`p-1.5 rounded-lg transition ${
            showSpacetimeGrid ? 'bg-cyan-500/20 text-pulsar' : 'text-slate-500 hover:text-slate-300'
          }`}
          title="Toggle Spacetime Curvature Grid"
        >
          <Grid className="w-4 h-4" />
        </button>
        <button
          onClick={onToggleVelocityVectors}
          className={`p-1.5 rounded-lg transition ${
            showVelocityVectors ? 'bg-cyan-500/20 text-pulsar' : 'text-slate-500 hover:text-slate-300'
          }`}
          title="Toggle Velocity Vectors"
        >
          <Navigation className="w-4 h-4" />
        </button>
        <button
          onClick={onResetToBarycenter}
          className="p-1.5 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-slate-800 transition"
          title="Reset Camera to Center of Mass"
        >
          <Compass className="w-4 h-4" />
        </button>
        <div className="w-[1px] h-4 bg-slate-800 mx-1" />
        <button
          onClick={onToggleMute}
          className="p-1.5 text-slate-400 hover:text-slate-200 rounded-lg hover:bg-slate-800 transition"
          title={isMuted ? 'Unmute Audio' : 'Mute Audio'}
        >
          {isMuted ? <VolumeX className="w-4 h-4 text-red-400" /> : <Volume2 className="w-4 h-4 text-cyan-400" />}
        </button>
      </div>
    </header>
  );
};
