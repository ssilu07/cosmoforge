// ============================================================================
// CosmoForge: Gamified Puzzle Engine & Mission Manager (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F22
// ============================================================================

namespace CosmoForge.Puzzles.Core
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using CosmoForge.Core.Performance;

    /// <summary>
    /// Coordinates mission lifecycle, real-time tick evaluation, objective win/loss
    /// state determination, star achievements (1-3 stars), and victory events.
    /// </summary>
    public class PuzzleEngine : MonoBehaviour
    {
        private SimulationWorld simulationWorld;

        public void BindSimulationWorld(SimulationWorld world)
        {
            simulationWorld = world;
        }

        private readonly List<IPuzzleMission> _missions = new List<IPuzzleMission>();
        private IPuzzleMission _activeMission;
        private int _activeMissionIndex = -1;

        public event Action<IPuzzleMission> OnMissionLoaded;
        public event Action<PuzzleEvaluationResult> OnEvaluationTick;
        public event Action<IPuzzleMission, int> OnMissionCompleted; // (mission, starsAwarded)

        public IReadOnlyList<IPuzzleMission> Missions => _missions;
        public IPuzzleMission ActiveMission => _activeMission;
        public int ActiveMissionIndex => _activeMissionIndex;

        public void RegisterMission(IPuzzleMission mission)
        {
            if (mission != null && !_missions.Contains(mission))
            {
                _missions.Add(mission);
            }
        }

        public void LoadMission(int index)
        {
            if (index < 0 || index >= _missions.Count || simulationWorld == null) return;

            _activeMissionIndex = index;
            _activeMission = _missions[index];
            _activeMission.Initialize(simulationWorld);
            OnMissionLoaded?.Invoke(_activeMission);
        }

        public void ResetActiveMission()
        {
            if (_activeMission != null && simulationWorld != null)
            {
                _activeMission.ResetMission(simulationWorld);
                OnMissionLoaded?.Invoke(_activeMission);
            }
        }

        public void StepTick(double deltaTime)
        {
            if (_activeMission == null || simulationWorld == null) return;

            PuzzleEvaluationResult result = _activeMission.EvaluateTick(simulationWorld, deltaTime);
            OnEvaluationTick?.Invoke(result);

            if (result.Status == PuzzleStatus.Victory)
            {
                OnMissionCompleted?.Invoke(_activeMission, result.StarsAwarded);
            }
        }
    }
}
