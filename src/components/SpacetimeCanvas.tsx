// ============================================================================
// CosmoForge Web: High-Performance Spacetime & Orbital Canvas Renderer
// Conforms to Technical Specifications Section 1, 2, 3, 5
// ============================================================================

import React, { useRef, useEffect, useState, useCallback } from 'react';
import { CelestialBodyData, Vector2D } from '../types';
import { GravityEngine } from '../engine/GravityEngine';

interface SpacetimeCanvasProps {
  bodies: CelestialBodyData[];
  selectedBodyId: string | null;
  onSelectBody: (id: string | null) => void;
  engine: GravityEngine;
  showTrajectories: boolean;
  showSpacetimeGrid: boolean;
  showVelocityVectors: boolean;
  isSlingshotActive: boolean;
  onSlingshotLaunch: (pos: Vector2D, vel: Vector2D) => void;
}

export const SpacetimeCanvas: React.FC<SpacetimeCanvasProps> = ({
  bodies,
  selectedBodyId,
  onSelectBody,
  engine,
  showTrajectories,
  showSpacetimeGrid,
  showVelocityVectors,
  isSlingshotActive,
  onSlingshotLaunch,
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  // Camera State
  const [camera, setCamera] = useState<{ x: number; y: number; zoom: number }>({
    x: 0,
    y: 0,
    zoom: 6.0,
  });

  // Dragging & Interaction State
  const isDraggingCamRef = useRef(false);
  const dragStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const camStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });

  // Slingshot State
  const slingshotStartRef = useRef<Vector2D | null>(null);
  const slingshotCurrentRef = useRef<Vector2D | null>(null);

  // Convert screen to simulation world coordinates
  const screenToWorld = useCallback(
    (sx: number, sy: number): Vector2D => {
      const canvas = canvasRef.current;
      if (!canvas) return { x: 0, y: 0 };
      const cx = canvas.width / 2;
      const cy = canvas.height / 2;
      return {
        x: (sx - cx) / camera.zoom + camera.x,
        y: (sy - cy) / camera.zoom + camera.y,
      };
    },
    [camera]
  );

  // Convert simulation world to screen coordinates
  const worldToScreen = useCallback(
    (wx: number, wy: number): Vector2D => {
      const canvas = canvasRef.current;
      if (!canvas) return { x: 0, y: 0 };
      const cx = canvas.width / 2;
      const cy = canvas.height / 2;
      return {
        x: (wx - camera.x) * camera.zoom + cx,
        y: (wy - camera.y) * camera.zoom + cy,
      };
    },
    [camera]
  );

  // Handle Mouse Wheel Zoom
  const handleWheel = (e: React.WheelEvent<HTMLCanvasElement>) => {
    e.preventDefault();
    const zoomFactor = e.deltaY < 0 ? 1.15 : 0.85;
    setCamera((prev) => ({
      ...prev,
      zoom: Math.max(0.05, Math.min(prev.zoom * zoomFactor, 200.0)),
    }));
  };

  // Handle Mouse Down
  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current?.getBoundingClientRect();
    if (!rect) return;
    const sx = e.clientX - rect.left;
    const sy = e.clientY - rect.top;
    const worldPos = screenToWorld(sx, sy);

    if (isSlingshotActive) {
      slingshotStartRef.current = worldPos;
      slingshotCurrentRef.current = worldPos;
      return;
    }

    // Check if clicked near an existing celestial body
    let clickedId: string | null = null;
    let minDist = 20 / camera.zoom; // 20px hit radius

    for (const b of bodies) {
      if (b.isDestroyed) continue;
      const dx = b.position.x - worldPos.x;
      const dy = b.position.y - worldPos.y;
      const d = Math.sqrt(dx * dx + dy * dy);
      if (d < Math.max(b.radius, minDist)) {
        minDist = d;
        clickedId = b.id;
      }
    }

    if (clickedId) {
      onSelectBody(clickedId);
    } else {
      // Start camera panning
      isDraggingCamRef.current = true;
      dragStartRef.current = { x: sx, y: sy };
      camStartRef.current = { x: camera.x, y: camera.y };
      onSelectBody(null);
    }
  };

  // Handle Mouse Move
  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current?.getBoundingClientRect();
    if (!rect) return;
    const sx = e.clientX - rect.left;
    const sy = e.clientY - rect.top;

    if (isSlingshotActive && slingshotStartRef.current) {
      slingshotCurrentRef.current = screenToWorld(sx, sy);
      return;
    }

    if (isDraggingCamRef.current) {
      const dx = (sx - dragStartRef.current.x) / camera.zoom;
      const dy = (sy - dragStartRef.current.y) / camera.zoom;
      setCamera((prev) => ({
        ...prev,
        x: camStartRef.current.x - dx,
        y: camStartRef.current.y - dy,
      }));
    }
  };

  // Handle Mouse Up
  const handleMouseUp = () => {
    if (isSlingshotActive && slingshotStartRef.current && slingshotCurrentRef.current) {
      const start = slingshotStartRef.current;
      const curr = slingshotCurrentRef.current;
      // Slingshot velocity vector: pull backwards to shoot forward
      const vel: Vector2D = {
        x: (start.x - curr.x) * 0.35,
        y: (start.y - curr.y) * 0.35,
      };
      onSlingshotLaunch(start, vel);
      slingshotStartRef.current = null;
      slingshotCurrentRef.current = null;
    }

    isDraggingCamRef.current = false;
  };

  // Main 60 FPS Render Loop
  useEffect(() => {
    let animId: number;
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Resize canvas to match display size
    const resize = () => {
      if (canvas.width !== window.innerWidth || canvas.height !== window.innerHeight) {
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
      }
    };
    resize();
    window.addEventListener('resize', resize);

    const render = () => {
      resize();
      const w = canvas.width;
      const h = canvas.height;

      // 1. Clear with Deep Void Background
      ctx.fillStyle = '#02040a';
      ctx.fillRect(0, 0, w, h);

      // 2. Draw Distant Cosmic Starfield Parallax
      ctx.fillStyle = '#ffffff22';
      for (let i = 0; i < 70; i++) {
        const starX = ((i * 137.5 + camera.x * 0.05) % w + w) % w;
        const starY = ((i * 224.3 + camera.y * 0.05) % h + h) % h;
        const starR = (i % 3 === 0 ? 1.5 : 0.8);
        ctx.beginPath();
        ctx.arc(starX, starY, starR, 0, Math.PI * 2);
        ctx.fill();
      }

      // 3. Draw 2.5D Spacetime Curvature Grid
      if (showSpacetimeGrid) {
        const gridSpacing = 25.0; // In world units
        const gridExtent = 250.0;
        ctx.strokeStyle = '#00f0ff18';
        ctx.lineWidth = 1.0;

        for (let gx = -gridExtent; gx <= gridExtent; gx += gridSpacing) {
          ctx.beginPath();
          for (let gy = -gridExtent; gy <= gridExtent; gy += 10.0) {
            const warp = engine.calculateSpacetimeCurvature(gx, gy, bodies);
            // Visual 2.5D depression: warp depresses Y coordinate
            const s = worldToScreen(gx, gy - warp * 1.8);
            if (gy === -gridExtent) ctx.moveTo(s.x, s.y);
            else ctx.lineTo(s.x, s.y);
          }
          ctx.stroke();
        }

        for (let gy = -gridExtent; gy <= gridExtent; gy += gridSpacing) {
          ctx.beginPath();
          for (let gx = -gridExtent; gx <= gridExtent; gx += 10.0) {
            const warp = engine.calculateSpacetimeCurvature(gx, gy, bodies);
            const s = worldToScreen(gx, gy - warp * 1.8);
            if (gx === -gridExtent) ctx.moveTo(s.x, s.y);
            else ctx.lineTo(s.x, s.y);
          }
          ctx.stroke();
        }
      }

      // 4. Draw Orbit Trails
      for (const b of bodies) {
        if (b.isDestroyed || !b.trail || b.trail.length < 2) continue;
        ctx.beginPath();
        for (let i = 0; i < b.trail.length; i++) {
          const pt = worldToScreen(b.trail[i].x, b.trail[i].y);
          if (i === 0) ctx.moveTo(pt.x, pt.y);
          else ctx.lineTo(pt.x, pt.y);
        }
        ctx.strokeStyle = b.color + '44';
        ctx.lineWidth = Math.max(1.0, 1.5 * (camera.zoom / 10));
        ctx.stroke();
      }

      // 5. Draw Forward Keplerian Ghost Trajectory (500 steps)
      if (showTrajectories && bodies.length > 0) {
        const selected = bodies.find((b) => b.id === selectedBodyId) || bodies[1] || bodies[0];
        if (selected && !selected.isDestroyed) {
          const ghostPoints = engine.predictForwardTrajectory(selected, bodies, 400, 0.04);
          if (ghostPoints.length > 1) {
            ctx.beginPath();
            for (let i = 0; i < ghostPoints.length; i++) {
              const s = worldToScreen(ghostPoints[i].x, ghostPoints[i].y);
              if (i === 0) ctx.moveTo(s.x, s.y);
              else ctx.lineTo(s.x, s.y);
            }
            ctx.strokeStyle = '#00f0ffaa';
            ctx.lineWidth = 1.8;
            ctx.setLineDash([4, 4]);
            ctx.stroke();
            ctx.setLineDash([]);

            // Draw Periapsis (closest) and Apoapsis (farthest) markers
            if (bodies.length > 1 && bodies[0].id !== selected.id) {
              const primary = bodies[0];
              let minDist = Infinity;
              let maxDist = -Infinity;
              let periPt: Vector2D = ghostPoints[0];
              let apoPt: Vector2D = ghostPoints[0];

              for (const pt of ghostPoints) {
                const dx = pt.x - primary.position.x;
                const dy = pt.y - primary.position.y;
                const d = Math.sqrt(dx * dx + dy * dy);
                if (d < minDist) {
                  minDist = d;
                  periPt = pt;
                }
                if (d > maxDist) {
                  maxDist = d;
                  apoPt = pt;
                }
              }

              const pScreen = worldToScreen(periPt.x, periPt.y);
              const aScreen = worldToScreen(apoPt.x, apoPt.y);

              // Periapsis marker (Green circle + "Pe")
              ctx.fillStyle = '#22c55e';
              ctx.beginPath();
              ctx.arc(pScreen.x, pScreen.y, 4, 0, Math.PI * 2);
              ctx.fill();
              ctx.font = '10px JetBrains Mono';
              ctx.fillText('Pe', pScreen.x + 6, pScreen.y - 4);

              // Apoapsis marker (Red circle + "Ap")
              ctx.fillStyle = '#ef4444';
              ctx.beginPath();
              ctx.arc(aScreen.x, aScreen.y, 4, 0, Math.PI * 2);
              ctx.fill();
              ctx.fillText('Ap', aScreen.x + 6, aScreen.y - 4);
            }
          }
        }
      }

      // 6. Draw Celestial Bodies & Relativistic Effects
      for (const b of bodies) {
        if (b.isDestroyed) continue;
        const screen = worldToScreen(b.position.x, b.position.y);
        const radiusPx = Math.max(3.0, b.radius * camera.zoom);

        // A. Planetary Rings (e.g. Saturn / Roche disrupted ring systems)
        if (b.rings) {
          ctx.save();
          ctx.translate(screen.x, screen.y);
          ctx.scale(1.0, 0.35); // Oblique tilt
          ctx.beginPath();
          ctx.arc(0, 0, b.rings.outerRadius * camera.zoom, 0, Math.PI * 2);
          ctx.arc(0, 0, b.rings.innerRadius * camera.zoom, 0, Math.PI * 2, true);
          ctx.fillStyle = b.rings.color + '66';
          ctx.fill();
          ctx.restore();
        }

        // B. Relativistic Supermassive Black Hole
        if (b.type === 'black_hole') {
          // Swirling Accretion Disk with Doppler beaming
          ctx.save();
          ctx.translate(screen.x, screen.y);
          ctx.scale(1.0, 0.4);
          const diskGrad = ctx.createRadialGradient(0, 0, radiusPx * 1.5, 0, 0, radiusPx * 4.5);
          diskGrad.addColorStop(0, '#ffffff');
          diskGrad.addColorStop(0.3, '#f59e0b');
          diskGrad.addColorStop(0.7, '#ec4899');
          diskGrad.addColorStop(1, 'transparent');
          ctx.fillStyle = diskGrad;
          ctx.beginPath();
          ctx.arc(0, 0, radiusPx * 4.5, 0, Math.PI * 2);
          ctx.fill();
          ctx.restore();

          // Einstein Ring Gravitational Lensing Arc
          ctx.strokeStyle = '#00f0ff';
          ctx.lineWidth = 2.5;
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx * 1.35, -0.4, 2.8);
          ctx.stroke();

          // Pure Event Horizon (Schwarzschild sphere)
          ctx.fillStyle = '#000000';
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx, 0, Math.PI * 2);
          ctx.fill();

          ctx.strokeStyle = '#8b5cf6';
          ctx.lineWidth = 1.5;
          ctx.stroke();
        }
        // C. Neutron Star / Pulsar Synchrotron Beams
        else if (b.type === 'pulsar') {
          ctx.save();
          ctx.translate(screen.x, screen.y);
          ctx.rotate(b.spinAngle * 2);
          const beamGrad = ctx.createLinearGradient(0, -radiusPx * 8, 0, radiusPx * 8);
          beamGrad.addColorStop(0, '#00f0ff00');
          beamGrad.addColorStop(0.5, '#00f0ffee');
          beamGrad.addColorStop(1, '#00f0ff00');
          ctx.fillStyle = beamGrad;
          ctx.fillRect(-radiusPx * 0.25, -radiusPx * 8, radiusPx * 0.5, radiusPx * 16);
          ctx.restore();

          // Core Sphere
          ctx.fillStyle = '#00f0ff';
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx, 0, Math.PI * 2);
          ctx.fill();
        }
        // D. Gas Giant, Terrestrial Planets, Stars
        else {
          // Atmospheric Rim & Corona Glow
          const glowGrad = ctx.createRadialGradient(
            screen.x,
            screen.y,
            radiusPx * 0.8,
            screen.x,
            screen.y,
            radiusPx * 2.5
          );
          glowGrad.addColorStop(0, b.color + '88');
          glowGrad.addColorStop(1, 'transparent');
          ctx.fillStyle = glowGrad;
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx * 2.5, 0, Math.PI * 2);
          ctx.fill();

          // Spherical Shading
          const sphereGrad = ctx.createRadialGradient(
            screen.x - radiusPx * 0.3,
            screen.y - radiusPx * 0.3,
            radiusPx * 0.1,
            screen.x,
            screen.y,
            radiusPx
          );
          sphereGrad.addColorStop(0, '#ffffff');
          sphereGrad.addColorStop(0.3, b.color);
          sphereGrad.addColorStop(1, '#030712');

          ctx.fillStyle = sphereGrad;
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx, 0, Math.PI * 2);
          ctx.fill();

          // Atmospheric scattering rim
          ctx.strokeStyle = b.color;
          ctx.lineWidth = 1.5;
          ctx.stroke();
        }

        // Velocity Vectors Overlay
        if (showVelocityVectors) {
          ctx.strokeStyle = '#22c55e';
          ctx.lineWidth = 1.5;
          ctx.beginPath();
          ctx.moveTo(screen.x, screen.y);
          ctx.lineTo(screen.x + b.velocity.x * camera.zoom * 2.0, screen.y + b.velocity.y * camera.zoom * 2.0);
          ctx.stroke();
        }

        // Selected Body Target Reticle
        if (b.id === selectedBodyId) {
          ctx.strokeStyle = '#00f0ff';
          ctx.lineWidth = 1.5;
          ctx.setLineDash([4, 4]);
          ctx.beginPath();
          ctx.arc(screen.x, screen.y, radiusPx + 8, 0, Math.PI * 2);
          ctx.stroke();
          ctx.setLineDash([]);
        }

        // Body Name Label
        ctx.fillStyle = '#e2e8f0';
        ctx.font = '11px Space Grotesk';
        ctx.fillText(b.name, screen.x + radiusPx + 6, screen.y + 4);
      }

      // 7. Slingshot Aim Vector Arrow
      if (isSlingshotActive && slingshotStartRef.current && slingshotCurrentRef.current) {
        const startScreen = worldToScreen(slingshotStartRef.current.x, slingshotStartRef.current.y);
        const currScreen = worldToScreen(slingshotCurrentRef.current.x, slingshotCurrentRef.current.y);

        // Pull-back line (red)
        ctx.strokeStyle = '#ef4444aa';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(startScreen.x, startScreen.y);
        ctx.lineTo(currScreen.x, currScreen.y);
        ctx.stroke();

        // Launch trajectory preview arrow (cyan)
        const launchEndScreen = {
          x: startScreen.x + (startScreen.x - currScreen.x),
          y: startScreen.y + (startScreen.y - currScreen.y),
        };
        ctx.strokeStyle = '#00f0ff';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.moveTo(startScreen.x, startScreen.y);
        ctx.lineTo(launchEndScreen.x, launchEndScreen.y);
        ctx.stroke();

        // Spawn point circle
        ctx.fillStyle = '#00f0ff';
        ctx.beginPath();
        ctx.arc(startScreen.x, startScreen.y, 6, 0, Math.PI * 2);
        ctx.fill();
      }

      animId = requestAnimationFrame(render);
    };

    render();
    return () => {
      window.removeEventListener('resize', resize);
      cancelAnimationFrame(animId);
    };
  }, [
    bodies,
    camera,
    selectedBodyId,
    engine,
    showTrajectories,
    showSpacetimeGrid,
    showVelocityVectors,
    isSlingshotActive,
    worldToScreen,
  ]);

  return (
    <canvas
      ref={canvasRef}
      onWheel={handleWheel}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      className={`absolute inset-0 w-full h-full block ${
        isSlingshotActive ? 'cursor-crosshair' : 'cursor-grab active:cursor-grabbing'
      }`}
    />
  );
};
