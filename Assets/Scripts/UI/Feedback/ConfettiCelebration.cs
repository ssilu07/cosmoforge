// ============================================================================
// CosmoForge: Victory Confetti Celebration Controller (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F22
// ============================================================================

namespace CosmoForge.UI.Feedback
{
    using UnityEngine;

    /// <summary>
    /// Spawns vibrant celebration confetti particles and sound when
    /// an orbital puzzle mission is completed or achievement unlocked.
    /// </summary>
    public class ConfettiCelebration : MonoBehaviour
    {
        [Header("Celebration FX")]
        [SerializeField] private ParticleSystem confettiParticleSystem;
        [SerializeField] private AudioSource victoryAudioSource;

        private void Awake()
        {
            if (confettiParticleSystem == null) confettiParticleSystem = GetComponent<ParticleSystem>();
            if (victoryAudioSource == null) victoryAudioSource = GetComponent<AudioSource>();
        }

        public void TriggerCelebration()
        {
            if (confettiParticleSystem != null)
            {
                confettiParticleSystem.Play();
            }

            if (victoryAudioSource != null)
            {
                victoryAudioSource.Play();
            }
        }
    }
}
