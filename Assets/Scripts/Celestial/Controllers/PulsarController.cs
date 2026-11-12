// ============================================================================
// CosmoForge: Pulsar & Synchrotron Radiation Controller (MonoBehaviour)
// Conforms to PROJECT.md § Architecture & Requirement F10
// ============================================================================

namespace CosmoForge.Celestial.Controllers
{
    using UnityEngine;
    using CosmoForge.Celestial.Data;

    /// <summary>
    /// Controls high-speed relativistic rotation, precessing magnetic dipole axis,
    /// and twin collimated synchrotron radiation beams of a Neutron Star / Pulsar.
    /// </summary>
    public class PulsarController : MonoBehaviour
    {
        [Header("Pulsar Dynamics")]
        [Tooltip("Rotational frequency in Hertz (spins per second).")]
        [Range(0.5f, 60.0f)]
        [SerializeField] private float spinFrequencyHz = 12.0f;

        [Tooltip("Magnetic dipole tilt angle in degrees.")]
        [Range(0f, 90f)]
        [SerializeField] private float magneticTiltAngle = 23.5f;

        [Header("Synchrotron Beams")]
        [SerializeField] private Transform northBeamTransform;
        [SerializeField] private Transform southBeamTransform;
        [SerializeField] private LineRenderer northBeamLine;
        [SerializeField] private LineRenderer southBeamLine;
        [SerializeField] private float beamLength = 80.0f;

        public float SpinFrequencyHz
        {
            get => spinFrequencyHz;
            set => spinFrequencyHz = Mathf.Max(0.1f, value);
        }

        public float MagneticTiltAngle
        {
            get => magneticTiltAngle;
            set => magneticTiltAngle = value;
        }

        private void Start()
        {
            SetupBeams();
        }

        private void SetupBeams()
        {
            if (northBeamLine != null)
            {
                northBeamLine.positionCount = 2;
                northBeamLine.SetPosition(0, Vector3.zero);
                northBeamLine.SetPosition(1, Vector3.up * beamLength);
            }
            if (southBeamLine != null)
            {
                southBeamLine.positionCount = 2;
                southBeamLine.SetPosition(0, Vector3.zero);
                southBeamLine.SetPosition(1, -Vector3.up * beamLength);
            }
        }

        private void Update()
        {
            // Rapid axial spin
            float spinDelta = spinFrequencyHz * 360f * Time.deltaTime;
            transform.Rotate(Vector3.up, spinDelta, Space.Self);

            // Precess magnetic dipole
            if (northBeamTransform != null)
            {
                northBeamTransform.localRotation = Quaternion.Euler(magneticTiltAngle, 0f, 0f);
            }
            if (southBeamTransform != null)
            {
                southBeamTransform.localRotation = Quaternion.Euler(-magneticTiltAngle, 180f, 0f);
            }
        }
    }
}
