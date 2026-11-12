// ============================================================================
// CosmoForge Web: Symplectic Gravity & Keplerian Orbit Engine
// Conforms to Technical Specifications Section 1, 2, 3
// ============================================================================

import { CelestialBodyData, ConicOrbitElements, Vector2D } from '../types';

export class GravityEngine {
  public G: number = 1.0;
  public softening: number = 0.05; // Plummer epsilon
  public c: number = 50.0; // Speed of light in simulation units

  /**
   * Calculates gravitational acceleration on a target body from all other bodies.
   */
  public computeAccelerations(bodies: CelestialBodyData[]): Vector2D[] {
    const n = bodies.length;
    const accels: Vector2D[] = Array.from({ length: n }, () => ({ x: 0, y: 0 }));
    const epsSqr = this.softening * this.softening;

    for (let i = 0; i < n; i++) {
      const b1 = bodies[i];
      if (b1.isDestroyed) continue;

      for (let j = i + 1; j < n; j++) {
        const b2 = bodies[j];
        if (b2.isDestroyed) continue;

        const dx = b2.position.x - b1.position.x;
        const dy = b2.position.y - b1.position.y;
        const r2 = dx * dx + dy * dy + epsSqr;
        const dist = Math.sqrt(r2);
        const invDist3 = 1.0 / (dist * dist * dist);

        // F = G * m1 * m2 / r^2
        const fOverR = this.G * invDist3;

        accels[i].x += dx * (b2.mass * fOverR);
        accels[i].y += dy * (b2.mass * fOverR);

        accels[j].x -= dx * (b1.mass * fOverR);
        accels[j].y -= dy * (b1.mass * fOverR);
      }
    }

    return accels;
  }

  /**
   * Symplectic Velocity Verlet Integrator Step:
   * x(t + dt) = x(t) + v(t)*dt + 0.5 * a(t) * dt^2
   * v(t + dt) = v(t) + 0.5 * (a(t) + a(t + dt)) * dt
   */
  public stepVelocityVerlet(bodies: CelestialBodyData[], dt: number): void {
    if (bodies.length === 0 || dt === 0) return;

    // Step 1: Initial accelerations a(t)
    const a0 = this.computeAccelerations(bodies);

    // Step 2: Update positions x(t + dt)
    for (let i = 0; i < bodies.length; i++) {
      const b = bodies[i];
      if (b.isDestroyed) continue;

      b.position.x += b.velocity.x * dt + 0.5 * a0[i].x * dt * dt;
      b.position.y += b.velocity.y * dt + 0.5 * a0[i].y * dt * dt;

      // Update rotational spin
      b.spinAngle += b.spinSpeed * dt;

      // Store trail history
      if (!b.trail) b.trail = [];
      b.trail.push({ x: b.position.x, y: b.position.y });
      if (b.trail.length > 80) {
        b.trail.shift();
      }
    }

    // Step 3: Compute new accelerations a(t + dt)
    const a1 = this.computeAccelerations(bodies);

    // Step 4: Update velocities v(t + dt)
    for (let i = 0; i < bodies.length; i++) {
      const b = bodies[i];
      if (b.isDestroyed) continue;

      b.velocity.x += 0.5 * (a0[i].x + a1[i].x) * dt;
      b.velocity.y += 0.5 * (a0[i].y + a1[i].y) * dt;
    }
  }

  /**
   * Computes analytical osculating Keplerian orbital elements (conic section).
   */
  public calculateOsculatingElements(
    body: CelestialBodyData,
    attractor: CelestialBodyData
  ): ConicOrbitElements {
    const rx = body.position.x - attractor.position.x;
    const ry = body.position.y - attractor.position.y;
    const vx = body.velocity.x - attractor.velocity.x;
    const vy = body.velocity.y - attractor.velocity.y;

    const r = Math.sqrt(rx * rx + ry * ry);
    const vSqr = vx * vx + vy * vy;
    const mu = this.G * (attractor.mass + body.mass);

    if (r < 1e-6 || mu < 1e-12) {
      return {
        semiMajorAxis: r,
        eccentricity: 0,
        periapsisDistance: r,
        apoapsisDistance: r,
        orbitalPeriod: 0,
        specificEnergy: 0,
        type: 'Circular',
      };
    }

    // Specific orbital energy: E = v^2/2 - mu/r
    const specificEnergy = 0.5 * vSqr - mu / r;

    // Relative specific angular momentum h = r x v (Z component in 2D)
    const h = rx * vy - ry * vx;
    const hSqr = h * h;

    // Eccentricity: e = sqrt(1 + 2 * E * h^2 / mu^2)
    const eTerm = 1.0 + (2.0 * specificEnergy * hSqr) / (mu * mu);
    const ecc = Math.sqrt(Math.max(0.0, eTerm));

    let a: number;
    let rPeri: number;
    let rApo: number;
    let period: number;
    let type: ConicOrbitElements['type'];

    if (ecc < 1e-4) {
      type = 'Circular';
      a = r;
      rPeri = r;
      rApo = r;
      period = 2.0 * Math.PI * Math.sqrt((a * a * a) / mu);
    } else if (ecc < 1.0 - 1e-4) {
      type = 'Elliptical';
      a = -mu / (2.0 * specificEnergy);
      rPeri = a * (1.0 - ecc);
      rApo = a * (1.0 + ecc);
      period = a > 0 ? 2.0 * Math.PI * Math.sqrt((a * a * a) / mu) : Infinity;
    } else if (Math.abs(ecc - 1.0) <= 1e-4) {
      type = 'Parabolic';
      a = Infinity;
      rPeri = hSqr / (2.0 * mu);
      rApo = Infinity;
      period = Infinity;
    } else {
      type = 'Hyperbolic';
      a = specificEnergy > 1e-9 ? -mu / (2.0 * specificEnergy) : Infinity;
      rPeri = Math.abs(a) * (ecc - 1.0);
      rApo = Infinity;
      period = Infinity;
    }

    return {
      semiMajorAxis: a,
      eccentricity: ecc,
      periapsisDistance: rPeri,
      apoapsisDistance: rApo,
      orbitalPeriod: period,
      specificEnergy,
      type,
    };
  }

  /**
   * Predicts forward ghost trajectory coordinates up to maxSteps (500 steps)
   * without mutating live world states.
   */
  public predictForwardTrajectory(
    target: CelestialBodyData,
    allBodies: CelestialBodyData[],
    maxSteps: number = 300,
    dt: number = 0.04
  ): Vector2D[] {
    const points: Vector2D[] = [];
    let curX = target.position.x;
    let curY = target.position.y;
    let curVx = target.velocity.x;
    let curVy = target.velocity.y;
    const epsSqr = this.softening * this.softening;

    for (let s = 0; s < maxSteps; s++) {
      let ax = 0;
      let ay = 0;

      for (let i = 0; i < allBodies.length; i++) {
        const other = allBodies[i];
        if (other.id === target.id || other.isDestroyed) continue;

        const dx = other.position.x - curX;
        const dy = other.position.y - curY;
        const distSqr = dx * dx + dy * dy + epsSqr;
        const dist = Math.sqrt(distSqr);
        const invDist3 = 1.0 / (dist * dist * dist);
        const fOverR = this.G * other.mass * invDist3;

        ax += dx * fOverR;
        ay += dy * fOverR;
      }

      curVx += ax * dt;
      curVy += ay * dt;
      curX += curVx * dt;
      curY += curVy * dt;

      points.push({ x: curX, y: curY });
    }

    return points;
  }

  /**
   * Detects and resolves perfectly inelastic collisions conserving linear momentum.
   */
  public resolveCollisions(
    bodies: CelestialBodyData[],
    onCollision?: (primary: CelestialBodyData, secondary: CelestialBodyData, vMerged: Vector2D) => void
  ): void {
    const n = bodies.length;

    for (let i = 0; i < n; i++) {
      const b1 = bodies[i];
      if (b1.isDestroyed) continue;

      for (let j = i + 1; j < n; j++) {
        const b2 = bodies[j];
        if (b2.isDestroyed) continue;

        const dx = b2.position.x - b1.position.x;
        const dy = b2.position.y - b1.position.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        const contactDist = b1.radius + b2.radius;

        if (dist <= contactDist) {
          const primary = b1.mass >= b2.mass ? b1 : b2;
          const secondary = b1.mass >= b2.mass ? b2 : b1;

          const totalM = primary.mass + secondary.mass;
          const vMerged: Vector2D = {
            x: (primary.mass * primary.velocity.x + secondary.mass * secondary.velocity.x) / totalM,
            y: (primary.mass * primary.velocity.y + secondary.mass * secondary.velocity.y) / totalM,
          };

          // Merged radius scales by volume
          primary.radius = Math.cbrt(Math.pow(primary.radius, 3) + Math.pow(secondary.radius, 3));
          primary.mass = totalM;
          primary.velocity = vMerged;

          secondary.isDestroyed = true;

          onCollision?.(primary, secondary, vMerged);
        }
      }
    }
  }

  /**
   * Evaluates fluid Roche limit tidal disruption into planetary rings:
   * d_Roche = 2.44 * R_M * (rho_M / rho_m)^(1/3)
   */
  public evaluateRocheDisruptions(
    bodies: CelestialBodyData[],
    onDisrupted?: (primary: CelestialBodyData, moon: CelestialBodyData) => void
  ): void {
    for (let i = 0; i < bodies.length; i++) {
      const primary = bodies[i];
      if (primary.isDestroyed || primary.radius <= 0) continue;

      for (let j = 0; j < bodies.length; j++) {
        if (i === j) continue;
        const moon = bodies[j];
        if (moon.isDestroyed || moon.mass >= primary.mass || moon.radius <= 0) continue;

        const volP = (4.0 / 3.0) * Math.PI * Math.pow(primary.radius, 3);
        const volM = (4.0 / 3.0) * Math.PI * Math.pow(moon.radius, 3);
        const rhoP = primary.mass / volP;
        const rhoM = moon.mass / volM;

        if (rhoM <= 0) continue;

        const dRoche = 2.44 * primary.radius * Math.cbrt(rhoP / rhoM);
        const dx = moon.position.x - primary.position.x;
        const dy = moon.position.y - primary.position.y;
        const dist = Math.sqrt(dx * dx + dy * dy);

        if (dist < dRoche && dist > primary.radius) {
          moon.isDestroyed = true;
          // Spawn ring on primary
          primary.rings = {
            innerRadius: primary.radius * 1.3,
            outerRadius: dRoche * 1.1,
            color: moon.color || '#94a3b8',
          };
          onDisrupted?.(primary, moon);
        }
      }
    }
  }

  /**
   * Calculates 2.5D Spacetime Curvature Grid depression at a coordinate (x, y):
   * z_warp(x, y) = - sum_i [ G * m_i / (dist_i + c^2) ]
   */
  public calculateSpacetimeCurvature(x: number, y: number, bodies: CelestialBodyData[]): number {
    const cSq = this.c * this.c;
    let warp = 0;

    for (let i = 0; i < bodies.length; i++) {
      const b = bodies[i];
      if (b.isDestroyed) continue;

      const dx = x - b.position.x;
      const dy = y - b.position.y;
      const dist = Math.sqrt(dx * dx + dy * dy);
      warp -= (this.G * b.mass) / (dist + cSq);
    }

    return warp;
  }
}
