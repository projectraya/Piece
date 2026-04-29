

'use strict';

class EventManager {
  /**
   * @param {object} opts
   * @param {HTMLCanvasElement} opts.canvas
   * @param {object}           opts.gameAPI  – interface into game.js
   */
  constructor({ canvas, gameAPI }) {
    this.canvas  = canvas;
    this.game    = gameAPI;

    /** Map: keyCode → column index (DFJK layout) */
    this.KEY_MAP = {
      KeyD: 0, KeyF: 1, KeyJ: 2, KeyK: 3,
    };

    /** Track which keys are currently held to avoid key-repeat spam */
    this._heldKeys = new Set();

    /** Custom cursor position */
    this._cursorX = -100;
    this._cursorY = -100;

    /** Throttle: mousemove fires at most every 16ms (~60fps) */
    this._lastMouseMove = 0;

    this._registerAll();
  }

  /* ── Registration ─────────────────────────────────────────────── */

  _registerAll() {
    // ── 1. keydown ────────────────────────────────────────────────
    window.addEventListener('keydown', (e) => this._onKeyDown(e));

    // ── 2. keyup ──────────────────────────────────────────────────
    window.addEventListener('keyup', (e) => this._onKeyUp(e));

    // ── 3. click ──────────────────────────────────────────────────
    // Delegated from document.body — catches all menu/HUD buttons
    document.body.addEventListener('click', (e) => this._onClick(e));

    // ── 4. mousemove ──────────────────────────────────────────────
    // Throttled to avoid over-triggering cursor/highlight updates
    document.addEventListener('mousemove', (e) => this._onMouseMove(e));

    // ── 5. mousedown on canvas ────────────────────────────────────
    // Allows mouse/touch column hits (accessibility + mobile friendliness)
    this.canvas.addEventListener('mousedown', (e) => this._onCanvasMouseDown(e));

    // ── 6. contextmenu ────────────────────────────────────────────
    // Block right-click during gameplay so it doesn't steal focus
    this.canvas.addEventListener('contextmenu', (e) => {
      e.preventDefault();
      // Could show a custom context menu here in future
    });

    // ── 7. wheel ──────────────────────────────────────────────────
    // Scroll wheel adjusts master volume in-game
    document.addEventListener('wheel', (e) => this._onWheel(e), { passive: false });

    // ── 8. focus ──────────────────────────────────────────────────
    // Tab/window regains focus — resume if we auto-paused
    window.addEventListener('focus', () => this._onFocus());

    // ── 9. blur ───────────────────────────────────────────────────
    // Tab/window loses focus — auto-pause to avoid missed notes
    window.addEventListener('blur', () => this._onBlur());

    // ── 10. dblclick ──────────────────────────────────────────────
    // Double-click canvas to toggle fullscreen
    this.canvas.addEventListener('dblclick', () => this._onDblClick());

    // ── 11. visibilitychange (bonus) ──────────────────────────────
    document.addEventListener('visibilitychange', () => {
      if (document.hidden) this._onBlur();
      else                  this._onFocus();
    });

    // ── 12. bot:stateChange (custom) ──────────────────────────────
    // Fired by BotFSM.transition() — updates the HUD bot-state readout
    window.addEventListener('bot:stateChange', (e) => this._onBotStateChange(e));
  }

  /* ── Handlers ─────────────────────────────────────────────────── */

  /** keydown — note hit + UI shortcuts */
  _onKeyDown(e) {
    // Prevent browser shortcuts (arrow scroll, space bar scroll, etc.)
    if (['Space','ArrowUp','ArrowDown'].includes(e.code)) e.preventDefault();

    // Ignore key-repeat events (browser fires them continuously when held)
    if (this._heldKeys.has(e.code)) return;
    this._heldKeys.add(e.code);

    // Note-hit keys
    if (this.KEY_MAP[e.code] !== undefined) {
      const col = this.KEY_MAP[e.code];
      this._lightColumn(col, true);
      this.game.hitColumn(col);
      return;
    }

    // Game controls
    switch (e.code) {
      case 'Escape':
      case 'Space':
        this.game.togglePause();
        break;
      case 'KeyB':
        // Toggle bot on/off mid-game (debug shortcut)
        this.game.toggleBot();
        break;
      case 'KeyR':
        if (e.ctrlKey) { e.preventDefault(); this.game.restart(); }
        break;
    }
  }

  /** keyup — release key lights, trigger hold-note release */
  _onKeyUp(e) {
    this._heldKeys.delete(e.code);
    if (this.KEY_MAP[e.code] !== undefined) {
      const col = this.KEY_MAP[e.code];
      this._lightColumn(col, false);
      this.game.releaseColumn(col);
    }
  }

  /** click — delegated menu / button routing */
  _onClick(e) {
    const target = e.target.closest('[data-action], button[id]');
    if (!target) return;

    const action = target.dataset.action || target.id;
    switch (action) {
      /* menu → song select */
      case 'song-select':     this.game.showScreen('songselect'); break;
      case 'song-select-bot':      this.game.showScreen('songselect'); break;
      case 'stats':
      case 'btn-stats':            this.game.showScreen('stats'); break;
      case 'btn-back-stats':       this.game.showScreen('menu'); break;
        window._songSelect?.setPendingBot(true);
        this.game.showScreen('songselect');
        break;
      /* legacy direct-start (kept for restart flow) */
      case 'play':        this.game.startGame({}); break;
      case 'autoplay':    this.game.startGame({ botEnabled: true }); break;
      /* song select screen internal buttons handled by SongSelectManager */
      case 'howto':
      case 'btn-howto':   this.game.showScreen('howto');  break;
      case 'btn-back-howto': this.game.showScreen('menu'); break;
      case 'btn-back-songselect': this.game.showScreen('menu'); break;
      case 'btn-launch-record':
        window._songSelect?._launchRecord();
        break;
      case 'btn-resume':  this.game.togglePause(); break;
      case 'btn-quit':    this.game.showScreen('menu'); break;
      case 'btn-retry':   this.game.restart(); break;
      case 'btn-menu':    this.game.showScreen('menu'); break;
      case 'btn-saveslot-skip': this.game.showScreen('results'); break;
    }
  }

  /** mousemove — track cursor + column hover glow */
  _onMouseMove(e) {
    const now = performance.now();
    /* MATH NOTE: Throttle via time-delta check — only process if
       16ms (≈60fps) has elapsed since last call */
    if (now - this._lastMouseMove < 16) return;
    this._lastMouseMove = now;

    this._cursorX = e.clientX;
    this._cursorY = e.clientY;
    this.game.setCursorPos(this._cursorX, this._cursorY);
  }

  /** mousedown on canvas — hit the column the mouse is over */
  _onCanvasMouseDown(e) {
    if (e.button !== 0) return; // left-click only
    const rect   = this.canvas.getBoundingClientRect();
    const relX   = e.clientX - rect.left;
    /* FIELD_W / 4 cols, offset by fieldX so clicks outside the play area
       don't accidentally trigger a column */
    const fieldX = (this.canvas.width - (window._game?.noteManager?.fieldW || this.canvas.width)) / 2;
    const fieldW = window._game?.noteManager?.fieldW || this.canvas.width;
    const colW   = fieldW / 4;
    const col    = Math.floor((relX - fieldX) / colW);
    if (col >= 0 && col <= 3) {
      this._lightColumn(col, true);
      this.game.hitColumn(col);
      // Release light after 120ms (simulates keyup for mouse)
      setTimeout(() => this._lightColumn(col, false), 120);
    }
  }

  /** wheel — adjust master volume */
  _onWheel(e) {
    if (!this.game.isPlaying()) return;
    e.preventDefault();
    /* MATH NOTE: Clamp volume delta to ±0.05 per tick, total [0..1] */
    const delta = e.deltaY > 0 ? -0.05 : 0.05;
    this.game.adjustVolume(delta);
  }

  /** focus — auto-resume if we paused on blur */
  _onFocus() {
    if (this.game.wasAutoPaused?.()) {
      this.game.togglePause(/* forceResume= */ true);
    }
  }

  /** blur — auto-pause game when window loses focus */
  _onBlur() {
    if (this.game.isPlaying()) {
      this.game.togglePause(/* forceResume= */ false, /* auto= */ true);
    }
  }

  /** dblclick — fullscreen toggle */
  _onDblClick() {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen?.().catch(() => {});
    } else {
      document.exitFullscreen?.();
    }
  }

  /** bot:stateChange — update HUD bot-state readout */
  _onBotStateChange(e) {
    const el = document.getElementById('hud-bot-state');
    if (el) el.textContent = e.detail.state;
  }

  /* ── Helpers ─────────────────────────────────────────────────── */

  _lightColumn(col, on) {
    const keys = ['D', 'F', 'J', 'K'];
    const el   = document.querySelector(`.key-light[data-key="${keys[col]}"]`);
    if (!el) return;
    if (on) el.classList.add('lit');
    else    el.classList.remove('lit');
  }
}

window.EventManager = EventManager;