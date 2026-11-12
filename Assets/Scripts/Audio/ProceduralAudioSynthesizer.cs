// ============================================================================
// CosmoForge: Pure Procedural Cosmic Audio DSP (MonoBehaviour)
// Conforms to PROJECT.md § Interface Contracts & Requirement F28
// ============================================================================

namespace CosmoForge.Audio
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Pure C# procedural audio DSP synthesizer generating spacetime sub-bass hums,
    /// pulsar metronomic clicks, thruster launch bursts, and supernova cataclysm blasts
    /// directly into the Unity audio stream via OnAudioFilterRead without external MP3 assets.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudioSynthesizer : MonoBehaviour, IProceduralAudioEngine
    {
        [Header("Master Controls")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.8f;

        [Header("Live Synthesis Parameters")]
        [Tooltip("Active metric curvature warp for spacetime hum.")]
        [SerializeField] private double activeMetricWarp = 0.0;

        [Tooltip("Active pulsar spin frequency in Hz.")]
        [SerializeField] private double activePulsarFrequency = 12.0;

        private double _sampleRate = 48000.0;
        private double _humPhase = 0.0;
        private double _pulsarPhase = 0.0;
        private double _launchElapsed = 100.0; // Inactive by default
        private double _supernovaElapsed = 100.0;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public double SampleRate
        {
            get => _sampleRate;
            set => _sampleRate = Math.Max(1.0, value);
        }

        public double ActiveMetricWarp
        {
            get => activeMetricWarp;
            set => activeMetricWarp = value;
        }

        public double ActivePulsarFrequency
        {
            get => activePulsarFrequency;
            set => activePulsarFrequency = value;
        }

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000.0;
        }

        public double GetSpacetimeHumFrequency(double metricWarp)
        {
            double baseFreq = 55.0;
            return baseFreq * (1.0 - 0.35 * Math.Tanh(Math.Abs(metricWarp) / 20.0));
        }

        public double GetPulsarImpulseInterval(double spinFrequencyHz)
        {
            double nyquist = _sampleRate * 0.5;
            double clamped = Math.Min(spinFrequencyHz, nyquist - 100.0);
            return _sampleRate / Math.Max(1.0, clamped);
        }

        public double GetLaunchBurstFrequency(double elapsedSeconds)
        {
            return 180.0 * Math.Exp(-elapsedSeconds / 0.045) + 42.0;
        }

        public double GetLaunchBurstAmplitude(double elapsedSeconds)
        {
            return Math.Exp(-elapsedSeconds / 0.12);
        }

        public double GetSupernovaBlastPitch(double elapsedSeconds)
        {
            return Math.Max(24.0, 95.0 - (71.0 * (elapsedSeconds / 2.5)));
        }

        public double GetSupernovaEnvelope(double elapsedSeconds, double tau)
        {
            return Math.Exp(-elapsedSeconds / tau);
        }

        public void TriggerLaunchSound()
        {
            _launchElapsed = 0.0;
        }

        public void TriggerSupernovaSound()
        {
            _supernovaElapsed = 0.0;
        }

        public void FillAudioBuffer(float[] buffer, AudioEventType eventType, double parameter)
        {
            if (buffer == null || buffer.Length == 0) return;

            if (masterVolume <= 0.0f)
            {
                Array.Clear(buffer, 0, buffer.Length);
                return;
            }

            float phase = 0f;
            float phaseIncrement = (float)((2.0 * Math.PI * 440.0) / _sampleRate);

            switch (eventType)
            {
                case AudioEventType.SpacetimeHum:
                    double humFreq = GetSpacetimeHumFrequency(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI * humFreq) / _sampleRate);
                    break;
                case AudioEventType.SupernovaBlast:
                    double snPitch = GetSupernovaBlastPitch(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI * snPitch) / _sampleRate);
                    break;
                case AudioEventType.PulsarClick:
                    double pInterval = GetPulsarImpulseInterval(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI) / pInterval);
                    break;
                case AudioEventType.LaunchBurst:
                    double lPitch = GetLaunchBurstFrequency(parameter);
                    phaseIncrement = (float)((2.0 * Math.PI * lPitch) / _sampleRate);
                    break;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                float raw = Mathf.Sin(phase) * 1.5f;
                float clipped = (2.0f / Mathf.PI) * Mathf.Atan(raw) * masterVolume;
                buffer[i] = Mathf.Clamp(clipped, -1.0f, 1.0f);
                phase += phaseIncrement;
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (masterVolume <= 0f) return;

            double dt = 1.0 / _sampleRate;
            double humFreq = GetSpacetimeHumFrequency(activeMetricWarp);
            double humInc = (2.0 * Math.PI * humFreq) * dt;

            for (int i = 0; i < data.Length; i += channels)
            {
                // 1. Spacetime sub-bass hum
                double sample = Math.Sin(_humPhase) * 0.25;
                _humPhase += humInc;
                if (_humPhase > 2.0 * Math.PI) _humPhase -= 2.0 * Math.PI;

                // 2. Launch sound transient
                if (_launchElapsed < 1.0)
                {
                    double lFreq = GetLaunchBurstFrequency(_launchElapsed);
                    double lAmp = GetLaunchBurstAmplitude(_launchElapsed);
                    sample += Math.Sin(_launchElapsed * 2.0 * Math.PI * lFreq) * lAmp * 0.4;
                    _launchElapsed += dt;
                }

                // 3. Supernova explosion
                if (_supernovaElapsed < 4.0)
                {
                    double snPitch = GetSupernovaBlastPitch(_supernovaElapsed);
                    double snAmp = GetSupernovaEnvelope(_supernovaElapsed, 1.2);
                    sample += Math.Sin(_supernovaElapsed * 2.0 * Math.PI * snPitch) * snAmp * 0.6;
                    _supernovaElapsed += dt;
                }

                // 4. Pulsar periodic acoustic clicks
                if (activePulsarFrequency > 0.0)
                {
                    double pInterval = GetPulsarImpulseInterval(activePulsarFrequency);
                    _pulsarPhase += 1.0;
                    if (_pulsarPhase >= pInterval)
                    {
                        sample += 0.3;
                        _pulsarPhase -= pInterval;
                    }
                }

                // Soft-clipping saturation
                float outVal = (float)((2.0 / Math.PI) * Math.Atan(sample * 1.5) * masterVolume);

                for (int c = 0; c < channels; c++)
                {
                    data[i + c] += outVal;
                }
            }
        }
    }
}
