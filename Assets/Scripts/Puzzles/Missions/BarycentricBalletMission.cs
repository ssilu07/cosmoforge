// ============================================================================
// CosmoForge: Puzzle Mission 4 - "Barycentric Ballet"
// Conforms to PROJECT.md § Architecture & Requirement F20
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
    /// Level 4 Mission: Place two massive stars in orbit around their mutual center of mass
    /// (barycenter) and find a stable circumbinary orbit for an inhabited planet.
    /// </summary>
    public class BarycentricBalletMission : IPuzzleMission
    {
        public string MissionId => "m4_barycentric_ballet";
        public string DisplayTitle => "Barycentric Ballet";
        public string Description => "Maintain a binary star system and place an inhabited planet into a stable circumbinary orbit.";

        public double MinCircumbinaryRadius = 50.0;
        public double MaxCircumbinaryRadius = 180.0;
        public double RequiredStabilityDuration = 8.0;

        private double _elapsedTime;

        public void Initialize(SimulationWorld world)
        {
            _elapsedTime = 0.0;
            world.ClearBodies();

            double G = 1.0;
            double m1 = 1500.0;
            double m2 = 1200.0;
            double totalM = m1 + m2;
            double separation = 25.0;

            double r1 = -(m2 / totalM) * separation;
            double r2 = (m1 / totalM) * separation;
            double vRel = Math.Sqrt(G * totalM / separation);
            double v1 = -(m2 / totalM) * vRel;
            double v2 = (m1 / totalM) * vRel;

            var starA = new CelestialBodyState(1, "Alpha Centauri A", BodyType.Star, m1, 5.0, new Vector3d(r1, 0, 0), new Vector3d(0, v1, 0));
            var starB = new CelestialBodyState(2, "Alpha Centauri B", BodyType.Star, m2, 4.2, new Vector3d(r2, 0, 0), new Vector3d(0, v2, 0));

            world.AddBody(starA);
            world.AddBody(starB);
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            if (world.Bodies == null || world.Bodies.Count < 3)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, 0f, "Launch the habitable planet into circumbinary orbit.");
            }

            var starA = world.Bodies[0];
            var starB = world.Bodies[1];
            var planet = world.Bodies[2];

            if (planet.IsDestroyed)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Planet collided with one of the binary stars!");
            }

            // Barycenter calculation
            Vector3d barycenter = (starA.Position * starA.Mass + starB.Position * starB.Mass) / (starA.Mass + starB.Mass);
            double distToBarycenter = (planet.Position - barycenter).magnitude;

            if (distToBarycenter < MinCircumbinaryRadius)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Planet entered chaotic non-circumbinary 3-body zone!");
            }

            if (distToBarycenter > MaxCircumbinaryRadius)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Planet escaped binary star system!");
            }

            _elapsedTime += deltaTime;
            float progress = Mathf.Clamp01((float)(_elapsedTime / RequiredStabilityDuration));

            if (_elapsedTime >= RequiredStabilityDuration)
            {
                int stars = 3;
                return new PuzzleEvaluationResult(PuzzleStatus.Victory, stars, 1.0f, "Circumbinary orbital stability maintained!");
            }

            return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, progress, $"Ballet stable... Progress: {progress * 100:F0}%");
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }
}
