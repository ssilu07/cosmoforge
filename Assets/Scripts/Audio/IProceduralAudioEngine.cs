// ============================================================================
// CosmoForge: Procedural Audio Engine & Audio Event Type Contract
// Conforms to PROJECT.md § Interface Contracts
// ============================================================================

namespace CosmoForge.Audio
{
    public enum AudioEventType
    {
        SpacetimeHum,
        PulsarClick,
        LaunchBurst,
        SupernovaBlast
    }

    public interface IProceduralAudioEngine
    {
        float MasterVolume { get; set; }
        double SampleRate { get; set; }

        double GetSpacetimeHumFrequency(double metricWarp);
        double GetPulsarImpulseInterval(double spinFrequencyHz);
        double GetLaunchBurstFrequency(double elapsedSeconds);
        double GetLaunchBurstAmplitude(double elapsedSeconds);
        double GetSupernovaBlastPitch(double elapsedSeconds);
        double GetSupernovaEnvelope(double elapsedSeconds, double tau);

        void FillAudioBuffer(float[] buffer, AudioEventType eventType, double parameter);
    }
}
