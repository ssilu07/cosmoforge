// ============================================================================
// CosmoForge: Celestial Body State
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Celestial.Data
{
    using System;
    using CosmoForge.Core.Math;

    /// <summary>
    /// Pure physics representation of a celestial body in the N-body simulation.
    /// Tracks double-precision coordinates, mass, velocity, acceleration, and state.
    /// </summary>
    [Serializable]
    public class CelestialBodyState
    {
        public int Id;
        public string Name;
        public BodyType Type;
        public double Mass;
        public double Radius;
        public Vector3d Position;
        public Vector3d Velocity;
        public Vector3d Acceleration;
        public double SurfaceTemperature;
        public double CoreFuel; // 0.0 - 1.0 for stars
        public bool IsDestroyed;

        // Auxiliary physics and rendering attributes
        public bool IsStatic;
        public Vector3d PreviousPosition;
        public Vector3d RenderPosition;

        // Lowercase alias properties for fixture and test compatibility
        public int id { get => Id; set => Id = value; }
        public string name { get => Name; set => Name = value; }
        public string displayName { get => Name; set => Name = value; }
        public BodyType type { get => Type; set => Type = value; }
        public double mass { get => Mass; set => Mass = value; }
        public double radius { get => Radius; set => Radius = value; }
        public Vector3d position { get => Position; set => Position = value; }
        public Vector3d velocity { get => Velocity; set => Velocity = value; }
        public Vector3d acceleration { get => Acceleration; set => Acceleration = value; }

        public CelestialBodyState()
        {
            Id = 0;
            Name = "Unnamed";
            Type = BodyType.Terrestrial;
            Mass = 1.0;
            Radius = 1.0;
            Position = Vector3d.zero;
            PreviousPosition = Vector3d.zero;
            RenderPosition = Vector3d.zero;
            Velocity = Vector3d.zero;
            Acceleration = Vector3d.zero;
            SurfaceTemperature = 288.0;
            CoreFuel = 1.0;
            IsDestroyed = false;
            IsStatic = false;
        }

        public CelestialBodyState(int id, string name, BodyType type, double mass, double radius, Vector3d position, Vector3d velocity)
        {
            Id = id;
            Name = name;
            Type = type;
            Mass = mass;
            Radius = radius;
            Position = position;
            PreviousPosition = position;
            RenderPosition = position;
            Velocity = velocity;
            Acceleration = Vector3d.zero;
            SurfaceTemperature = 288.0;
            CoreFuel = 1.0;
            IsDestroyed = false;
            IsStatic = false;
        }

        /// <summary>
        /// Creates a deep clone of the body state for trajectory lookahead without altering world state.
        /// </summary>
        public CelestialBodyState Clone()
        {
            return new CelestialBodyState
            {
                Id = this.Id,
                Name = this.Name,
                Type = this.Type,
                Mass = this.Mass,
                Radius = this.Radius,
                Position = this.Position,
                PreviousPosition = this.PreviousPosition,
                RenderPosition = this.RenderPosition,
                Velocity = this.Velocity,
                Acceleration = this.Acceleration,
                SurfaceTemperature = this.SurfaceTemperature,
                CoreFuel = this.CoreFuel,
                IsDestroyed = this.IsDestroyed,
                IsStatic = this.IsStatic
            };
        }
    }
}
