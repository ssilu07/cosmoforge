// ============================================================================
// CosmoForge Web: Core Types & Interfaces
// ============================================================================

export type BodyType =
  | 'asteroid'
  | 'moon'
  | 'terrestrial'
  | 'gas_giant'
  | 'star'
  | 'hypergiant'
  | 'pulsar'
  | 'black_hole';

export interface Vector2D {
  x: number;
  y: number;
}

export interface CelestialBodyData {
  id: string;
  name: string;
  type: BodyType;
  mass: number; // In Earth masses (M_earth)
  radius: number; // In Earth radii (R_earth)
  position: Vector2D; // In AU or scaled units
  velocity: Vector2D; // In km/s or scaled units
  color: string;
  trail: Vector2D[];
  temperature: number; // Kelvin
  coreFuel: number; // 0.0 - 1.0 for stars
  spinAngle: number; // Radians
  spinSpeed: number; // Radians per sec
  isDestroyed: boolean;
  rings?: {
    innerRadius: number;
    outerRadius: number;
    color: string;
  };
}

export type WorkbenchMode = 'sandbox' | 'solar_system' | 'puzzles';

export interface ConicOrbitElements {
  semiMajorAxis: number;
  eccentricity: number;
  periapsisDistance: number;
  apoapsisDistance: number;
  orbitalPeriod: number;
  specificEnergy: number;
  type: 'Circular' | 'Elliptical' | 'Parabolic' | 'Hyperbolic';
}

export interface PuzzleMission {
  id: string;
  title: string;
  description: string;
  targetObjective: string;
  setup: () => CelestialBodyData[];
  evaluate: (bodies: CelestialBodyData[], elapsed: number) => {
    status: 'in_progress' | 'victory' | 'defeat';
    stars: number;
    progress: number;
    feedback: string;
  };
}
