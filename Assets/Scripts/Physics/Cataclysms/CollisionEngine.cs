// ============================================================================
// CosmoForge: Cataclysm & Inelastic Collision Engine
// Conforms to PROJECT.md § Interface Contracts & Requirements F12, F14
// ============================================================================

namespace CosmoForge.Physics.Cataclysms
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Production implementation of ICollisionEngine.
    /// Evaluates inelastic mergers conserving total linear momentum and calculates
    /// fluid Roche limit tidal disruptions into planetary ring systems.
    /// </summary>
    public class CollisionEngine : ICollisionEngine
    {
        public List<CollisionEvent> RecordedCollisions { get; } = new List<CollisionEvent>();
        public List<RocheDisruptionEvent> RecordedRocheEvents { get; } = new List<RocheDisruptionEvent>();

        /// <summary>
        /// Detects physical contact collisions between celestial bodies, executing
        /// a perfectly inelastic merger with strict linear momentum conservation:
        /// v_merged = (m1 * v1 + m2 * v2) / (m1 + m2)
        /// </summary>
        public void CheckAndResolveCollisions(List<CelestialBodyState> bodies, double deltaTime, Action<CollisionEvent> onMerged)
        {
            if (bodies == null || bodies.Count < 2) return;

            for (int i = 0; i < bodies.Count; i++)
            {
                var b1 = bodies[i];
                if (b1 == null || b1.IsDestroyed) continue;

                for (int j = i + 1; j < bodies.Count; j++)
                {
                    var b2 = bodies[j];
                    if (b2 == null || b2.IsDestroyed) continue;

                    Vector3d relPos = b2.Position - b1.Position;
                    double dist = relPos.magnitude;
                    double contactDist = b1.Radius + b2.Radius;

                    if (dist <= contactDist)
                    {
                        // Determine primary (more massive) vs secondary
                        CelestialBodyState primary = b1.Mass >= b2.Mass ? b1 : b2;
                        CelestialBodyState secondary = b1.Mass >= b2.Mass ? b2 : b1;

                        double m1 = primary.Mass;
                        double m2 = secondary.Mass;
                        double totalM = m1 + m2;

                        if (totalM <= 0.0) continue;

                        Vector3d p1 = primary.Velocity * m1;
                        Vector3d p2 = secondary.Velocity * m2;
                        Vector3d vMerged = (p1 + p2) / totalM;

                        // Kinetic energy before vs after merger
                        double keBefore = 0.5 * m1 * primary.Velocity.sqrMagnitude + 0.5 * m2 * secondary.Velocity.sqrMagnitude;
                        double keAfter = 0.5 * totalM * vMerged.sqrMagnitude;
                        double dissipatedKE = Math.Max(0.0, keBefore - keAfter);

                        // Volumetric radius accumulation: V_new = V1 + V2 -> R_new = (R1^3 + R2^3)^(1/3)
                        double mergedRadius = Math.Cbrt(Math.Pow(primary.Radius, 3.0) + Math.Pow(secondary.Radius, 3.0));
                        Vector3d impactPoint = primary.Position + (relPos.normalized * primary.Radius);

                        // Apply merger to primary body
                        primary.Mass = totalM;
                        primary.Radius = mergedRadius;
                        primary.Velocity = vMerged;

                        // Destroy secondary
                        secondary.IsDestroyed = true;

                        var collisionEvt = new CollisionEvent
                        {
                            Primary = primary,
                            Secondary = secondary,
                            ImpactPoint = impactPoint,
                            MergedVelocity = vMerged,
                            ConservedMass = totalM,
                            DissipatedKineticEnergy = dissipatedKE,
                            MergedRadius = mergedRadius,
                            CatastrophicFragmentation = dissipatedKE > 1e6
                        };

                        RecordedCollisions.Add(collisionEvt);
                        onMerged?.Invoke(collisionEvt);
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates fluid Roche limit tidal disruption between massive primaries and moons:
        /// d_Roche = 2.44 * R_M * (rho_M / rho_m)^(1/3)
        /// If distance &lt; d_Roche, the moon shatters into a planar planetary ring.
        /// </summary>
        public void EvaluateRocheLimits(List<CelestialBodyState> bodies, Action<RocheDisruptionEvent> onDisrupted)
        {
            if (bodies == null || bodies.Count < 2) return;

            for (int i = 0; i < bodies.Count; i++)
            {
                var primary = bodies[i];
                if (primary == null || primary.IsDestroyed || primary.Radius <= 0.0) continue;

                for (int j = 0; j < bodies.Count; j++)
                {
                    if (i == j) continue;
                    var moon = bodies[j];
                    if (moon == null || moon.IsDestroyed || moon.Radius <= 0.0 || moon.Mass >= primary.Mass) continue;

                    double volP = (4.0 / 3.0) * Math.PI * Math.Pow(primary.Radius, 3.0);
                    double volM = (4.0 / 3.0) * Math.PI * Math.Pow(moon.Radius, 3.0);
                    if (volP <= 1e-15 || volM <= 1e-15) continue;

                    double rhoP = primary.Mass / volP;
                    double rhoM = moon.Mass / volM;
                    if (rhoM <= 1e-15) continue;

                    double dRoche = 2.44 * primary.Radius * Math.Cbrt(rhoP / rhoM);
                    double currentDist = (moon.Position - primary.Position).magnitude;

                    if (currentDist < dRoche && currentDist > primary.Radius)
                    {
                        moon.IsDestroyed = true;
                        int spawnedParticles = (int)Math.Clamp(moon.Mass * 10.0, 50.0, 500.0);

                        var evt = new RocheDisruptionEvent
                        {
                            Primary = primary,
                            DisruptedMoon = moon,
                            RocheDistance = dRoche,
                            SpawnedRingParticles = spawnedParticles
                        };

                        RecordedRocheEvents.Add(evt);
                        onDisrupted?.Invoke(evt);
                    }
                }
            }
        }
    }
}
