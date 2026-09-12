interface ToneOptions {
  frequency?: number;
  frequencyEnd?: number | null;
  duration?: number;
  volume?: number;
  type?: OscillatorType;
  delay?: number;
  pan?: number;
}

interface NoiseOptions {
  duration?: number;
  volume?: number;
  delay?: number;
  highpass?: number;
  lowpass?: number;
}

class SoundEngineClass {
  context: AudioContext | null = null;
  master: GainNode | null = null;
  compressor: DynamicsCompressorNode | null = null;
  enabled = false;

  /**
   * Initializes AudioContext and master routing graph.
   * Graph setup pattern and dynamics compression 
   * (Mozilla Developer Network, 2026)
   */
  async init(): Promise<boolean> {
    try {
      if (!this.context) {
        const AudioContextCtor =
          window.AudioContext || (window as any).webkitAudioContext;

        if (!AudioContextCtor) {
          console.warn("Web Audio API not supported.");
          return false;
        }

        this.context = new AudioContextCtor();

        // Dynamics compressor setup to prevent signal clipping when multiple nodes play
        // (Mozilla Developer Network, 2026)
        this.compressor = this.context.createDynamicsCompressor();
        this.compressor.threshold.value = -18;
        this.compressor.knee.value = 16;
        this.compressor.ratio.value = 5;
        this.compressor.attack.value = 0.003;
        this.compressor.release.value = 0.18;

        this.master = this.context.createGain();
        this.master.gain.value = 0.32;

        this.master.connect(this.compressor);
        this.compressor.connect(this.context.destination);
      }

      if (this.context.state === "suspended") {
        await this.context.resume();
      }

      this.enabled = true;
      return true;
    } catch (error) {
      console.error("Audio initialization failed:", error);
      return false;
    }
  }

  async ensureActive(): Promise<boolean> {
    if (!this.context) {
      return await this.init();
    }

    if (this.context.state === "suspended") {
      try {
        await this.context.resume();
      } catch (error) {
        console.warn("Unable to resume audio:", error);
        return false;
      }
    }

    return true;
  }

  /**
   * Synthesizes oscillator tones with optional pitch ramps and panning.
   * Exponential scheduling and audio gain envelopes
   * (Mozilla Developer Network, 2026)
   */
  tone({
    frequency = 440,
    frequencyEnd = null,
    duration = 0.1,
    volume = 0.1,
    type = "sine",
    delay = 0,
    pan = 0,
  }: ToneOptions = {}) {
    if (!this.enabled || !this.context || !this.master) {
      return;
    }

    const ctx = this.context;
    const start = ctx.currentTime + delay;

    const oscillator = ctx.createOscillator();
    const gain = ctx.createGain();

    oscillator.type = type;
    oscillator.frequency.setValueAtTime(Math.max(1, frequency), start);

    // Exponential pitch glide scheduling (Mozilla Developer Network, 2026)
    if (frequencyEnd) {
      oscillator.frequency.exponentialRampToValueAtTime(
        Math.max(1, frequencyEnd),
        start + duration
      );
    }

    // Fast gain envelope to avoid audible transient clicks (Mozilla Developer Network, 2026)
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(Math.max(0.0001, volume), start + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);

    oscillator.connect(gain);

    // Spatial panning node setup (Mozilla Developer Network, 2026)
    if (typeof ctx.createStereoPanner === "function") {
      const panner = ctx.createStereoPanner();
      panner.pan.value = Math.max(-1, Math.min(1, pan));

      gain.connect(panner);
      panner.connect(this.master);
    } else {
      gain.connect(this.master);
    }

    oscillator.start(start);
    oscillator.stop(start + duration + 0.05);
  }

  /**
   * Generates band-pass filtered noise using an AudioBuffer.
   * (Mozilla Developer Network, 2026)
   */
  noise({
    duration = 0.15,
    volume = 0.04,
    delay = 0,
    highpass = 500,
    lowpass = 14000,
  }: NoiseOptions = {}) {
    if (!this.enabled || !this.context || !this.master) {
      return;
    }

    const ctx = this.context;
    const sampleLength = Math.max(1, Math.floor(ctx.sampleRate * duration));

    // Fill buffer with pseudo-random white noise values (Mozilla Developer Network, 2026)
    const buffer = ctx.createBuffer(1, sampleLength, ctx.sampleRate);
    const data = buffer.getChannelData(0);

    for (let i = 0; i < sampleLength; i++) {
      data[i] = (Math.random() * 2 - 1) * (1 - (i / sampleLength) * 0.35);
    }

    const source = ctx.createBufferSource();
    source.buffer = buffer;

    // Filter node configuration (Mozilla Developer Network, 2026)
    const high = ctx.createBiquadFilter();
    high.type = "highpass";
    high.frequency.value = highpass;

    const low = ctx.createBiquadFilter();
    low.type = "lowpass";
    low.frequency.value = lowpass;

    const gain = ctx.createGain();
    const start = ctx.currentTime + delay;

    gain.gain.setValueAtTime(volume, start);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);

    source.connect(high);
    high.connect(low);
    low.connect(gain);
    gain.connect(this.master);

    source.start(start);
  }

  /* 
   * PRESET AUDIO SEQUENCES, (holoMailAuthor, 2026).
   * */

  open() {
    this.tone({ frequency: 65, frequencyEnd: 105, duration: 0.65, volume: 0.16, type: "sine" });
    this.tone({ frequency: 120, frequencyEnd: 980, duration: 0.75, volume: 0.09, type: "sine" });
    this.tone({ frequency: 260, frequencyEnd: 1650, duration: 0.58, volume: 0.035, type: "sawtooth", delay: 0.08 });
    this.noise({ duration: 0.5, volume: 0.025, highpass: 1300, lowpass: 9000 });
    this.tone({ frequency: 58, duration: 0.42, volume: 0.16, type: "sine", delay: 0.58 });
  }

  materialize() {
    const frequencies = [220, 330, 495, 742, 990];

    frequencies.forEach((frequency, index) => {
      this.tone({
        frequency,
        frequencyEnd: frequency * 1.32,
        duration: 0.55,
        volume: 0.032 - index * 0.003,
        type: "sine",
        delay: index * 0.06,
        pan: -0.45 + index * 0.22,
      });
    });

    this.noise({ duration: 0.42, volume: 0.018, highpass: 2300, lowpass: 12000 });
  }

  packet() {
    this.tone({ frequency: 980, duration: 0.025, volume: 0.035, type: "square" });
    this.tone({ frequency: 1340, duration: 0.03, volume: 0.03, type: "square", delay: 0.055 });
    this.tone({ frequency: 720, duration: 0.045, volume: 0.025, type: "square", delay: 0.11 });
  }

  dataReveal() {
    this.tone({ frequency: 390, duration: 0.07, volume: 0.045, type: "square" });
    this.tone({ frequency: 570, duration: 0.07, volume: 0.04, type: "square", delay: 0.075 });
    this.tone({ frequency: 840, duration: 0.1, volume: 0.035, type: "sine", delay: 0.15 });
  }

  key(character = "") {
    const punctuation = /[.,!?;:'"-]/.test(character);
    const base = punctuation ? 950 : 1450;
    const frequency = base + Math.random() * 900;

    this.tone({
      frequency,
      duration: 0.012 + Math.random() * 0.018,
      volume: punctuation ? 0.008 : 0.014 + Math.random() * 0.008,
      type: Math.random() > 0.7 ? "square" : "sine",
      pan: Math.random() * 0.5 - 0.25,
    });
  }

  confirm() {
    const notes = [392, 523.25, 659.25, 783.99];

    notes.forEach((frequency, index) => {
      this.tone({ frequency, duration: 0.34, volume: 0.04, type: "sine", delay: index * 0.075 });
    });

    this.tone({ frequency: 110, duration: 0.34, volume: 0.08, type: "sine", delay: 0.12 });
  }
}

// Singleton export (holoMailAuthor, 2026)
export const SoundEngine = new SoundEngineClass();