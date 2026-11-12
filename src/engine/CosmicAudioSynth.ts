// ============================================================================
// CosmoForge Web: Procedural Cosmic Audio Synthesizer (Web Audio API)
// Conforms to Technical Specifications Section 5: Sensory Juice & Audio
// ============================================================================

export class CosmicAudioSynth {
  private ctx: AudioContext | null = null;
  private masterGain: GainNode | null = null;
  private humOsc: OscillatorNode | null = null;
  private humGain: GainNode | null = null;
  private isMuted: boolean = false;

  public init(): void {
    if (this.ctx) return;

    try {
      const AudioCtxClass = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      this.ctx = new AudioCtxClass();

      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.setValueAtTime(0.7, this.ctx.currentTime);
      this.masterGain.connect(this.ctx.destination);

      // Deep 45Hz sub-bass spacetime hum
      this.humOsc = this.ctx.createOscillator();
      this.humOsc.type = 'sine';
      this.humOsc.frequency.setValueAtTime(45.0, this.ctx.currentTime);

      this.humGain = this.ctx.createGain();
      this.humGain.gain.setValueAtTime(0.15, this.ctx.currentTime);

      this.humOsc.connect(this.humGain);
      this.humGain.connect(this.masterGain);
      this.humOsc.start();
    } catch (e) {
      console.warn('Web Audio API not allowed or supported yet:', e);
    }
  }

  public resume(): void {
    if (this.ctx && this.ctx.state === 'suspended') {
      this.ctx.resume();
    }
  }

  public setMuted(muted: boolean): void {
    this.isMuted = muted;
    if (this.masterGain && this.ctx) {
      this.masterGain.gain.setValueAtTime(muted ? 0.0 : 0.7, this.ctx.currentTime);
    }
  }

  /**
   * Modulates spacetime hum frequency and amplitude based on metric curvature.
   */
  public updateSpacetimeHum(metricWarp: number): void {
    if (!this.ctx || !this.humOsc || !this.humGain || this.isMuted) return;

    const baseFreq = 45.0;
    const modulatedFreq = baseFreq * (1.0 - 0.25 * Math.tanh(Math.abs(metricWarp) / 20.0));
    const now = this.ctx.currentTime;

    this.humOsc.frequency.setTargetAtTime(modulatedFreq, now, 0.1);
    const targetGain = Math.min(0.35, 0.12 + Math.abs(metricWarp) * 0.015);
    this.humGain.gain.setTargetAtTime(targetGain, now, 0.1);
  }

  /**
   * Slingshot thruster launch burst sound (filtered exponential pitch chirp).
   */
  public playLaunchBurst(): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;
    this.resume();

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'triangle';
    osc.frequency.setValueAtTime(180.0, now);
    osc.frequency.exponentialRampToValueAtTime(42.0, now + 0.2);

    gain.gain.setValueAtTime(0.3, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.25);

    osc.connect(gain);
    gain.connect(this.masterGain);

    osc.start(now);
    osc.stop(now + 0.26);
  }

  /**
   * Pulsar acoustic radio metronome click.
   */
  public playPulsarClick(frequencyHz: number = 10.0): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;
    this.resume();

    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'square';
    osc.frequency.setValueAtTime(800.0 + frequencyHz * 10.0, now);

    gain.gain.setValueAtTime(0.12, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.03);

    osc.connect(gain);
    gain.connect(this.masterGain);

    osc.start(now);
    osc.stop(now + 0.035);
  }

  /**
   * Cataclysmic Supernova / Merger bass explosion with high-frequency shimmer.
   */
  public playSupernovaBoom(): void {
    if (!this.ctx || !this.masterGain || this.isMuted) return;
    this.resume();

    const now = this.ctx.currentTime;

    // Sub-bass thump
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();

    osc.type = 'sine';
    osc.frequency.setValueAtTime(95.0, now);
    osc.frequency.exponentialRampToValueAtTime(24.0, now + 1.2);

    gain.gain.setValueAtTime(0.6, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 1.5);

    osc.connect(gain);
    gain.connect(this.masterGain);

    osc.start(now);
    osc.stop(now + 1.55);

    // High shimmer noise
    const bufferSize = this.ctx.sampleRate * 0.8;
    const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const output = buffer.getChannelData(0);
    for (let i = 0; i < bufferSize; i++) {
      output[i] = (Math.random() * 2 - 1) * Math.exp(-i / (this.ctx.sampleRate * 0.2));
    }

    const whiteNoise = this.ctx.createBufferSource();
    whiteNoise.buffer = buffer;

    const noiseFilter = this.ctx.createBiquadFilter();
    noiseFilter.type = 'bandpass';
    noiseFilter.frequency.setValueAtTime(1200, now);

    const noiseGain = this.ctx.createGain();
    noiseGain.gain.setValueAtTime(0.2, now);
    noiseGain.gain.exponentialRampToValueAtTime(0.001, now + 0.8);

    whiteNoise.connect(noiseFilter);
    noiseFilter.connect(noiseGain);
    noiseGain.connect(this.masterGain);

    whiteNoise.start(now);
  }
}
