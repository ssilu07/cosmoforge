// ============================================================================
// CosmoForge: Cataclysm & Collision Engine Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Physics.Cataclysms
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;

    public struct CollisionEvent
    {
        public CelestialBodyState Primary;
        public CelestialBodyState Secondary;
        public Vector3d ImpactPoint;
        public Vector3d MergedVelocity;
        public double ConservedMass;
        public double DissipatedKineticEnergy;
        public double MergedRadius;
        public bool CatastrophicFragmentation;
    }

    public struct RocheDisruptionEvent
    {
        public CelestialBodyState Primary;
        public CelestialBodyState DisruptedMoon;
        public double RocheDistance;
        public int SpawnedRingParticles;
    }

    public interface ICollisionEngine
    {
        void CheckAndResolveCollisions(List<CelestialBodyState> bodies, double deltaTime, Action<CollisionEvent> onMerged);
        void EvaluateRocheLimits(List<CelestialBodyState> bodies, Action<RocheDisruptionEvent> onDisrupted);
    }
}
