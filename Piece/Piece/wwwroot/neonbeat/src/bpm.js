
'use strict';

/* ── Constants ────────────────────────────────────────────────── */

/** Target sample rate for the downsampled energy envelope (Hz).
 *  Lower = faster, less precise.  4 410 Hz works well. */
const ANALYSIS_RATE  = 4410;

/** Frame size in samples at ANALYSIS_RATE.
 *  ~10ms per frame: 4410 * 0.010 ≈ 44 samples */
const FRAME_SIZE     = 44;

/** BPM search range */
const BPM_MIN = 60;
const BPM_MAX = 180;

/* ── Public API ───────────────────────────────────────────────── */

/**
 * detectBPM(audioBuffer)
 * Analyses an AudioBuffer and returns an estimated BPM (integer).
 *
 * @param  {AudioBuffer} audioBuffer  — decoded audio from decodeAudioData()
 * @returns {number}  estimated BPM
 */
function detectBPM(audioBuffer) {
  /* Step 1 — Convert to mono float32 array at original sample rate */
  const rawData = _toMono(audioBuffer);

  /* Step 2 — Downsample to ANALYSIS_RATE
     MATH: decimation factor = originalRate / targetRate
     We average every `step` samples into one output sample. */
  const step       = Math.round(audioBuffer.sampleRate / ANALYSIS_RATE);
  const downsampled = _downsample(rawData, step);

  /* Step 3 — Compute RMS energy envelope, one value per FRAME_SIZE samples
     MATH: RMS = sqrt( (1/N) * sum(x_i^2) )
     This gives a smooth "loudness over time" signal. */
  const envelope = _energyEnvelope(downsampled, FRAME_SIZE);

  /* Step 4 — Autocorrelation over the BPM-relevant lag range
     MATH: autocorrelation at lag L:
       R(L) = sum_i( envelope[i] * envelope[i + L] )
     A high R(L) means the signal is similar to itself shifted by L frames,
     i.e. there is a repeating pattern with period L. */
  const { lag, bpm } = _autocorrelate(envelope, ANALYSIS_RATE, FRAME_SIZE);

  return Math.round(bpm);
}

/* ── Internal helpers ─────────────────────────────────────────── */

/** Mix all channels down to a single Float32Array */
function _toMono(audioBuffer) {
  const len    = audioBuffer.length;
  const mono   = new Float32Array(len);
  const nCh    = audioBuffer.numberOfChannels;

  for (let ch = 0; ch < nCh; ch++) {
    const ch_data = audioBuffer.getChannelData(ch);
    for (let i = 0; i < len; i++) {
      mono[i] += ch_data[i] / nCh;   // average across channels
    }
  }
  return mono;
}

/** Reduce sample count by averaging every `step` samples */
function _downsample(data, step) {
  const out = new Float32Array(Math.floor(data.length / step));
  for (let i = 0; i < out.length; i++) {
    let sum = 0;
    for (let j = 0; j < step; j++) {
      sum += Math.abs(data[i * step + j]);
    }
    out[i] = sum / step;
  }
  return out;
}

/** Compute RMS energy per frame */
function _energyEnvelope(data, frameSize) {
  const frames = Math.floor(data.length / frameSize);
  const env    = new Float32Array(frames);

  for (let f = 0; f < frames; f++) {
    let sumSq = 0;
    const offset = f * frameSize;
    for (let i = 0; i < frameSize; i++) {
      sumSq += data[offset + i] ** 2;
    }
    /* MATH: RMS = sqrt(mean of squares) */
    env[f] = Math.sqrt(sumSq / frameSize);
  }
  return env;
}

/**
 * Autocorrelate the energy envelope over the lag range for BPM_MIN–BPM_MAX.
 *
 * MATH:
 *   framesPerBeat(bpm) = (ANALYSIS_RATE / FRAME_SIZE) * (60 / bpm)
 *                      = framesPerSecond * secondsPerBeat
 *
 * We search lags from framesPerBeat(BPM_MAX) to framesPerBeat(BPM_MIN)
 * and find the lag with the highest autocorrelation sum.
 */
function _autocorrelate(env, analysisRate, frameSize) {
  const fps       = analysisRate / frameSize;   // frames per second
  const lagMin    = Math.floor(fps * 60 / BPM_MAX);
  const lagMax    = Math.ceil (fps * 60 / BPM_MIN);
  const N         = env.length;

  let bestLag  = lagMin;
  let bestCorr = -Infinity;

  for (let lag = lagMin; lag <= lagMax; lag++) {
    let corr = 0;
    const limit = N - lag;
    for (let i = 0; i < limit; i++) {
      corr += env[i] * env[i + lag];
    }
    /* Normalise by number of summed pairs so longer lags aren't penalised */
    corr /= limit;

    if (corr > bestCorr) {
      bestCorr = corr;
      bestLag  = lag;
    }
  }

  /* MATH: bpm = (framesPerSecond / bestLag) * 60 */
  const bpm = (fps / bestLag) * 60;
  return { lag: bestLag, bpm };
}

/* ── Export ───────────────────────────────────────────────────── */
window.detectBPM = detectBPM;

/**
 * detectOffset(audioBuffer, bpm)
 * Finds the ms offset from the file start to the FIRST downbeat.
 *
 * MATH: We know the beat period (beatMs). We scan the energy envelope
 * and find the phase shift φ that maximises the sum of energy values
 * at positions φ, φ+beatMs, φ+2*beatMs, …
 * That φ is the offset.
 *
 * @param  {AudioBuffer} audioBuffer
 * @param  {number}      bpm
 * @returns {number}  offset in ms  (typically 0–2000ms)
 */
function detectOffset(audioBuffer, bpm) {
  const FRAME   = 512;
  const sr      = audioBuffer.sampleRate;
  const mono    = _toMono(audioBuffer);
  const nFrames = Math.floor(mono.length / FRAME);

  /* Build energy envelope */
  const energy = new Float32Array(nFrames);
  for (let f = 0; f < nFrames; f++) {
    let s = 0; const o = f * FRAME;
    for (let i = 0; i < FRAME; i++) s += mono[o+i] ** 2;
    energy[f] = Math.sqrt(s / FRAME);
  }

  const beatMs      = (60 / bpm) * 1000;
  const frameDurMs  = (FRAME / sr) * 1000;
  const framesPerBeat = beatMs / frameDurMs;

  /* Search phase offsets from 0 to 1 beat */
  let bestPhase = 0, bestSum = -1;
  const steps = Math.ceil(framesPerBeat);

  for (let phase = 0; phase < steps; phase++) {
    let sum = 0, count = 0;
    for (let f = phase; f < nFrames; f += framesPerBeat) {
      sum += energy[Math.round(f)] || 0;
      count++;
    }
    if (sum / count > bestSum) {
      bestSum   = sum / count;
      bestPhase = phase;
    }
  }

  return Math.round(bestPhase * frameDurMs);
}

window.detectOffset = detectOffset;


/* ================================================================
   BEATMAP GENERATION  —  Grid-Aware Charter
   ================================================================

   PHILOSOPHY (from the RoBeats / osu!mania charting spec):

   Notes must feel like they BELONG to the music, not just land on
   every transient.  The two-step approach:

     STEP A  — Beat-grid analysis
       Quantise the song into a fine grid (1/16th notes by default).
       For each grid slot, measure how much energy PEAKS at that exact
       moment vs the surrounding slots.  This tells us "was there a
       real musical event here?" rather than "was there any noise?"

     STEP B  — Musical patterning rules
       Once we know WHEN notes go, decide WHICH column each one gets
       using charting conventions:
         • Pitch-to-lane  — low freq → left cols, high freq → right cols
         • Downbeat emphasis  — beat 1 of each bar gets a chord (2 notes)
         • Syncopation  — off-beat 8th/16th hits alternate hands
         • NPS cap  — never exceed ~6 notes/sec (stays humanly hittable)
         • Min gap  — 90ms between notes in the same column

   The result is a chart that reacts to the actual song structure
   rather than carpet-bombing every frame with notes.
   ================================================================ */

/**
 * generateBeatmap(audioBuffer, bpm)
 *
 * @param  {AudioBuffer} audioBuffer
 * @param  {number}      bpm          — detected or user-adjusted BPM
 * @returns {{ col:number, beat:number }[]}   sorted by beat ascending
 */
function generateBeatmap(audioBuffer, bpm, opts = {}) {
  const difficulty     = opts.difficulty || 'medium';
  const offsetMs       = opts.offset     || 0;       // ms shift to first downbeat

  /* ── Tunables per difficulty ──────────────────────────────────
     easy:   sparse, only downbeats + strong 8ths, max 3 NPS
     medium: standard, 8th-note grid, max 6 NPS          (default)
     hard:   dense, 16th-note grid allowed, max 10 NPS          */
  const DIFF = {
    easy:   { GRID_DIV:8,  ONSET_THRESH:2.2, MAX_NPS:3,  MIN_COL_GAP_MS:180, CHORD_THRESH:0.95 },
    medium: { GRID_DIV:16, ONSET_THRESH:1.6, MAX_NPS:6,  MIN_COL_GAP_MS:90,  CHORD_THRESH:0.85 },
    hard:   { GRID_DIV:16, ONSET_THRESH:1.2, MAX_NPS:10, MIN_COL_GAP_MS:60,  CHORD_THRESH:0.70 },
  };
  const { GRID_DIV, ONSET_THRESH, MAX_NPS, MIN_COL_GAP_MS, CHORD_THRESH } = DIFF[difficulty] || DIFF.medium;
  const ONSET_WINDOW   = 16;   // frames for local mean window

  const sr         = audioBuffer.sampleRate;
  const durationMs = audioBuffer.duration * 1000;
  const beatMs     = (60 / bpm) * 1000;          // ms per quarter note
  const slotMs     = beatMs / GRID_DIV;           // ms per grid slot (e.g. ~29ms at 128bpm/16th)
  const totalSlots = Math.ceil(durationMs / slotMs);

  /* ── A1: Separate low and high frequency bands ────────────────
     Low band  (~20–300 Hz)  = kick drum, bass → left columns
     High band (~300 Hz+)    = snare, hi-hat, melody → right columns

     MATH: We use a simple single-pole IIR filter for each band.
       Low-pass:   y[n] = α*x[n] + (1-α)*y[n-1],  α = 2πfc / (2πfc + sr)
       High-pass:  y[n] = (1-α)*(y[n-1] + x[n] - x[n-1])
     This avoids an FFT and runs in O(n).                        */

  const mono = _toMono(audioBuffer);

  const FC_LOW  = 300;   // Hz
  const alphaL  = (2 * Math.PI * FC_LOW)  / (2 * Math.PI * FC_LOW  + sr);
  const alphaH  = (2 * Math.PI * FC_LOW)  / (2 * Math.PI * FC_LOW  + sr);

  const lowBand  = new Float32Array(mono.length);
  const highBand = new Float32Array(mono.length);

  let yL = 0, yH = 0, prevX = 0;
  for (let i = 0; i < mono.length; i++) {
    const x = mono[i];
    yL = alphaL * x + (1 - alphaL) * yL;
    yH = (1 - alphaH) * (yH + x - prevX);
    lowBand[i]  = yL;
    highBand[i] = yH;
    prevX = x;
  }

  /* ── A2: RMS energy per GRID SLOT for each band ───────────────
     MATH: RMS = sqrt( mean(x²) ) over the samples in each slot  */
  const samplesPerSlot = Math.round(sr * slotMs / 1000);

  const energyLow  = new Float32Array(totalSlots);
  const energyHigh = new Float32Array(totalSlots);

  for (let s = 0; s < totalSlots; s++) {
    const start = s * samplesPerSlot;
    const end   = Math.min(start + samplesPerSlot, mono.length);
    let sumL = 0, sumH = 0;
    for (let i = start; i < end; i++) {
      sumL += lowBand[i]  ** 2;
      sumH += highBand[i] ** 2;
    }
    const n = end - start;
    energyLow[s]  = Math.sqrt(sumL / n);
    energyHigh[s] = Math.sqrt(sumH / n);
  }

  /* ── A3: Spectral flux (positive energy increase) per slot ────
     MATH: flux[s] = max(0, energy[s] - energy[s-1])
     Captures moments where energy RISES — i.e. new note attacks  */
  const fluxLow  = new Float32Array(totalSlots);
  const fluxHigh = new Float32Array(totalSlots);
  for (let s = 1; s < totalSlots; s++) {
    fluxLow[s]  = Math.max(0, energyLow[s]  - energyLow[s-1]);
    fluxHigh[s] = Math.max(0, energyHigh[s] - energyHigh[s-1]);
  }

  /* ── A4: Adaptive threshold peak-picking ─────────────────────
     threshold[s] = ONSET_THRESH * mean( flux[s-W .. s+W] )
     Only slots where flux exceeds their local threshold are onsets.
     This adapts to quiet intros, loud drops, breakdowns etc.     */
  function pickOnsets(flux) {
    const onsets = new Uint8Array(totalSlots);
    for (let s = ONSET_WINDOW; s < totalSlots - ONSET_WINDOW; s++) {
      let mean = 0;
      for (let w = s - ONSET_WINDOW; w <= s + ONSET_WINDOW; w++) mean += flux[w];
      mean /= (ONSET_WINDOW * 2 + 1);
      if (flux[s] > ONSET_THRESH * mean && flux[s] > 0.0005) {
        onsets[s] = 1;
      }
    }
    return onsets;
  }

  const onsetsLow  = pickOnsets(fluxLow);
  const onsetsHigh = pickOnsets(fluxHigh);

  /* ── B: Patterning — convert onset slots to chart notes ───────

     Column assignment rules:
       cols 0,1 (D,F) = LEFT  HAND → driven by low-freq  onsets
       cols 2,3 (J,K) = RIGHT HAND → driven by high-freq onsets

     Within each hand we alternate between the two columns to keep
     the chart physically comfortable.

     Downbeats (slot % GRID_DIV === 0) get extra emphasis:
       if BOTH bands peak on a downbeat → chord (one note each side)

     NPS cap: if more than MAX_NPS notes would land in any 1-second
     window, we thin them by only keeping the highest-flux ones.    */

  // lastHitMs per column for MIN_COL_GAP enforcement
  const lastHitMs  = [-Infinity, -Infinity, -Infinity, -Infinity];
  // alternating pointer within each hand
  let leftPtr  = 0;   // 0→col0, 1→col1
  let rightPtr = 0;   // 0→col2, 1→col3

  // NPS rate limiter: track note times in a sliding 1s window
  const recentNotes = [];

  const notes = [];  // final output

  for (let s = 0; s < totalSlots; s++) {
    const tMs   = s * slotMs;
    /* MATH: subtract offsetMs so beat=0 aligns to the first real downbeat.
       Beat values < 0 are before the music starts — skip them. */
    const beatRaw = (tMs - offsetMs) / beatMs;
    const beat    = beatRaw;
    if (beat < 0) continue;

    /* Is this slot a downbeat? (beat 1 of each measure, assuming 4/4) */
    const isDownbeat = (s % GRID_DIV === 0);
    /* Is this an 8th-note position? (every 2 slots on 1/16 grid) */
    const isEighth   = (s % 2 === 0);

    const hasLow  = onsetsLow[s]  === 1;
    const hasHigh = onsetsHigh[s] === 1;

    if (!hasLow && !hasHigh) continue;

    /* NPS cap — prune sliding window */
    while (recentNotes.length > 0 && tMs - recentNotes[0] > 1000) recentNotes.shift();
    if (recentNotes.length >= MAX_NPS) continue;

    /* ── Assign columns ── */
    const candidates = [];

    if (hasLow) {
      /* Low freq → left hand columns 0,1 */
      const col = leftPtr % 2;   // alternates 0,1
      if (tMs - lastHitMs[col] >= MIN_COL_GAP_MS) {
        candidates.push(col);
        leftPtr++;
      }
    }

    if (hasHigh) {
      /* High freq → right hand columns 2,3 */
      const col = 2 + (rightPtr % 2);   // alternates 2,3
      if (tMs - lastHitMs[col] >= MIN_COL_GAP_MS) {
        candidates.push(col);
        rightPtr++;
      }
    }

    /* On downbeats, if only one band fired but energy is high,
       add a chord note on the opposite hand for emphasis */
    if (isDownbeat && candidates.length === 1) {
      const relEnergy = Math.max(energyLow[s], energyHigh[s]);
      const prevMax   = Math.max(...Array.from({length:8},(_,i)=>
        Math.max(energyLow[Math.max(0,s-i)], energyHigh[Math.max(0,s-i)])
      ));
      if (relEnergy > CHORD_THRESH * prevMax) {
        /* Add the other hand */
        if (!hasLow) {
          const col = leftPtr % 2;
          if (tMs - lastHitMs[col] >= MIN_COL_GAP_MS) { candidates.push(col); leftPtr++; }
        } else {
          const col = 2 + (rightPtr % 2);
          if (tMs - lastHitMs[col] >= MIN_COL_GAP_MS) { candidates.push(col); rightPtr++; }
        }
      }
    }

    /* Skip 16th-note off-beats unless there was a clear transient */
    if (!isEighth && !isDownbeat) {
      /* Only keep a 16th-note hit if flux is a strong peak */
      const totalFlux = fluxLow[s] + fluxHigh[s];
      const prevFlux  = fluxLow[s-1] + fluxHigh[s-1];
      const nextFlux  = (s+1 < totalSlots) ? fluxLow[s+1] + fluxHigh[s+1] : 0;
      if (totalFlux < prevFlux * 1.5 && totalFlux < nextFlux * 1.5) continue;
    }

    for (const col of candidates) {
      notes.push({ col, beat });
      lastHitMs[col] = tMs;
      recentNotes.push(tMs);
    }
  }

  notes.sort((a, b) => a.beat - b.beat);

  /* ── Add hold notes ───────────────────────────────────────────
     After charting, identify runs where the same column fires on
     consecutive downbeats — convert the first note into a hold
     that spans to the next note in that column.

     Hold probability scales with difficulty:
       easy: 10%  medium: 20%  hard: 30%
     Only notes with a gap of at least 1 beat become holds,
     so they never overlap.                                       */
  const holdProb = { easy: 0.10, medium: 0.20, hard: 0.30 }[difficulty] || 0.20;

  for (let i = 0; i < notes.length - 1; i++) {
    const n    = notes[i];
    const next = notes.find((m, j) => j > i && m.col === n.col);
    if (!next) continue;

    const gap = next.beat - n.beat;   // gap in quarter-note units
    if (gap < 1 || gap > 4) continue; // too short or too long
    if (Math.random() > holdProb) continue;

    /* Turn this note into a hold ending just before the next note */
    n.holdBeats = gap - 0.25;
  }

  console.log(`[Charter] Generated ${notes.length} notes for ${(durationMs/1000).toFixed(1)}s song @ ${bpm} BPM`);
  console.log(`[Charter] Avg NPS: ${(notes.length / (durationMs/1000)).toFixed(2)}`);
  return notes;
}

window.generateBeatmap = generateBeatmap;