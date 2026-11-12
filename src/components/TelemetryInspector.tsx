// ============================================================================
// CosmoForge Web: Live Orbital Telemetry Inspector Card
// Conforms to Technical Specifications Section 6
// ============================================================================

import React from 'react';
import { CelestialBodyData, ConicOrbitElements } from '../types';
import { Activity, Flame, Gauge, Orbit, Radius, Weight, X } from 'lucide-react';

interface TelemetryInspectorProps {
  selectedBody: CelestialBodyData | null;
  dominantAttractor: CelestialBodyData | null;
  conic: ConicOrbitElements | null;
  onClose: () => void;
}

export const TelemetryInspector: React.FC<TelemetryInspectorProps> = ({
  selectedBody,
  conic,
  onClose,
}) => {
  if (!selectedBody) return null;

  const speed = Math.sqrt(
    selectedBody.velocity.x * selectedBody.velocity.x + selectedBody.velocity.y * selectedBody.velocity.y
  );

  return (
    <div className="absolute right-3 top-20 z-20 w-72 bg-slate-950/85 backdrop-blur-md border border-slate-800/80 rounded-2xl p-4 shadow-2xl space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between pb-2 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <div
            className="w-3 h-3 rounded-full shadow-sm"
            style={{ backgroundColor: selectedBody.color }}
          />
          <div>
            <h3 className="text-sm font-bold text-slate-100">{selectedBody.name}</h3>
            <span className="text-[10px] text-cyan-400 font-mono uppercase">
              {selectedBody.type.replace('_', ' ')}
            </span>
          </div>
        </div>
        <button
          onClick={onClose}
          className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition"
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Primary Physical Stats */}
      <div className="grid grid-cols-2 gap-2 text-xs">
        <div className="bg-slate-900/60 p-2 rounded-xl border border-slate-800/50">
          <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-mono">
            <Weight className="w-3 h-3 text-amber-400" /> Mass
          </div>
          <div className="text-slate-200 font-bold font-mono mt-0.5">
            {selectedBody.mass.toLocaleString(undefined, { maximumFractionDigits: 2 })} M⊕
          </div>
        </div>

        <div className="bg-slate-900/60 p-2 rounded-xl border border-slate-800/50">
          <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-mono">
            <Radius className="w-3 h-3 text-cyan-400" /> Radius
          </div>
          <div className="text-slate-200 font-bold font-mono mt-0.5">
            {selectedBody.radius.toFixed(2)} R⊕
          </div>
        </div>

        <div className="bg-slate-900/60 p-2 rounded-xl border border-slate-800/50">
          <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-mono">
            <Gauge className="w-3 h-3 text-emerald-400" /> Velocity
          </div>
          <div className="text-slate-200 font-bold font-mono mt-0.5">
            {speed.toFixed(2)} km/s
          </div>
        </div>

        <div className="bg-slate-900/60 p-2 rounded-xl border border-slate-800/50">
          <div className="flex items-center gap-1.5 text-slate-400 text-[10px] font-mono">
            <Flame className="w-3 h-3 text-rose-400" /> Surface Temp
          </div>
          <div className="text-slate-200 font-bold font-mono mt-0.5">
            {selectedBody.temperature.toFixed(0)} K
          </div>
        </div>
      </div>

      {/* Keplerian Conic Elements */}
      {conic && (
        <div className="pt-2 border-t border-slate-800/80 space-y-1.5">
          <div className="flex items-center justify-between text-[11px] font-mono">
            <span className="text-slate-400 flex items-center gap-1">
              <Orbit className="w-3 h-3 text-purple-400" /> Conic Section:
            </span>
            <span
              className={`px-1.5 py-0.5 rounded text-[10px] font-bold ${
                conic.type === 'Circular'
                  ? 'bg-emerald-950 text-emerald-400 border border-emerald-800'
                  : conic.type === 'Elliptical'
                  ? 'bg-cyan-950 text-cyan-400 border border-cyan-800'
                  : 'bg-amber-950 text-amber-400 border border-amber-800'
              }`}
            >
              {conic.type}
            </span>
          </div>

          <div className="flex items-center justify-between text-[11px] font-mono">
            <span className="text-slate-400">Eccentricity (e):</span>
            <span className="text-slate-200 font-bold">{conic.eccentricity.toFixed(4)}</span>
          </div>

          <div className="flex items-center justify-between text-[11px] font-mono">
            <span className="text-slate-400">Semi-Major Axis (a):</span>
            <span className="text-slate-200 font-bold">
              {Number.isFinite(conic.semiMajorAxis) ? `${conic.semiMajorAxis.toFixed(2)} AU` : '∞'}
            </span>
          </div>

          <div className="flex items-center justify-between text-[11px] font-mono">
            <span className="text-slate-400">Orbital Period:</span>
            <span className="text-slate-200 font-bold">
              {Number.isFinite(conic.orbitalPeriod) ? `${conic.orbitalPeriod.toFixed(1)} d` : 'Unbound'}
            </span>
          </div>
        </div>
      )}
    </div>
  );
};
