
'use strict';

/* ── Constants ────────────────────────────────────────────────── */
const BOT_STATES = Object.freeze({
  IDLE:       'IDLE',
  SCANNING:   'SCANNING',
  READY:      'READY',
  HITTING:    'HITTING',
  RECOVERING: 'RECOVERING',
  MISSING:    'MISSING',
});

/* How far above the hit zone the bot starts paying attention (px) */
const LOOKAHEAD_PX  = 320;
/* How far above the hit zone the bot "arms" for a hit (px) */
const ARM_PX        = 80;
/* How long the RECOVERING state lasts (ms) */
const RECOVER_MS    = 120;
/* Bot accuracy: 0.0 = always miss, 1.0 = always perfect */
const BOT_ACCURACY  = 0.92;

/* ── BotFSM Class ─────────────────────────────────────────────── */
class BotFSM {
  /**
   * @param {object} opts
   * @param {function} opts.onHit  – callback(columnIndex) when bot presses
   * @param {number}  [opts.accuracy=BOT_ACCURACY]
   */
  constructor({ onHit, accuracy = BOT_ACCURACY } = {}) {
    this.state        = BOT_STATES.IDLE;
    this.onHit        = onHit || (() => {});
    this.accuracy     = accuracy;

    /** Currently targeted note { col, y, id } or null */
    this.targetNote   = null;
    /** Timestamp when we entered RECOVERING */
    this._recoverTime = 0;
    /** Whether to miss the current target (decided when entering SCANNING) */
    this._willMiss    = false;

    /** State-entry handlers – kept as a map for clean dispatch */
    this._onEnter = {
      [BOT_STATES.IDLE]:       () => this._enterIdle(),
      [BOT_STATES.SCANNING]:   () => this._enterScanning(),
      [BOT_STATES.READY]:      () => this._enterReady(),
      [BOT_STATES.HITTING]:    () => this._enterHitting(),
      [BOT_STATES.RECOVERING]: () => this._enterRecovering(),
      [BOT_STATES.MISSING]:    () => this._enterMissing(),
    };
  }

  /* ── Public API ─────────────────────────────────────────────── */

  /**
   * update() — called every frame by game.js
   * @param {Note[]}  nearNotes   – notes currently visible (from notes.js)
   * @param {number}  hitZoneY    – canvas Y of the hit zone centre
   * @param {number}  nowMs       – current audio clock time in ms
   */
  update(nearNotes, hitZoneY, nowMs) {
    switch (this.state) {
      case BOT_STATES.IDLE:       this._tickIdle(nearNotes, hitZoneY); break;
      case BOT_STATES.SCANNING:   this._tickScanning(hitZoneY);        break;
      case BOT_STATES.READY:      this._tickReady(hitZoneY);           break;
      case BOT_STATES.HITTING:    /* handled in _enterHitting */        break;
      case BOT_STATES.RECOVERING: this._tickRecovering(nowMs);         break;
      case BOT_STATES.MISSING:    this._tickMissing(hitZoneY);         break;
    }
  }

  /** Transition to a new state */
  transition(newState) {
    if (newState === this.state) return;
    // console.debug(`[BotFSM] ${this.state} → ${newState}`);
    this.state = newState;
    this._onEnter[newState]?.();
    // Notify HUD
    this._broadcastState();
  }

  /* ── Tick Handlers ──────────────────────────────────────────── */

  _tickIdle(nearNotes, hitZoneY) {
    // Pick the topmost note that has entered the lookahead window
    const target = this._pickTarget(nearNotes, hitZoneY);
    if (target) {
      this.targetNote = target;
      this.transition(BOT_STATES.SCANNING);
    }
  }

  _tickScanning(hitZoneY) {
    if (!this.targetNote) { this.transition(BOT_STATES.IDLE); return; }
    const dist = hitZoneY - this.targetNote.y;

    /* MATH NOTE: Linear distance check — no interpolation needed here,
       because we're just comparing pixel positions. */
    if (dist <= ARM_PX) {
      this.transition(this._willMiss ? BOT_STATES.MISSING : BOT_STATES.READY);
    }
  }

  _tickReady(hitZoneY) {
    if (!this.targetNote) { this.transition(BOT_STATES.IDLE); return; }
    const dist = hitZoneY - this.targetNote.y;

    /* Fire when note is within ±10px of hit zone centre */
    if (Math.abs(dist) <= 10) {
      this.transition(BOT_STATES.HITTING);
    }
    /* If note sailed past, give up */
    if (dist < -40) {
      this.targetNote = null;
      this.transition(BOT_STATES.IDLE);
    }
  }

  _tickRecovering(nowMs) {
    if (nowMs - this._recoverTime >= RECOVER_MS) {
      this.targetNote = null;
      this.transition(BOT_STATES.IDLE);
    }
  }

  _tickMissing(hitZoneY) {
    if (!this.targetNote) { this.transition(BOT_STATES.IDLE); return; }
    /* Let the note drift past the hit zone entirely */
    if (this.targetNote.y > hitZoneY + 60) {
      this.targetNote = null;
      this.transition(BOT_STATES.IDLE);
    }
  }

  /* ── Enter Handlers ─────────────────────────────────────────── */

  _enterIdle()      { /* nothing to set up */ }
  _enterReady()     { /* could add a visual "priming" effect here */ }
  _enterMissing()   { /* intentional no-op */ }

  _enterScanning() {
    /* Decide NOW (at scan time) whether we'll miss this note.
       Math: uniform random draw against accuracy threshold. */
    this._willMiss = Math.random() > this.accuracy;
  }

  _enterHitting() {
    if (this.targetNote !== null) {
      this.onHit(this.targetNote.col);
    }
    this.transition(BOT_STATES.RECOVERING);
  }

  _enterRecovering() {
    this._recoverTime = performance.now();
  }

  /* ── Helpers ────────────────────────────────────────────────── */

  /**
   * Choose the highest-priority (closest to hit zone, not yet passed)
   * note that has entered the lookahead window.
   *
   * MATH NOTE: We sort by ascending distance-to-hitzone, which is a
   * simple linear ranking — O(n log n) but n is always tiny (< 20).
   */
  _pickTarget(notes, hitZoneY) {
    return notes
      .filter(n => {
        const dist = hitZoneY - n.y;
        return dist > 0 && dist <= LOOKAHEAD_PX && !n.judged;
      })
      .sort((a, b) => (hitZoneY - a.y) - (hitZoneY - b.y))
      .shift() || null;
  }

  _broadcastState() {
    /* Let the HUD know via a simple custom event (decoupled from DOM refs) */
    window.dispatchEvent(
      new CustomEvent('bot:stateChange', { detail: { state: this.state } })
    );
  }
}

/* ── Exports (module-less, globals for simplicity in this prototype) ── */
window.BotFSM      = BotFSM;
window.BOT_STATES  = BOT_STATES;
