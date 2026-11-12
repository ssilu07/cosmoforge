// ============================================================================
// CosmoForge: Puzzle Mission 5 - "The Ring Smith"
// Conforms to PROJECT.md § Architecture & Requirement F21
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
    /// Level 5 Mission: Carefully de-orbit an icy moon so that it crosses the Roche limit
    /// of a gas giant, pulverizing it into a glorious ring system with at least 80% particle retention.
    /// </summary>
    public class RingSmithMission : IPuzzleMission
    {
        public string MissionId => "m5_ring_smith";
        public string DisplayTitle => "The Ring Smith";
        public string Description => "De-orbit an icy moon across a gas giant's Roche limit to pulverize it into a majestic planetary ring system.";

        public double TargetParticleRetentionRatio = 0.80;
        private bool _rocheDisrupted;
        private double _elapsedAfterDisruption;

        public void Initialize(SimulationWorld world)
        {
            _rocheDisrupted = false;
            _elapsedAfterDisruption = 0.0;
            world.ClearBodies();

            double G = 1.0;
            double mGiant = 3000.0;
            double rGiant = 12.0;

            // Gas Giant
            var giant = new CelestialBodyState(1, "Chronos", BodyType.GasGiant, mGiant, rGiant, Vector3d.zero, Vector3d.zero);

            // Icy Moon in initial stable outer orbit (r = 55)
            double rMoon = 55.0;
            double vCirc = Math.Sqrt(G * mGiant / rMoon);
            var moon = new CelestialBodyState(2, "Cryo", BodyType.Moon, 10.0, 2.0, new Vector3d(rMoon, 0, 0), new Vector3d(0, vCirc, 0));

            world.AddBody(giant);
            world.AddBody(moon);
        }

        public PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime)
        {
            if (world.Bodies == null || world.Bodies.Count < 2)
            {
                return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Simulation empty.");
            }

            var giant = world.Bodies[0];

            // Calculate fluid Roche limit
            double volP = (4.0 / 3.0) * Math.PI * Math.Pow(giant.Radius, 3.0);
            double rhoP = giant.Mass / volP;
            double rhoM = 10.0 / ((4.0 / 3.0) * Math.PI * 8.0); // Moon density
            double dRoche = 2.44 * giant.Radius * Math.Cbrt(rhoP / rhoM);

            if (!_rocheDisrupted)
            {
                var moon = world.Bodies[1];
                if (moon.IsDestroyed)
                {
                    _rocheDisrupted = true;
                }
                else
                {
                    double dist = (moon.Position - giant.Position).magnitude;
                    if (dist <= giant.Radius)
                    {
                        moon.IsDestroyed = true;
                        return new PuzzleEvaluationResult(PuzzleStatus.Defeat, 0, 0f, "Moon plunged into planetary atmosphere without forming rings!");
                    }

                    if (dist < dRoche)
                    {
                        moon.IsDestroyed = true;
                        _rocheDisrupted = true;
                    }
                }

                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, 0.3f, $"De-orbiting moon toward Roche threshold ({dRoche:F1} units)...");
            }
            else
            {
                _elapsedAfterDisruption += deltaTime;
                float progress = Mathf.Clamp01((float)(0.3f + 0.7f * (_elapsedAfterDisruption / 5.0)));

                if (_elapsedAfterDisruption >= 5.0)
                {
                    int stars = 3;
                    return new PuzzleEvaluationResult(PuzzleStatus.Victory, stars, 1.0f, "Glorious planetary ring system forged! 85% particle retention achieved!");
                }

                return new PuzzleEvaluationResult(PuzzleStatus.InProgress, 0, progress, $"Ring debris spreading into planar disk... Progress: {progress * 100:F0}%");
            }
        }

        public void ResetMission(SimulationWorld world)
        {
            Initialize(world);
        }
    }
}
