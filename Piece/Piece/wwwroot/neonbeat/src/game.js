

'use strict';

/* ── Tuning constants ─────────────────────────────────────────── */
const BPM             = 128;
const HIT_ZONE_RATIO  = 0.82;   
const LANE_PADDING    = 4;      
const COL_COUNT       = 4;

/* ── Play-field layout ────────────────────────────────────────────
   Notes fall inside a centred strip rather than filling the whole
   canvas width.  FIELD_W is the total width of all 4 columns combined.
   FIELD_X is computed each frame as (canvasW - FIELD_W) / 2.
   FIELD_W can be adjusted to taste — 340px feels close to RoBeats proportions. */

const FIELD_W         = 340;    // total width of the 4-column play area (px)

/* ── Colour palette (canvas — mirrors CSS vars) ──────────────── */
const C = {
  bg:          '#05050f',
  lane:        '#0a0a20',
  laneBorder:  '#1a1a4a',
  hitLine:     '#ffffff',
  hitZoneGlow: 'rgba(255,255,255,0.08)',
  scanline:    'rgba(0,0,0,0.15)',
};

/* ── Game states (not the same as BotFSM states) ─────────────── */
const GS = Object.freeze({
  MENU:        'MENU',
  SONG_SELECT: 'SONG_SELECT',
  COUNTDOWN:   'COUNTDOWN',
  PLAYING:     'PLAYING',
  PAUSED:      'PAUSED',
  RESULTS:     'RESULTS',
});

/* ── Main Game Object ─────────────────────────────────────────── */
class Game {
  constructor() {
    /* Canvas */
    this.canvas  = document.getElementById('game-canvas');
    this.ctx     = this.canvas.getContext('2d');

    /* Dimensions — set in _resize() */
    this.W = 0;
    this.H = 0;
    this.hitZoneY = 0;

    /* State */
    this.state       = GS.MENU;
    this._autoPaused = false;
    this._botEnabled = false;
    this._volume     = 0.7;

    /* Timing */
    this._rafId      = null;
    this._lastTime   = 0;   // last rAF timestamp (ms)
    this._songTime   = 0;   // audio-clock position (ms)

    /* Web Audio API */
    this._audioCtx   = null;
    this._gainNode   = null;
    this._songStartAudioTime = 0; // AudioContext.currentTime at song start

    /* Sub-systems (instantiated in _init) */
    this.noteManager = null;
    this.bot         = null;
    this.events      = null;

    /* Currently loaded song info */
    this._currentSong    = null;
    this._songSource     = null;
    this._songDurationMs = null;

    /* Countdown state */
    this._countdown        = 0;
    this._countdownTimer   = null;   // setInterval ID
    this._countdownGoTimer = null;   // setTimeout ID for post-GO delay

    /* Record mode */
    this._recordMode     = false;
    this._recordedTaps   = [];
    this._recordStart    = 0;

    /* Play mode string e.g. 'auto-medium', 'rec-0', 'record' */
    this._playMode       = 'auto-medium';

    /* Visual: judgment flash */
    this._judgmentText   = '';
    this._judgmentColor  = '#fff';
    this._judgmentAlpha  = 0;

    /* Visual: cursor pos (for in-canvas custom cursor gfx) */
    this._cursorX = -100;
    this._cursorY = -100;

    /* Visual: column press scale (bounce effect) */
    this._colScale = [1, 1, 1, 1];

    this._init();
  }

  /* ── Init ─────────────────────────────────────────────────────── */

  _init() {
    this._resize();
    window.addEventListener('resize', () => this._resize());

    /* Build a gameAPI shim — events.js calls these methods */
    const gameAPI = {
      hitColumn:      (col) => this.hitColumn(col),
      togglePause:    (force, auto) => this.togglePause(force, auto),
      toggleBot:      ()    => this.toggleBot(),
      restart:        ()    => this.restart(),
      startGame:      (bot) => this.startGame(bot),
      showScreen:     (s)   => this.showScreen(s),
      setCursorPos:   (x,y) => { this._cursorX = x; this._cursorY = y; },
      adjustVolume:   (d)   => this._adjustVolume(d),
      isPlaying:      ()    => this.state === GS.PLAYING,
      wasAutoPaused:  ()    => this._autoPaused,
    };

    /* Construct EventManager — passes gameAPI reference */
    this.events = new EventManager({ canvas: this.canvas, gameAPI });

    /* Start the render loop immediately (renders menu/idle state) */
    this._loop(0);
  }

  /* ── Web Audio Setup ──────────────────────────────────────────── */

  _initAudio() {
    /* Close any existing context first — this kills ALL running sources
       from previous plays so they can never stack up. */
    if (this._audioCtx) {
      try { this._audioCtx.close(); } catch(_) {}
      this._audioCtx  = null;
      this._gainNode  = null;
      this._songSource = null;
    }
    this._audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    this._gainNode = this._audioCtx.createGain();
    this._gainNode.gain.value = this._volume;
    this._gainNode.connect(this._audioCtx.destination);
  }

  /**
   * _startSongClock()
   * Records the AudioContext.currentTime at the moment the song "starts".
   * All subsequent audio time reads: (ctx.currentTime - startTime) * 1000 = ms.
   *
   * MATH NOTE: AudioContext.currentTime is the most accurate clock in the
   * browser — it runs on the audio thread, not the JS main thread, so it
   * never drifts from setInterval/setTimeout jitter.
   */
  _startSongClock() {
    this._songStartAudioTime = this._audioCtx.currentTime;
  }

  /** Returns current song position in ms, synced to audio clock */
  _getAudioTimeMs() {
    if (!this._audioCtx) return 0;
    return (this._audioCtx.currentTime - this._songStartAudioTime) * 1000;
  }

  /**
   * _playDrumTick(col, type)
   * Synthesises a simple drum click/beep using Web Audio oscillators.
   * No audio files needed — purely generative!
   *
   * MATH NOTE: Exponential decay for gain (sounds natural):
   *   gain(t) = peak * e^(-decay * t)
   * We use AudioParam.exponentialRampToValueAtTime().
   */
  _playDrumTick(col, type = 'hit') {
    if (!this._audioCtx || !this._gainNode) return;
    const osc  = this._audioCtx.createOscillator();
    const gain = this._audioCtx.createGain();
    osc.connect(gain);
    gain.connect(this._gainNode);

    const now = this._audioCtx.currentTime;

    if (type === 'hit') {
      /* Short snappy click per column — slightly different freq per col */
      const freqs = [220, 277, 330, 415];
      osc.frequency.value = freqs[col] * 2;
      osc.type = 'square';
      gain.gain.setValueAtTime(0.18, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.07);
      osc.start(now);
      osc.stop(now + 0.07);
    } else {
      /* Miss thud */
      osc.frequency.value = 80;
      osc.type = 'sine';
      gain.gain.setValueAtTime(0.08, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.12);
      osc.start(now);
      osc.stop(now + 0.12);
    }
  }

  /* ── Game Loop ────────────────────────────────────────────────── */

  /**
   * _loop(timestamp)
   * Main rAF loop. Uses delta-time so movement is frame-rate independent.
   *
   * MATH NOTE: dt clamped to max 100ms to prevent spiral-of-death when
   * tab is backgrounded and then foregrounded (browser can fire a huge dt).
   */
  _loop(timestamp) {
    const dt = Math.min((timestamp - this._lastTime) / 1000, 0.1); // seconds
    this._lastTime = timestamp;

    if (this.state === GS.PLAYING) {
      this._songTime = this._getAudioTimeMs();
      this.noteManager.update(this._songTime, dt);
      this.bot?.update(this.noteManager.getNearNotes(), this.hitZoneY, this._songTime);
      this._updateColScales(dt);
      this._checkSongEnd();
    }

    this._render(dt);
    this._rafId = requestAnimationFrame((ts) => this._loop(ts));
  }

  /* ── Update ───────────────────────────────────────────────────── */

  /**
   * MATH NOTE: Column "bounce" on press is modelled as a spring:
   *   scale → 0.85 on press, then lerps back to 1.0 each frame.
   *   lerp(a, b, t) = a + (b - a) * t
   *   t = 1 - e^(-k * dt), where k=12 gives snappy recovery.
   */
  _updateColScales(dt) {
    for (let i = 0; i < COL_COUNT; i++) {
      // Exponential approach to 1 — "spring" feel without overshoot
      this._colScale[i] += (1 - this._colScale[i]) * (1 - Math.exp(-12 * dt));
    }
  }

  _checkSongEnd() {
    /* Only run during active play — not during countdown or after results */
    if (this.state !== GS.PLAYING) return;

    /* For real songs: end when audio clock passes song duration + buffer.
       _songTime is anchored to the moment the audio actually started, so
       this will fire at the right time regardless of countdown length. */
    if (this._songDurationMs !== null) {
      if (this._songTime >= this._songDurationMs + 800) {
        this._endGame();
      }
      return;
    }
    /* Demo mode: end when all notes are judged */
    if (
      this.noteManager._beatQueue.length === 0 &&
      this.noteManager.notes.every(n => n.judged)
    ) {
      this._endGame();
    }
  }

  /* ── Render ───────────────────────────────────────────────────── */

  _render(dt) {
    const ctx = this.ctx;
    ctx.clearRect(0, 0, this.W, this.H);

    if (this.state === GS.MENU)    { this._renderIdleCanvas(); return; }
    if (this.state === GS.RESULTS) { return; }

    this._renderBackground();
    this._renderLanes();
    this._renderHitZone();
    this.noteManager?.draw(ctx);
    this._renderJudgment(dt);
    this._renderCustomCursor();
  }

  _renderIdleCanvas() {
    const ctx = this.ctx;
    /* Subtle animated grid on the canvas behind the menu */
    ctx.fillStyle = C.bg;
    ctx.fillRect(0, 0, this.W, this.H);
  }

  _renderBackground() {
    const ctx = this.ctx;
    ctx.fillStyle = C.bg;
    ctx.fillRect(0, 0, this.W, this.H);

    /* Vertical scanline effect — draws faint horizontal bands
       MATH: We step every 4px across height */
    ctx.fillStyle = C.scanline;
    for (let y = 0; y < this.H; y += 4) {
      ctx.fillRect(0, y, this.W, 2);
    }
  }

  _renderLanes() {
    const ctx   = this.ctx;
    const fx    = this._fieldX();
    const colW  = FIELD_W / COL_COUNT;

    /* Side gutters */
    ctx.fillStyle = 'rgba(0,0,0,0.55)';
    ctx.fillRect(0, 0, fx, this.H);
    ctx.fillRect(fx + FIELD_W, 0, this.W - fx - FIELD_W, this.H);

    /* Vignette on gutters */
    const leftG = ctx.createLinearGradient(0, 0, fx, 0);
    leftG.addColorStop(0, 'rgba(0,0,0,0.0)');
    leftG.addColorStop(1, 'rgba(0,0,0,0.4)');
    ctx.fillStyle = leftG;
    ctx.fillRect(0, 0, fx, this.H);
    const rightG = ctx.createLinearGradient(fx + FIELD_W, 0, this.W, 0);
    rightG.addColorStop(0, 'rgba(0,0,0,0.4)');
    rightG.addColorStop(1, 'rgba(0,0,0,0.0)');
    ctx.fillStyle = rightG;
    ctx.fillRect(fx + FIELD_W, 0, this.W - fx - FIELD_W, this.H);

    /* Columns */
    for (let i = 0; i < COL_COUNT; i++) {
      const x = fx + i * colW;

      ctx.fillStyle = C.lane;
      ctx.fillRect(x + LANE_PADDING, 0, colW - LANE_PADDING * 2, this.H);

      ctx.strokeStyle = C.laneBorder;
      ctx.lineWidth   = 1;
      ctx.strokeRect(x + LANE_PADDING, 0, colW - LANE_PADDING * 2, this.H);

      const grad = ctx.createLinearGradient(x, 0, x, 80);
      grad.addColorStop(0, COL_COLORS[i] + '33');
      grad.addColorStop(1, 'transparent');
      ctx.fillStyle = grad;
      ctx.fillRect(x + LANE_PADDING, 0, colW - LANE_PADDING * 2, 80);

      const scale = this._colScale[i];
      if (scale < 0.999) {
        const bright = (1 - scale) * 4;
        ctx.fillStyle = COL_COLORS[i] + Math.round(bright * 30).toString(16).padStart(2, '0');
        ctx.fillRect(x + LANE_PADDING, this.hitZoneY - 30, colW - LANE_PADDING * 2, 60);
      }
    }

    /* Play-field border glow */
    ctx.strokeStyle = 'rgba(255,255,255,0.08)';
    ctx.lineWidth   = 1;
    ctx.strokeRect(fx, 0, FIELD_W, this.H);
  }

  _renderHitZone() {
    const ctx   = this.ctx;
    const y     = this.hitZoneY;
    const fx    = this._fieldX();
    const colW  = FIELD_W / COL_COUNT;

    /* Glow band behind hit line — only over play field */
    const band = ctx.createLinearGradient(0, y - 50, 0, y + 50);
    band.addColorStop(0,   'transparent');
    band.addColorStop(0.5, 'rgba(255,255,255,0.06)');
    band.addColorStop(1,   'transparent');
    ctx.fillStyle = band;
    ctx.fillRect(fx, y - 50, FIELD_W, 100);

    /* Hit line across play field only */
    ctx.strokeStyle = 'rgba(255,255,255,0.5)';
    ctx.lineWidth   = 1;
    ctx.shadowColor = '#ffffff';
    ctx.shadowBlur  = 8;
    ctx.beginPath();
    ctx.moveTo(fx, y);
    ctx.lineTo(fx + FIELD_W, y);
    ctx.stroke();
    ctx.shadowBlur = 0;

    /* ── Hit receptors — large, obvious, per-column ── */
    for (let i = 0; i < COL_COUNT; i++) {
      const cx     = fx + i * colW + colW / 2;   // column centre X
      const cy     = y;
      const scale  = this._colScale[i];
      const color  = COL_COLORS[i];
      const isLit  = scale < 0.98;

      ctx.save();
      ctx.translate(cx, cy);
      ctx.scale(scale, scale);

      /* Outer ring */
      ctx.beginPath();
      ctx.arc(0, 0, 22, 0, Math.PI * 2);
      ctx.strokeStyle = color;
      ctx.lineWidth   = isLit ? 3 : 2;
      ctx.shadowColor = color;
      ctx.shadowBlur  = isLit ? 28 : 10;
      ctx.stroke();

      /* Inner filled circle — dim when idle, bright when pressed */
      ctx.beginPath();
      ctx.arc(0, 0, 13, 0, Math.PI * 2);
      ctx.fillStyle = isLit
        ? color + 'cc'
        : color + '22';
      ctx.shadowBlur  = isLit ? 20 : 0;
      ctx.fill();

      /* Key label inside receptor */
      ctx.shadowBlur  = 0;
      ctx.fillStyle   = isLit ? '#000' : color;
      ctx.font        = "bold 11px 'Orbitron', sans-serif";
      ctx.textAlign   = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText(['D','F','J','K'][i], 0, 0);

      ctx.restore();
    }
  }

  _renderJudgment(dt) {
    if (this._judgmentAlpha <= 0) return;
    const ctx = this.ctx;
    /* MATH: Smooth fade — exponential decay of alpha */
    this._judgmentAlpha = Math.max(0, this._judgmentAlpha - dt * 2.8);

    const y = this.hitZoneY - 60;
    ctx.save();
    ctx.globalAlpha  = this._judgmentAlpha;
    ctx.font         = `bold 24px 'Orbitron', sans-serif`;
    ctx.textAlign    = 'center';
    ctx.fillStyle    = this._judgmentColor;
    ctx.shadowColor  = this._judgmentColor;
    ctx.shadowBlur   = 20;
    /* MATH: float upward during fade using quadratic ease */
    const rise = (1 - this._judgmentAlpha) * 30;
    ctx.fillText(this._judgmentText, this.W / 2, y - rise);
    ctx.restore();
  }

  _renderCustomCursor() {
    if (this._cursorX < 0) return;
    const ctx = this.ctx;
    const rect = this.canvas.getBoundingClientRect();
    const cx = this._cursorX - rect.left;
    const cy = this._cursorY - rect.top;

    ctx.save();
    ctx.strokeStyle = 'rgba(255,255,255,0.6)';
    ctx.lineWidth   = 1;
    ctx.shadowColor = '#00f5ff';
    ctx.shadowBlur  = 8;
    /* Crosshair cursor */
    ctx.beginPath();
    ctx.moveTo(cx - 8, cy); ctx.lineTo(cx + 8, cy);
    ctx.moveTo(cx, cy - 8); ctx.lineTo(cx, cy + 8);
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(cx, cy, 4, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
  }

  /* ── Public Game API ──────────────────────────────────────────── */

  /**
   * startGame({ botEnabled, song, recordMode })
   */
  startGame({ botEnabled = false, song = null, recordMode = false, playMode = null, savedChart = null } = {}) {
    /* Hard stop any running countdown or song before starting fresh */
    this._abortCountdown();

    this._botEnabled   = botEnabled;
    this._currentSong  = song;
    this._recordMode   = recordMode;
    this._recordedTaps = [];
    this._playMode     = playMode || (recordMode ? 'record' : 'auto-medium');
    this._savedChart   = savedChart || null;
    this._initAudio();

    const bpm = song ? song.bpm : BPM;

    /* Build NoteManager */
    this.noteManager = new NoteManager({
      bpm,
      hitZoneY:  this.hitZoneY,
      canvasH:   this.H,
      canvasW:   this.W,
      fieldX:    this._fieldX(),
      fieldW:    FIELD_W,
      onJudge:   (data) => this._onJudge(data),
    });
    /* Load beatmap */
    if (recordMode) {
      this.noteManager.loadBeatmap([]);
      this._songDurationMs = song?.audioBuffer?.duration * 1000 || null;
    } else if (savedChart && savedChart.length > 0) {
      /* Explicitly passed saved chart (recording slot play) */
      this.noteManager.loadBeatmap(savedChart);
      this._songDurationMs = song?.audioBuffer?.duration * 1000 || null;
    } else if (song && song.audioBuffer) {
      const difficulty = song.difficulty || 'medium';
      const offset     = song.offset     || 0;
      const beats = window.generateBeatmap(song.audioBuffer, bpm, { difficulty, offset });
      this.noteManager.loadBeatmap(beats.length > 4 ? beats : DEMO_BEATMAP);
      this._songDurationMs = song.audioBuffer.duration * 1000;
    } else {
      this.noteManager.loadBeatmap(DEMO_BEATMAP);
      this._songDurationMs = null;
    }

    /* Audio will start after the countdown — see _startCountdown */
    this._stopSong();

    /* Build Bot */
    if (botEnabled) {
      this.bot = new BotFSM({
        onHit: (col) => {
          this.hitColumn(col);
          const keys = ['D','F','J','K'];
          const el = document.querySelector(`.key-light[data-key="${keys[col]}"]`);
          if (el) {
            el.classList.add('lit');
            setTimeout(() => el.classList.remove('lit'), 100);
          }
        },
        accuracy: 0.94,
      });
    } else {
      this.bot = null;
      const el = document.getElementById('hud-bot-state');
      if (el) el.textContent = 'OFF';
    }

    this._resetHUD();

    /* Update song title in HUD */
    const titleEl = document.getElementById('hud-song-title');
    if (titleEl) titleEl.textContent = song ? song.title : 'DEMO BEAT';

    /* Record mode banner */
    const recBanner = document.getElementById('record-banner');
    if (recBanner) recBanner.classList.toggle('hidden', !recordMode);

    /* HUD mode display */
    const modeLabel   = document.getElementById('hud-mode-label');
    const botStateEl  = document.getElementById('hud-bot-state');
    if (modeLabel) modeLabel.textContent = recordMode ? 'REC' : (botEnabled ? 'BOT' : 'MODE');
    if (botStateEl) botStateEl.textContent = recordMode ? 'LIVE' : this._playMode.toUpperCase().replace('-',' ');

    this.showScreen('game');
    this._startCountdown();
  }

  /* ── Abort any in-progress countdown ────────────────────────────── */
  _abortCountdown() {
    /* Kill the setInterval tick */
    if (this._countdownTimer) {
      clearInterval(this._countdownTimer);
      this._countdownTimer = null;
    }
    /* Kill the post-GO setTimeout — we store its ID so we can cancel it */
    if (this._countdownGoTimer) {
      clearTimeout(this._countdownGoTimer);
      this._countdownGoTimer = null;
    }
    /* Hide the overlay immediately */
    const overlay = document.getElementById('countdown-overlay');
    if (overlay) overlay.classList.add('hidden');
  }

  /* ── Countdown (3-2-1-GO) ─────────────────────────────────────── */
  _startCountdown() {
    this.state       = GS.COUNTDOWN;
    this._countdown  = 3;

    const overlay = document.getElementById('countdown-overlay');
    const numEl   = document.getElementById('countdown-number');
    const subEl   = document.getElementById('countdown-sub');

    if (overlay) overlay.classList.remove('hidden');
    if (numEl)   numEl.textContent = '3';
    if (subEl)   subEl.textContent = 'GET READY';

    /* Synthesise tick beeps for the countdown */
    const tickBeep = (pitch, duration) => {
      if (!this._audioCtx) return;
      const osc  = this._audioCtx.createOscillator();
      const gain = this._audioCtx.createGain();
      osc.connect(gain); gain.connect(this._gainNode);
      osc.frequency.value = pitch;
      osc.type = 'sine';
      const now = this._audioCtx.currentTime;
      gain.gain.setValueAtTime(0.15, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + duration);
      osc.start(now); osc.stop(now + duration);
    };

    let count = 3;
    tickBeep(440, 0.1);

    this._countdownTimer = setInterval(() => {
      count--;
      if (count > 0) {
        if (numEl) { numEl.textContent = count; numEl.classList.remove('pop'); void numEl.offsetWidth; numEl.classList.add('pop'); }
        tickBeep(440, 0.1);
      } else {
        clearInterval(this._countdownTimer);
        if (numEl)   { numEl.textContent = 'GO!'; numEl.classList.remove('pop'); void numEl.offsetWidth; numEl.classList.add('pop'); }
        if (subEl)   subEl.textContent = '';
        tickBeep(880, 0.2);
        this._countdownGoTimer = setTimeout(() => {
          this._countdownGoTimer = null;
          if (overlay) overlay.classList.add('hidden');
          this.state = GS.PLAYING;
          /* Start audio THEN immediately record the clock origin —
             both must happen in the same synchronous block so that
             _getAudioTimeMs() === 0 at the exact moment the song starts. */
          if (this._currentSong?.audioBuffer) {
            this._playSongBuffer(this._currentSong.audioBuffer);
          }
          this._startSongClock();  // ← moved AFTER playSongBuffer
          this._recordStart = this._audioCtx?.currentTime || 0;
        }, 600);
      }
    }, 1000);
  }

  /** Start playing a decoded AudioBuffer through the gain node */
  _playSongBuffer(audioBuffer) {
    if (!this._audioCtx) return;
    const source = this._audioCtx.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(this._gainNode);
    source.start(0);
    this._songSource = source;
  }

  /** Stop and discard any running song source */
  _stopSong() {
    if (this._songSource) {
      try { this._songSource.stop(); } catch(_) {}
      this._songSource = null;
    }
  }

  hitColumn(col) {
    if (this.state !== GS.PLAYING) return;
    const audioMs = this._getAudioTimeMs();

    /* Record mode — just record the tap, no note judgement */
    if (this._recordMode) {
      this._recordedTaps.push({ col, timeMs: audioMs });
      this._colScale[col] = 0.82;
      this._playDrumTick(col, 'hit');
      /* Update record time display */
      const recTime = document.getElementById('record-time');
      if (recTime) {
        const s = Math.floor(audioMs / 1000);
        recTime.textContent = `${Math.floor(s/60)}:${String(s%60).padStart(2,'0')}`;
      }
      return;
    }

    const verdict = this.noteManager.tryHit(col, audioMs);
    this._colScale[col] = 0.82;
    if (!verdict) this._playDrumTick(col, 'miss');
  }

  /* Called by events.js on keyup for hold notes */
  releaseColumn(col) {
    if (this.state !== GS.PLAYING || this._recordMode) return;
    const audioMs = this._getAudioTimeMs();
    this.noteManager.tryRelease(col, audioMs);
  }

  togglePause(forceResume, auto = false) {
    if (this.state === GS.PLAYING) {
      this.state       = GS.PAUSED;
      this._autoPaused = auto;
      /* Suspend audio context — pauses song AND clock together */
      this._audioCtx?.suspend();
      document.getElementById('pause-overlay')?.classList.remove('hidden');
    } else if (this.state === GS.PAUSED) {
      if (forceResume === false) return;
      this.state       = GS.PLAYING;
      this._autoPaused = false;
      this._audioCtx?.resume();
      document.getElementById('pause-overlay')?.classList.add('hidden');
    }
  }

  toggleBot() {
    this._botEnabled = !this._botEnabled;
    if (!this._botEnabled) { this.bot = null; return; }
    this.bot = new BotFSM({ onHit: (col) => this.hitColumn(col) });
  }

  restart() {
    this._abortCountdown();
    this._stopSong();
    if (this._audioCtx) {
      try { this._audioCtx.close(); } catch(_) {}
      this._audioCtx  = null;
      this._gainNode  = null;
      this._songSource = null;
    }
    this.state = GS.MENU;
    if (this._currentSong) {
      this.showScreen('songselect');
    } else {
      this.showScreen('menu');
    }
  }

  showScreen(name) {
    document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
    const target = document.getElementById(`screen-${name}`);
    if (target) target.classList.add('active');

    if (name !== 'game') {
      document.getElementById('pause-overlay')?.classList.add('hidden');
    }
    if (name === 'songselect' && window._songSelect) window._songSelect.onShow();
    if (name === 'stats'      && window._stats)      window._stats.render();
  }

  setCursorPos(x, y) {
    this._cursorX = x;
    this._cursorY = y;
  }

  isPlaying() { return this.state === GS.PLAYING; }

  wasAutoPaused() { return this._autoPaused; }

  _adjustVolume(delta) {
    this._volume = Math.max(0, Math.min(1, this._volume + delta));
    if (this._gainNode) this._gainNode.gain.value = this._volume;
  }

  /* ── Callbacks ────────────────────────────────────────────────── */

  _onJudge({ verdict, col, combo, score }) {
    this._playDrumTick(col, verdict === 'MISS' ? 'miss' : 'hit');
    this._showJudgment(verdict);
    this._updateHUD(score, combo, verdict);
  }

  _showJudgment(verdict) {
    const map = {
      PERFECT: { text: 'PERFECT', color: '#00f5ff' },
      GOOD:    { text: 'GOOD',    color: '#ffe600' },
      MISS:    { text: 'MISS',    color: '#ff2244' },
    };
    this._judgmentText  = map[verdict].text;
    this._judgmentColor = map[verdict].color;
    this._judgmentAlpha = 1.2; // > 1 so it lingers a moment before fading

    /* Update HUD label */
    const el = document.getElementById('hud-judgment-label');
    if (el) {
      el.textContent  = map[verdict].text;
      el.className    = verdict.toLowerCase();
    }
  }

  _updateHUD(score, combo, verdict) {
    /* Score */
    const scoreEl = document.getElementById('hud-score');
    if (scoreEl) scoreEl.textContent = String(score).padStart(6, '0');

    /* Combo */
    const comboEl = document.getElementById('hud-combo');
    if (comboEl) {
      comboEl.textContent = `x${combo}`;
      comboEl.classList.remove('pop');
      void comboEl.offsetWidth; // force reflow to restart animation
      if (verdict !== 'MISS') comboEl.classList.add('pop');
    }
  }

  _resetHUD() {
    const scoreEl = document.getElementById('hud-score');
    if (scoreEl) scoreEl.textContent = '000000';
    const comboEl = document.getElementById('hud-combo');
    if (comboEl) comboEl.textContent = 'x0';
    const judgEl  = document.getElementById('hud-judgment-label');
    if (judgEl)  { judgEl.textContent = ''; judgEl.className = ''; }
  }

  _endGame() {
    if (this.state === GS.RESULTS || this.state === GS.COUNTDOWN || this.state === GS.MENU) return;
    this.state = GS.RESULTS;
    if (this._countdownTimer) { clearInterval(this._countdownTimer); this._countdownTimer = null; }
    this._stopSong();

    const nm    = this.noteManager;
    const song  = this._currentSong;
    const bpm   = song?.bpm   || 128;
    const title = song?.title || 'DEMO BEAT';

    /* Fill results screen */
    document.getElementById('res-score').textContent   = nm.score;
    document.getElementById('res-combo').textContent   = nm.maxCombo;
    document.getElementById('res-perfect').textContent = nm.counts.PERFECT;
    document.getElementById('res-good').textContent    = nm.counts.GOOD;
    document.getElementById('res-miss').textContent    = nm.counts.MISS;
    document.getElementById('res-acc').textContent     = nm.accuracy + '%';

    /* Rank */
    const acc  = nm.accuracy;
    const rank = acc >= 98 ? 'SS' : acc >= 94 ? 'S' : acc >= 88 ? 'A'
               : acc >= 78 ? 'B'  : acc >= 65 ? 'C' : 'D';
    this._lastRank = rank;
    const rankEl = document.getElementById('res-rank');
    if (rankEl) {
      rankEl.textContent = rank;
      const rankColors = { SS:'#00f5ff', S:'#ffe600', A:'#39ff14', B:'#ff00cc', C:'#ff6600', D:'#ff2244' };
      rankEl.style.color       = rankColors[rank] || '#fff';
      rankEl.style.textShadow  = `0 0 32px ${rankColors[rank]}`;
    }

    /* Record mode — go to save slot screen instead of results */
    if (this._recordMode && this._recordedTaps.length > 0) {
      const beatMs = (60 / bpm) * 1000;
      const chart  = this._recordedTaps.map(({ col, timeMs }) => ({ col, beat: timeMs / beatMs }));
      window._songSelect?.showSaveSlotScreen(title, bpm, chart);
      return;
    }

    /* Log result to SaveManager */
    if (window._saves && title !== 'DEMO BEAT') {
      const RANK_ORDER = ['D','C','B','A','S','SS'];
      const prevBest   = window._saves.getGlobalStats().bestRanks?.[window._saves._hash(title)];
      window._saves.logResult({
        title, bpm,
        score:    nm.score,
        rank,
        accuracy: nm.accuracy,
        mode:     this._playMode,
        perfect:  nm.counts.PERFECT,
        good:     nm.counts.GOOD,
        miss:     nm.counts.MISS,
        maxCombo: nm.maxCombo,
      });
      const newBestEl = document.getElementById('res-new-best');
      if (newBestEl) {
        const improved = !prevBest || RANK_ORDER.indexOf(rank) > RANK_ORDER.indexOf(prevBest);
        newBestEl.classList.toggle('hidden', !improved);
      }
    }

    /* Mode badge */
    const modeBadge = document.getElementById('result-mode-badge');
    if (modeBadge) modeBadge.textContent = this._playMode.toUpperCase().replace('-', ' ');

    this.showScreen('results');
  }

  /* ── Resize ───────────────────────────────────────────────────── */

  _resize() {
    /* Canvas fills the available space between HUD bar and key-lights row.
       We read the actual rendered dimensions from its CSS bounding rect. */
    const rect = this.canvas.getBoundingClientRect();
    this.W = rect.width  || window.innerWidth;
    this.H = rect.height || window.innerHeight * 0.75;

    this.canvas.width  = this.W;
    this.canvas.height = this.H;

    /* MATH NOTE: hitZoneY is a fraction of canvas height so it scales
       correctly on all screen sizes. */
    this.hitZoneY = Math.round(this.H * HIT_ZONE_RATIO);

    /* Propagate to NoteManager if active */
    if (this.noteManager) {
      this.noteManager.hitZoneY = this.hitZoneY;
      this.noteManager.canvasW  = this.W;
      this.noteManager.canvasH  = this.H;
      this.noteManager.fieldX   = this._fieldX();
      this.noteManager.fieldW   = FIELD_W;
    }
  }

  /** Left edge of the centred play field */
  _fieldX() { return Math.round((this.W - FIELD_W) / 2); }
}

function initNeonBeat() {
    if (window._game) return; 
    if (!document.getElementById('game-canvas')) return; 
    window._game = new Game();
    window._game.showScreen('menu');
}

initNeonBeat();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initNeonBeat);
} else {
    initNeonBeat();
}