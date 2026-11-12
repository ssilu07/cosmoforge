// ============================================================================
// CosmoForge: Puzzle Mission Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Puzzles.Core
{
    using CosmoForge.Core.Performance;

    public interface IPuzzleMission
    {
        string MissionId { get; }
        string DisplayTitle { get; }
        string Description { get; }
        void Initialize(SimulationWorld world);
        PuzzleEvaluationResult EvaluateTick(SimulationWorld world, double deltaTime);
        void ResetMission(SimulationWorld world);
    }
}
