// ============================================================================
// CosmoForge Web: 3D Celestial Tactile Icon Widget
// Conforms to User Specification: Real 3D Icons & Tactile Mobile Touch Feedback
// ============================================================================

import React, { useEffect, useRef, useState } from 'react';
import { BodyType } from '../types';

interface Icon3DProps {
  type: BodyType;
  label: string;
  sublabel: string;
  color: string;
  onClick: () => void;
  isSelected?: boolean;
}

export const Icon3D: React.FC<Icon3DProps> = ({
  type,
  label,
  sublabel,
  color,
  onClick,
  isSelected,
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [isHovered, setIsHovered] = useState(false);
  const [isPressed, setIsPressed] = useState(false);
  const angleRef = useRef(Math.random() * Math.PI * 2);

  useEffect(() => {
    let animId: number;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const render = () => {
      angleRef.current += isHovered ? 0.05 : 0.02;
      const angle = angleRef.current;
      const w = canvas.width;
      const h = canvas.height;
      const cx = w / 2;
      const cy = h / 2;
      const r = w * 0.34;

      ctx.clearRect(0, 0, w, h);

      // 1. Spacetime / Cosmic Glow
      const glow = ctx.createRadialGradient(cx, cy, r * 0.5, cx, cy, r * 1.5);
      glow.addColorStop(0, color + '66');
      glow.addColorStop(1, 'transparent');
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(cx, cy, r * 1.5, 0, Math.PI * 2);
      ctx.fill();

      // 2. Specific 3D Render per type
      if (type === 'black_hole') {
        // Accretion disk behind
        ctx.save();
        ctx.translate(cx, cy);
        ctx.scale(1, 0.35);
        ctx.beginPath();
        ctx.arc(0, 0, r * 1.8, 0, Math.PI * 2);
        ctx.strokeStyle = '#f59e0b';
        ctx.lineWidth = 6;
        ctx.stroke();
        ctx.restore();

        // Event Horizon sphere (pure void black with glowing photon ring)
        ctx.fillStyle = '#000000';
        ctx.beginPath();
        ctx.arc(cx, cy, r, 0, Math.PI * 2);
        ctx.fill();

        ctx.strokeStyle = '#8b5cf6';
        ctx.lineWidth = 2.5;
        ctx.stroke();

        // Einstein ring lensing arc
        ctx.beginPath();
        ctx.arc(cx, cy, r * 1.25, -0.6, 2.5);
        ctx.strokeStyle = '#00f0ff';
        ctx.lineWidth = 2;
        ctx.stroke();
      } else if (type === 'pulsar') {
        // Relativistic synchrotron radiation beam
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(angle * 2);
        const beamGrad = ctx.createLinearGradient(0, -r * 1.8, 0, r * 1.8);
        beamGrad.addColorStop(0, '#00f0ff00');
        beamGrad.addColorStop(0.5, '#00f0ffee');
        beamGrad.addColorStop(1, '#00f0ff00');
        ctx.fillStyle = beamGrad;
        ctx.fillRect(-3, -r * 1.8, 6, r * 3.6);
        ctx.restore();

        // Pulsar sphere
        const pGrad = ctx.createRadialGradient(cx - r * 0.3, cy - r * 0.3, 2, cx, cy, r);
        pGrad.addColorStop(0, '#ffffff');
        pGrad.addColorStop(0.6, '#00f0ff');
        pGrad.addColorStop(1, '#0284c7');
        ctx.fillStyle = pGrad;
        ctx.beginPath();
        ctx.arc(cx, cy, r * 0.7, 0, Math.PI * 2);
        ctx.fill();
      } else {
        // Spherical 3D shading
        const sphereGrad = ctx.createRadialGradient(
          cx - r * 0.35,
          cy - r * 0.35,
          r * 0.1,
          cx,
          cy,
          r
        );
        sphereGrad.addColorStop(0, '#ffffff');
        sphereGrad.addColorStop(0.25, color);
        sphereGrad.addColorStop(1, '#050b14');

        ctx.fillStyle = sphereGrad;
        ctx.beginPath();
        ctx.arc(cx, cy, r, 0, Math.PI * 2);
        ctx.fill();

        // 3D latitude bands / surface features rotating
        ctx.save();
        ctx.beginPath();
        ctx.arc(cx, cy, r, 0, Math.PI * 2);
        ctx.clip();

        // Draw rotating 3D surface lines
        ctx.strokeStyle = '#ffffff22';
        ctx.lineWidth = 1.5;
        for (let i = -2; i <= 2; i++) {
          const latY = cy + (i * r) / 3;
          const bandR = Math.sqrt(Math.max(0, r * r - (latY - cy) * (latY - cy)));
          ctx.beginPath();
          ctx.ellipse(cx, latY, bandR, bandR * 0.3, 0, 0, Math.PI * 2);
          ctx.stroke();
        }

        // Draw rotating longitudinal meridian
        ctx.beginPath();
        const meridianX = cx + Math.sin(angle) * r * 0.8;
        ctx.ellipse(cx, cy, Math.abs(Math.sin(angle) * r), r, 0, 0, Math.PI * 2);
        ctx.stroke();

        ctx.restore();

        // Planetary rings for Gas Giant
        if (type === 'gas_giant') {
          ctx.save();
          ctx.translate(cx, cy);
          ctx.rotate(0.35);
          ctx.scale(1, 0.28);
          ctx.beginPath();
          ctx.arc(0, 0, r * 1.6, 0, Math.PI * 2);
          ctx.strokeStyle = '#fde04788';
          ctx.lineWidth = 4;
          ctx.stroke();
          ctx.restore();
        }

        // Atmospheric rim lighting
        ctx.beginPath();
        ctx.arc(cx, cy, r, 0, Math.PI * 2);
        ctx.strokeStyle = color + 'aa';
        ctx.lineWidth = 2.5;
        ctx.stroke();
      }

      animId = requestAnimationFrame(render);
    };

    render();
    return () => cancelAnimationFrame(animId);
  }, [type, color, isHovered]);

  return (
    <button
      onClick={onClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => {
        setIsHovered(false);
        setIsPressed(false);
      }}
      onMouseDown={() => setIsPressed(true)}
      onMouseUp={() => setIsPressed(false)}
      onTouchStart={() => setIsPressed(true)}
      onTouchEnd={() => setIsPressed(false)}
      className={`group relative flex flex-col items-center p-2 rounded-xl border transition-all duration-200 select-none ${
        isSelected
          ? 'bg-cyan-950/40 border-pulsar shadow-[0_0_15px_rgba(0,240,255,0.4)] scale-105'
          : 'bg-slate-900/60 border-slate-800 hover:border-slate-600 hover:bg-slate-800/50'
      } ${isPressed ? 'scale-95' : isHovered ? 'scale-105' : 'scale-100'}`}
      style={{
        boxShadow: isHovered ? `0 0 20px ${color}44` : undefined,
      }}
    >
      <div className="relative w-14 h-14 flex items-center justify-center">
        <canvas
          ref={canvasRef}
          width={64}
          height={64}
          className="w-14 h-14 drop-shadow-[0_4px_8px_rgba(0,0,0,0.6)]"
        />
        {isSelected && (
          <div className="absolute inset-0 rounded-full border-2 border-pulsar animate-ping opacity-30 pointer-events-none" />
        )}
      </div>
      <span className="text-xs font-semibold text-slate-200 mt-1 tracking-tight">
        {label}
      </span>
      <span className="text-[10px] text-slate-400 font-mono">{sublabel}</span>
    </button>
  );
};
