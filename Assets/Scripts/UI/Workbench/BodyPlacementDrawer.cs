// ============================================================================
// CosmoForge: Celestial Body Placement Drawer (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F25
// ============================================================================

namespace CosmoForge.UI.Workbench
{
    using UnityEngine;
    using UnityEngine.UI;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Celestial body catalog drawer allowing selection from Asteroids, Moons,
    /// Terrestrial Planets, Gas Giants, Stars, Pulsars, and Black Holes for slingshot placement.
    /// </summary>
    public class BodyPlacementDrawer : MonoBehaviour
    {
        [Header("Slingshot Controller Reference")]
        [SerializeField] private SlingshotController slingshotController;

        [Header("Catalog Presets")]
        [SerializeField] private Button asteroidBtn;
        [SerializeField] private Button moonBtn;
        [SerializeField] private Button earthBtn;
        [SerializeField] private Button gasGiantBtn;
        [SerializeField] private Button sunBtn;
        [SerializeField] private Button blueHypergiantBtn;
        [SerializeField] private Button pulsarBtn;
        [SerializeField] private Button blackHoleBtn;

        [Header("Tactile Audio Reference")]
        [SerializeField] private AudioSource clickAudioSource;

        private void Start()
        {
            if (asteroidBtn != null) asteroidBtn.onClick.AddListener(() => SelectPreset(BodyType.AsteroidProbe, 0.001, 0.3));
            if (moonBtn != null) moonBtn.onClick.AddListener(() => SelectPreset(BodyType.Moon, 0.012, 0.5));
            if (earthBtn != null) earthBtn.onClick.AddListener(() => SelectPreset(BodyType.Terrestrial, 1.0, 1.0));
            if (gasGiantBtn != null) gasGiantBtn.onClick.AddListener(() => SelectPreset(BodyType.GasGiant, 318.0, 4.0));
            if (sunBtn != null) sunBtn.onClick.AddListener(() => SelectPreset(BodyType.Star, 1000.0, 8.0));
            if (blueHypergiantBtn != null) blueHypergiantBtn.onClick.AddListener(() => SelectPreset(BodyType.Star, 25000.0, 20.0));
            if (pulsarBtn != null) pulsarBtn.onClick.AddListener(() => SelectPreset(BodyType.Pulsar, 1800.0, 1.2));
            if (blackHoleBtn != null) blackHoleBtn.onClick.AddListener(() => SelectPreset(BodyType.BlackHole, 50000.0, 3.0));
        }

        public void SelectPreset(BodyType type, double mass, double radius)
        {
            if (slingshotController != null)
            {
                slingshotController.SetPendingBody(type, mass, radius);
            }

            if (clickAudioSource != null)
            {
                clickAudioSource.Play();
            }
        }
    }
}
