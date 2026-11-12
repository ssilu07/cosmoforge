// ============================================================================
// CosmoForge: Simulation World & 60 FPS Mobile Sub-Stepping Loop
// Conforms to PROJECT.md § Architecture & Survey E1
// ============================================================================

namespace CosmoForge.Core.Performance
{
    using System;
    using System.Collections.Generic;
    using CosmoForge.Core.Math;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Physics.Integrator;

    /// <summary>
    /// Orchestrates celestial simulation execution, time-warp sub-stepping,
    /// and render state interpolation to deliver smooth 60 FPS visual output.
    /// </summary>
    public class SimulationWorld
    {
        public List<CelestialBodyState> Bodies { get; set; } = new List<CelestialBodyState>();
        public IGravityIntegrator Integrator { get; set; }
        public bool IsPaused { get; set; } = false;
        public double TimeWarpMultiplier { get; set; } = 1.0;

        private double _accumulator = 0.0;
        public const double FixedDeltaTime = 0.02; // 50 Hz physics tick
        public const int MaxSubsteps = 25; // Clamps lag spikes to avoid mobile spiral of death

        public SimulationWorld()
        {
            Bodies = new List<CelestialBodyState>();
            Integrator = new VelocityVerletIntegrator(1.0, 0.1);
        }

        public SimulationWorld(IGravityIntegrator integrator)
        {
            Bodies = new List<CelestialBodyState>();
            Integrator = integrator;
        }

        public void AddBody(CelestialBodyState body)
        {
            if (body != null && !Bodies.Contains(body))
            {
                Bodies.Add(body);
            }
        }

        public void RemoveBody(CelestialBodyState body)
        {
            if (body != null)
            {
                Bodies.Remove(body);
            }
        }

        public void ClearBodies()
        {
            Bodies.Clear();
        }

        /// <summary>
        /// Advances the simulation by discrete sub-steps decoupled from rendering frame rate.
        /// </summary>
        public void UpdateSimulation(double unscaledDeltaTime, double timeWarpMultiplier)
        {
            if (IsPaused || Integrator == null) return;

            TimeWarpMultiplier = timeWarpMultiplier;
            double effectiveDelta = unscaledDeltaTime * timeWarpMultiplier;
            _accumulator += effectiveDelta;

            int stepsTaken = 0;
            while (_accumulator >= FixedDeltaTime && stepsTaken < MaxSubsteps)
            {
                // Cache previous positions for state interpolation
                for (int i = 0; i < Bodies.Count; i++)
                {
                    Bodies[i].PreviousPosition = Bodies[i].Position;
                }

                // Execute Symplectic Integration step
                Integrator.Step(Bodies, FixedDeltaTime);

                _accumulator -= FixedDeltaTime;
                stepsTaken++;
            }

            // Hard clamp accumulator to avoid backlog spiral
            if (_accumulator > FixedDeltaTime * MaxSubsteps)
            {
                _accumulator = 0.0;
            }

            // Compute interpolation weight
            double alpha = Math.Clamp(_accumulator / FixedDeltaTime, 0.0, 1.0);
            InterpolateVisualPositions(alpha);
        }

        /// <summary>
        /// Linearly interpolates celestial body render coordinates between previous and current physics steps.
        /// </summary>
        public void InterpolateVisualPositions(double alpha)
        {
            for (int i = 0; i < Bodies.Count; i++)
            {
                CelestialBodyState b = Bodies[i];
                if (b == null || b.IsDestroyed) continue;
                b.RenderPosition = Vector3d.Lerp(b.PreviousPosition, b.Position, alpha);
            }
        }
    }
}
