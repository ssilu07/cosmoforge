// ============================================================================
// CosmoForge: Puzzle Mission 2 - "Lagrange Parking"
// Conforms to PROJECT.md § Architecture & Requirement F18
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
    /// Level 2 Mission: Position a space telescope into the unstable L1 or stable L4/L5
    /// gravitational equilibrium sweet spot of the Earth-Moon system.
    /// </summary>
    public class LagrangeParkingMission : IPuzzleMission
    {
        public string MissionId => "m2_lagrange_parking";
        public string DisplayTitle => "Lagrange Parking";
        public string Description => "Park a space telescope into the L1 or L4/L5 gravitational equilibrium sweet spot of the Earth-Moon system.";

        public double TargetToleranceRadius = 8.0;
        public double RequiredStationTime = 6.0;

        private double _elapsedTime;

        public void Initialize(SimulationWorld world)
        {
            _elapsedTime = 0.0;
            world.ClearBodies();

            double G = 1.0;
            double mEarth = 1000.0;
            double mMoon = 50.0;
            double totalM = mEarth + mMoon;
            double rTotal = 60.0;

            // Earth and Moon barycentric orbit
            double rEarth = -(mMoon / totalM) * rTotal;
            double rMoon = (mEarth / totalM) * rTotal;
            double vRel = Math.Sqrt(G * totalM / rTotal);
            double vEarth = -(mMoon / totalM) * vRel;
            double vMoon = (mEarth / totalM) * vRel;

            var earth = new CelestialBodyState(1, "Earth", BodyType.Terrestrial, mEarth, 6.0, new Vector3d(rEarth, 0.0, 0.0), new Vector3d(0.0, vEarth, 0.0));
            var moon = new CelestialBodyState(2, "Moon", BodyType.Moon, mMoon, 2.0, new Vector3d(rMoon, 0.0, 0.0), new Vector3d(0.0, vMoon, 0.0));

            world.AddBody(earth);
            world.AddBody(moon);
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            if (world.Bodies == null || world.Bodies.Count < 3)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, 0f, "Deploy telescope probe into Earth-Moon system.");
            }

            var earth = world.Bodies[0];
            var moon = world.Bodies[1];
            var telescope = world.Bodies[2];

            if (telescope.IsDestroyed)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Telescope collided with primary body!");
            }

            // Calculate L1 point: approximately along Earth-Moon axis between Earth and Moon
            Vector3d rEM = moon.Position - earth.Position;
            double dEM = rEM.magnitude;
            double gamma = Math.Cbrt(moon.Mass / (3.0 * earth.Mass));
            Vector3d l1Target = moon.Position - rEM.normalized * (dEM * gamma);

            // Calculate L4 point: equilateral triangle in orbital plane
            Vector3d l4Target = earth.Position + (Quaternion.Euler(0, 0, 60) * rEM.ToVector3()).ToVector3d();

            double distToL1 = (telescope.Position - l1Target).magnitude;
            double distToL4 = (telescope.Position - l4Target).magnitude;
            double bestDist = Math.Min(distToL1, distToL4);

            if (bestDist <= TargetToleranceRadius)
            {
                _elapsedTime += deltaTime;
                float progress = Mathf.Clamp01((float)(_elapsedTime / RequiredStationTime));

                if (_elapsedTime >= RequiredStationTime)
                {
                    int stars = bestDist <= TargetToleranceRadius * 0.4 ? 3 : (bestDist <= TargetToleranceRadius * 0.7 ? 2 : 1);
                    return new PuzzleEvaluationResult(PuzzleStatus.Victory, stars, 1.0f, $"Telescope safely parked at Lagrange point! Stars: {stars}");
                }

                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, progress, $"Station-keeping active at equilibrium point. Progress: {progress * 100:F0}%");
            }
            else
            {
                _elapsedTime = Math.Max(0.0, _elapsedTime - deltaTime * 0.5);
                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, 0f, $"Telescope distance to nearest equilibrium point: {bestDist:F1} units.");
            }
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }
}
