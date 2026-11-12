// ============================================================================
// CosmoForge: Tactile 3D Cosmic Icon Controller (MonoBehaviour)
// Conforms to User Specification: Real 3D Icons & Tactile Mobile Touch Feedback
// ============================================================================

namespace CosmoForge.UI.Workbench
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Renders a real-time rotating tactile 3D celestial icon for HUD & drawers.
    /// Provides tactile squash-and-stretch spring animations on touch/pointer down,
    /// glowing selection aura, and haptic feedback.
    /// </summary>
    public class Tactile3DIcon : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Celestial Configuration")]
        [SerializeField] private BodyType bodyType = BodyType.Terrestrial;
        [SerializeField] private double presetMass = 1.0;
        [SerializeField] private double presetRadius = 1.0;
        [SerializeField] private Color auraColor = new Color(0f, 0.94f, 1f, 0.8f);

        [Header("3D Visuals & Rotation")]
        [SerializeField] private Transform iconModelTransform;
        [SerializeField] private float rotationSpeed = 25f;
        [SerializeField] private MeshRenderer iconRenderer;

        [Header("Tactile Spring Feedback")]
        [SerializeField] private float pressScale = 0.82f;
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float springSpeed = 16f;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private bool _isPressed = false;
        private bool _isHovered = false;

        public BodyType BodyType => bodyType;
        public double PresetMass => presetMass;
        public double PresetRadius => presetRadius;

        private void Awake()
        {
            if (iconModelTransform == null) iconModelTransform = transform;
            _originalScale = iconModelTransform.localScale;
            _targetScale = _originalScale;
        }

        private void Update()
        {
            // Continuous cosmic axial spin
            if (iconModelTransform != null)
            {
                iconModelTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);

                // Smooth spring scale interpolation for tactile feeling
                iconModelTransform.localScale = Vector3.Lerp(iconModelTransform.localScale, _targetScale, Time.deltaTime * springSpeed);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _targetScale = _originalScale * pressScale;

            // Trigger haptic vibration on Android mobile
            #if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
            #endif
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _isHovered ? _originalScale * hoverScale : _originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (!_isPressed)
            {
                _targetScale = _originalScale * hoverScale;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isPressed)
            {
                _targetScale = _originalScale;
            }
        }
    }
}
