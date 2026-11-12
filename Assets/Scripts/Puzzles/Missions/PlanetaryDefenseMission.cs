// ============================================================================
// CosmoForge: Puzzle Mission 3 - "Planetary Defense"
// Conforms to PROJECT.md § Architecture & Requirement F19
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
    /// Level 3 Mission: An extinction-level asteroid is on a collision course with Earth.
    /// Deploy a gravity-tractor probe to alter its velocity vector just enough to cause a safe flyby.
    /// </summary>
    public class PlanetaryDefenseMission : IPuzzleMission
    {
        public string MissionId => "m3_planetary_defense";
        public string DisplayTitle => "Planetary Defense";
        public string Description => "Extinction asteroid on collision course! Deploy a gravity tractor to alter its velocity for a safe flyby.";

        public double SafeFlybyDistance = 35.0;
        private double _simulationDuration = 10.0;
        private double _elapsedTime;

        public void Initialize(SimulationWorld world)
        {
            _elapsedTime = 0.0;
            world.ClearBodies();

            // Earth
            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, 2000.0, 8.0, Vector3d.zero, Vector3d.zero);

            // Asteroid incoming directly toward Earth from x = 120, y = 1.0 (slight impact trajectory)
            var asteroid = new CelestialBodyState(2, "Asteroid", BodyType.AsteroidProbe, 20.0, 1.5, new Vector3d(120.0, 1.0, 0.0), new Vector3d(-12.0, 0.0, 0.0));

            world.AddBody(earth);
            world.AddBody(asteroid);
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            if (world.Bodies == null || world.Bodies.Count < 2)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Missing planetary or asteroid body.");
            }

            var earth = world.Bodies[0];
            var asteroid = world.Bodies[1];

            if (asteroid.IsDestroyed)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Asteroid impacted Earth! Extinction event occurred!");
            }

            Vector3d rel = asteroid.Position - earth.Position;
            double dist = rel.magnitude;

            if (dist <= earth.Radius + asteroid.Radius)
            {
                asteroid.IsDestroyed = true;
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Impact confirmed! Asteroid devastated Earth!");
            }

            _elapsedTime += deltaTime;
            float progress = Mathf.Clamp01((float)(_elapsedTime / _simulationDuration));

            // Asteroid has safely flown by if it has passed Earth (x < -20) and distance exceeds safe flyby distance
            if (asteroid.Position.x < -20.0)
            {
                if (dist >= SafeFlybyDistance)
                {
                    int stars = dist >= SafeFlybyDistance * 1.5 ? 3 : 2;
                    return new PuzzleEvaluationResult(PuzzleStatus.Victory, stars, 1.0f, $"Earth Saved! Asteroid deflected to safe distance of {dist:F1} units!");
                }
                else if (dist > earth.Radius + asteroid.Radius)
                {
                    return new PuzzleEvaluationResult(PuzzleStatus.Victory, 1, 1.0f, $"Close shave! Asteroid passed within {dist:F1} units.");
                }
            }

            return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, progress, $"Asteroid inbound... Distance: {dist:F1} units.");
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }
}
