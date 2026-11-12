// ============================================================================
// CosmoForge: Puzzle Evaluation Result & Status
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Puzzles.Core
{
    public enum PuzzleStatus
    {
        InProgress,
        Victory,
        Defeat
    }

    public struct PuzzleEvaluationResult
    {
        public PuzzleStatus Status;
        public int StarsAwarded; // 0, 1, 2, 3
        public float Progress;    // 0.0 - 1.0
        public string FeedbackMessage;

        public PuzzleEvaluationResult(PuzzleStatus status, int starsAwarded, float progress, string feedbackMessage)
        {
            Status = status;
            StarsAwarded = starsAwarded;
            Progress = progress;
            FeedbackMessage = feedbackMessage;
        }
    }
}
