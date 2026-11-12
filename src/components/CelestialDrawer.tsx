// ============================================================================
// CosmoForge Web: Celestial Body Catalog Drawer with Tactile 3D Icons
// Conforms to User Specification: Real 3D Icons & Tactile Mobile Touch Feedback
// ============================================================================

import React from 'react';
import { BodyType } from '../types';
import { Icon3D } from './Icon3D';
import { Crosshair } from 'lucide-react';

interface CelestialDrawerProps {
  selectedPreset: BodyType | null;
  onSelectPreset: (type: BodyType) => void;
  isSlingshotActive: boolean;
  onToggleSlingshot: () => void;
  onClearUniverse: () => void;
}

export const CelestialDrawer: React.FC<CelestialDrawerProps> = ({
  selectedPreset,
  onSelectPreset,
  isSlingshotActive,
  onToggleSlingshot,
  onClearUniverse,
}) => {
  const celestialPresets: {
    type: BodyType;
    label: string;
    sublabel: string;
    color: string;
  }[] = [
    { type: 'asteroid', label: 'Asteroid', sublabel: '0.001 M⊕', color: '#94a3b8' },
    { type: 'moon', label: 'Moon', sublabel: '0.012 M⊕', color: '#cbd5e1' },
    { type: 'terrestrial', label: 'Earth-like', sublabel: '1.0 M⊕', color: '#38bdf8' },
    { type: 'gas_giant', label: 'Gas Giant', sublabel: '318 M⊕', color: '#fbbf24' },
    { type: 'star', label: 'Yellow Sun', sublabel: '1,000 M⊕', color: '#f59e0b' },
    { type: 'hypergiant', label: 'Hypergiant', sublabel: '25,000 M⊕', color: '#3b82f6' },
    { type: 'pulsar', label: 'Pulsar', sublabel: '1,800 M⊕', color: '#00f0ff' },
    { type: 'black_hole', label: 'Black Hole', sublabel: '50,000 M⊕', color: '#8b5cf6' },
  ];

  return (
    <aside className="absolute left-3 top-20 bottom-3 z-20 flex flex-col justify-between w-64 bg-slate-950/80 backdrop-blur-md border border-slate-800/80 rounded-2xl p-3 shadow-2xl overflow-y-auto">
      <div>
        <div className="flex items-center justify-between pb-2 mb-2 border-b border-slate-800/80">
          <span className="text-xs font-bold uppercase tracking-wider text-slate-300">
            Celestial Forge
          </span>
          <span className="text-[10px] text-cyan-400 font-mono font-medium">3D Tactile</span>
        </div>

        {/* Slingshot Launch Mode Button */}
        <button
          onClick={onToggleSlingshot}
          className={`w-full mb-3 flex items-center justify-center gap-2 py-2 px-3 rounded-xl text-xs font-bold transition-all shadow-lg ${
            isSlingshotActive
              ? 'bg-gradient-to-r from-pulsar to-cyan-500 text-slate-950 shadow-pulsar/30 ring-2 ring-pulsar'
              : 'bg-slate-900 border border-slate-800 text-slate-300 hover:border-slate-600 hover:text-white'
          }`}
        >
          <Crosshair className={`w-4 h-4 ${isSlingshotActive ? 'animate-spin' : ''}`} />
          {isSlingshotActive ? 'Slingshot Active (Drag Canvas)' : 'Activate Slingshot'}
        </button>

        {/* 3D Celestial Icons Grid */}
        <div className="grid grid-cols-2 gap-2">
          {celestialPresets.map((preset) => (
            <Icon3D
              key={preset.type}
              type={preset.type}
              label={preset.label}
              sublabel={preset.sublabel}
              color={preset.color}
              isSelected={selectedPreset === preset.type}
              onClick={() => onSelectPreset(preset.type)}
            />
          ))}
        </div>
      </div>

      {/* Drawer Bottom Actions */}
      <div className="pt-3 border-t border-slate-800/80 space-y-1.5">
        <button
          onClick={onClearUniverse}
          className="w-full py-1.5 text-xs font-semibold text-rose-400 bg-rose-950/20 hover:bg-rose-950/40 border border-rose-900/40 rounded-xl transition"
        >
          Clear Universe
        </button>
      </div>
    </aside>
  );
};
