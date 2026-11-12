// ============================================================================
// CosmoForge: Puzzle Mission 1 - "The Orbital Injection"
// Conforms to PROJECT.md § Architecture & Requirement F17
// ============================================================================

namespace CosmoForge.Puzzles.Missions
{
    using System;
    using UnityEngine;
    using CosmoForge.Core.Math;
    using CosmoForge.Core.Performance;
    using CosmoForge.Celestial.Data;
    using CosmoForge.Puzzles.Core;

    /// <summary>
    /// Level 1 Mission: Slingshot a space probe into a stable circular orbit around Earth
    /// without crashing into the atmosphere or escaping into deep space.
    /// </summary>
    public class OrbitalInjectionMission : IPuzzleMission
    {
        public string MissionId => "m1_orbital_injection";
        public string DisplayTitle => "The Orbital Injection";
        public string Description => "Slingshot a space probe into a stable circular orbit around Earth. Maintain orbit without crashing or escaping.";

        public double TargetEccentricityThreshold = 0.05;
        public double MinOrbitalRadius = 15.0;
        public double MaxOrbitalRadius = 150.0;
        public double RequiredSurvivingTime = 5.0;

        private double _elapsedTime;

        public void Initialize(SimulationWorld world)
        {
            _elapsedTime = 0.0;
            world.ClearBodies();

            // Earth at center
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 1000.0, 10.0, Vector3d.zero, Vector3d.zero)
            {
                SurfaceTemperature = 288.0
            };
            world.AddBody(earth);
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            if (world.Bodies == null || world.Bodies.Count < 2)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, 0f, "Launch the space probe to establish orbit.");
            }

            CelestialBodyState earth = world.Bodies[0];
            CelestialBodyState probe = world.Bodies[1];

            if (probe.IsDestroyed)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Probe was destroyed by atmospheric or surface impact!");
            }

            Vector3d r = probe.Position - earth.Position;
            double dist = r.magnitude;

            if (dist <= earth.Radius)
            {
                probe.IsDestroyed = true;
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Probe crashed into Earth's surface!");
            }

            if (dist > MaxOrbitalRadius)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Probe escaped Earth's gravitational influence into deep space!");
            }

            _elapsedTime += deltaTime;
            float progress = Mathf.Clamp01((float)(_elapsedTime / RequiredSurvivingTime));

            // Calculate orbital eccentricity
            double mu = world.Integrator.GravitationalConstant * (earth.Mass + probe.Mass);
            Vector3d v = probe.Velocity - earth.Velocity;
            Vector3d h = Vector3d.Cross(r, v);
            Vector3d eVec = (Vector3d.Cross(v, h) / mu) - (r / dist);
            double ecc = eVec.magnitude;

            if (_elapsedTime >= RequiredSurvivingTime)
            {
                int stars = 1;
                if (ecc <= TargetEccentricityThreshold) stars = 3;
                else if (ecc <= 0.15) stars = 2;

                return new PuzzleEvaluationResult(
                    PuzzleStatus.Victory,
                    stars,
                    1.0f,
                    $"Stable orbit achieved! Eccentricity: {ecc:F3}. Stars awarded: {stars}"
                );
            }

            return new PuzzleEvaluationResult(
                PuzzleStatus.InProgress,
                0,
                progress,
                $"Holding stable orbit... Eccentricity: {ecc:F3}. Progress: {progress * 100:F0}%"
            );
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }
}
