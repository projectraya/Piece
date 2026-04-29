

'use strict';

const TIMING = Object.freeze({ PERFECT: 50, GOOD: 120 });
const SCORE_TABLE = Object.freeze({ PERFECT: 300, GOOD: 100, MISS: 0, HOLD_TICK: 15 });
const NOTE_H      = 28;
const NOTE_RADIUS = 6;
const NOTE_SPEED  = 420;
const COL_COLORS  = ['#00f5ff', '#ff00cc', '#ffe600', '#39ff14'];

/* ── Note ──────────────────────────────────────────────────────── */
class Note {
  constructor(col, beatMs, tailMs = null) {
    this.id          = Note._nextId++;
    this.col         = col;
    this.beatMs      = beatMs;
    this.tailMs      = tailMs;          // null = tap note
    this.isHold      = tailMs !== null;

    this.y           = -NOTE_H;
    this.tailY       = -NOTE_H;         // canvas Y of the tail end
    this.x           = 0;
    this.width       = 0;

    this.judged      = false;
    this.verdict     = null;
    this.alpha       = 1.0;

    /* Hold-specific state */
    this.holdActive  = false;   // player is currently holding this note
    this.holdMissed  = false;   // player released too early
    this.holdScore   = 0;       // accumulated tick score while held
  }
}
Note._nextId = 0;

/* ── Particle ──────────────────────────────────────────────────── */
class Particle {
  constructor(x, y, color) {
    const angle = Math.random() * Math.PI * 2;
    const speed = 80 + Math.random() * 220;
    this.x = x; this.y = y;
    this.vx = Math.cos(angle) * speed;
    this.vy = Math.sin(angle) * speed;
    this.color = color;
    this.life  = 1.0;
    this.decay = 0.9 + Math.random() * 0.8;
    this.size  = 2 + Math.random() * 4;
    this.trail = [];
  }
  update(dt) {
    this.trail.push({ x: this.x, y: this.y });
    if (this.trail.length > 5) this.trail.shift();
    this.x  += this.vx * dt;
    this.y  += this.vy * dt;
    this.vx *= (1 - 2.5 * dt);
    this.vy *= (1 - 2.5 * dt);
    this.vy += 200 * dt;
    this.life -= this.decay * dt;
  }
  draw(ctx) {
    if (this.life <= 0) return;
    for (let i = 0; i < this.trail.length; i++) {
      ctx.globalAlpha = (i / this.trail.length) * this.life * 0.4;
      ctx.fillStyle   = this.color;
      ctx.beginPath();
      ctx.arc(this.trail[i].x, this.trail[i].y, this.size * (i / this.trail.length), 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.globalAlpha = this.life;
    ctx.fillStyle   = this.color;
    ctx.shadowColor = this.color;
    ctx.shadowBlur  = 8;
    ctx.beginPath();
    ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
    ctx.fill();
    ctx.shadowBlur = 0; ctx.globalAlpha = 1;
  }
  get dead() { return this.life <= 0; }
}

/* ── NoteManager ───────────────────────────────────────────────── */
class NoteManager {
  constructor({ bpm = 128, hitZoneY, canvasH, canvasW, fieldX = 0, fieldW, onJudge } = {}) {
    this.bpm      = bpm;
    this.hitZoneY = hitZoneY;
    this.canvasH  = canvasH;
    this.canvasW  = canvasW;
    this.fieldX   = fieldX;
    this.fieldW   = fieldW || canvasW;
    this.onJudge  = onJudge || (() => {});

    this.notes     = [];
    this.particles = [];
    this._beatQueue = [];

    this.combo    = 0;
    this.maxCombo = 0;
    this.score    = 0;
    this.counts   = { PERFECT: 0, GOOD: 0, MISS: 0 };

    /* Travel time: how many ms a note takes to fall from spawn to hitZoneY */
    this._travelMs = (hitZoneY / NOTE_SPEED) * 1000;
  }

  /* ── Beatmap loading ─────────────────────────────────────────── */
  loadBeatmap(pattern) {
    const beatDurMs = (60 / this.bpm) * 1000;
    this._beatQueue = pattern.map(({ col, beat, holdBeats = 0 }) => ({
      col,
      beatMs:  beat * beatDurMs,
      tailMs:  holdBeats > 0 ? (beat + holdBeats) * beatDurMs : null,
    })).sort((a, b) => a.beatMs - b.beatMs);
  }

  /* ── Frame update ────────────────────────────────────────────── */
  update(audioTimeMs, dt) {
    this._spawnDueNotes(audioTimeMs);
    this._moveNotes(audioTimeMs, dt);
    this._tickHolds(audioTimeMs, dt);
    this._autoMissNotes(audioTimeMs);
    this._updateParticles(dt);
  }

  /* ── Hit detection: TAP ──────────────────────────────────────── */
  tryHit(col, audioTimeMs) {
    const note = this.notes.find(n =>
      n.col === col && !n.judged &&
      !n.holdActive &&
      Math.abs(audioTimeMs - n.beatMs) < TIMING.GOOD
    );
    if (!note) return null;

    const delta = Math.abs(audioTimeMs - note.beatMs);
    const verdict = delta <= TIMING.PERFECT ? 'PERFECT' : 'GOOD';

    if (note.isHold) {
      /* Head hit — activate hold, don't fully judge yet */
      note.holdActive = true;
      note.verdict    = verdict;
      this._spawnParticles(note, 8);
      /* feedback without scoring yet */
      this.onJudge({ verdict, col, combo: this.combo, score: this.score, isHoldHead: true });
    } else {
      this._applyJudgment(note, verdict);
    }
    return verdict;
  }

  /* ── Hold release ────────────────────────────────────────────── */
  tryRelease(col, audioTimeMs) {
    const note = this.notes.find(n => n.col === col && n.isHold && n.holdActive && !n.judged);
    if (!note) return;

    const delta = Math.abs(audioTimeMs - note.tailMs);
    if (delta <= TIMING.GOOD) {
      /* Released at right time — finalise with the verdict from head hit */
      this._applyJudgment(note, note.verdict || 'GOOD', true);
    } else {
      /* Released too early — miss */
      note.holdActive = false;
      note.holdMissed = true;
      this._applyJudgment(note, 'MISS');
    }
  }

  /* ── Tick hold score while held ──────────────────────────────── */
  _tickHolds(audioTimeMs, dt) {
    for (const n of this.notes) {
      if (!n.isHold || !n.holdActive || n.judged) continue;

      /* Award tick points every 100ms of successful hold */
      n._lastTick = n._lastTick || n.beatMs;
      if (audioTimeMs - n._lastTick > 100) {
        n._lastTick  = audioTimeMs;
        n.holdScore += SCORE_TABLE.HOLD_TICK;
        this.score  += SCORE_TABLE.HOLD_TICK;
        /* Small particle burst along the hold body */
        const cx = n.x + n.width / 2;
        this.particles.push(new Particle(cx, this.hitZoneY, COL_COLORS[n.col]));
      }

      /* Auto-judge if tail has passed and note is still held — perfect release */
      if (audioTimeMs > n.tailMs + TIMING.GOOD && !n.judged) {
        this._applyJudgment(n, n.verdict || 'GOOD', true);
      }
    }
  }

  /* ── Spawning ────────────────────────────────────────────────── */
  _spawnDueNotes(audioTimeMs) {
    while (
      this._beatQueue.length > 0 &&
      audioTimeMs >= this._beatQueue[0].beatMs - this._travelMs
    ) {
      const { col, beatMs, tailMs } = this._beatQueue.shift();
      const n    = new Note(col, beatMs, tailMs);
      n.y        = 0;
      n.width    = this._colWidth() - 8;
      n.x        = this.fieldX + col * this._colWidth() + 4;
      this.notes.push(n);
    }
  }

  /* ── Movement ────────────────────────────────────────────────── */
  _moveNotes(audioTimeMs, dt) {
    for (const n of this.notes) {
      if (n.judged && !n.holdActive) {
        n.alpha = Math.max(0, n.alpha - dt * 6);
        continue;
      }

      /* MATH: audio-clock driven position — eliminates drift
         y = hitZoneY - (beatMs - audioTimeMs) / 1000 * speed  */
      n.y = this.hitZoneY - ((n.beatMs - audioTimeMs) / 1000) * NOTE_SPEED;

      if (n.isHold) {
        /* Tail Y — same formula but using tailMs */
        n.tailY = this.hitZoneY - ((n.tailMs - audioTimeMs) / 1000) * NOTE_SPEED;
      }
    }
    this.notes = this.notes.filter(n => !n.judged || n.alpha > 0);
  }

  /* ── Auto-miss ───────────────────────────────────────────────── */
  _autoMissNotes(audioTimeMs) {
    for (const n of this.notes) {
      if (n.judged) continue;
      /* Tap notes: missed if head passes without being hit */
      if (!n.isHold && audioTimeMs - n.beatMs > TIMING.GOOD) {
        this._applyJudgment(n, 'MISS');
      }
      /* Hold notes: missed if head passes without hold being activated */
      if (n.isHold && !n.holdActive && audioTimeMs - n.beatMs > TIMING.GOOD) {
        this._applyJudgment(n, 'MISS');
      }
    }
  }

  /* ── Judgment ────────────────────────────────────────────────── */
  _applyJudgment(note, verdict, isHoldTail = false) {
    note.judged     = true;
    note.holdActive = false;

    if (verdict === 'MISS') {
      this.combo = 0;
    } else {
      this.combo++;
      if (this.combo > this.maxCombo) this.maxCombo = this.combo;
      /* MATH: multiplier = 1 + floor(sqrt(combo)/4) */
      const mult   = 1 + Math.floor(Math.sqrt(this.combo) / 4);
      const points = SCORE_TABLE[verdict] * mult + (isHoldTail ? note.holdScore : 0);
      this.score  += points;
      const count  = isHoldTail ? 18 : (verdict === 'PERFECT' ? 18 : 10);
      this._spawnParticles(note, count);
    }

    this.counts[verdict]++;
    this.onJudge({ verdict, col: note.col, combo: this.combo, score: this.score });
  }

  _spawnParticles(note, count) {
    const cx = note.x + note.width / 2;
    for (let i = 0; i < count; i++) {
      this.particles.push(new Particle(cx, this.hitZoneY, COL_COLORS[note.col]));
    }
  }

  /* ── Particles ───────────────────────────────────────────────── */
  _updateParticles(dt) {
    for (const p of this.particles) p.update(dt);
    this.particles = this.particles.filter(p => !p.dead);
  }

  /* ── Drawing ─────────────────────────────────────────────────── */
  draw(ctx) {
    for (const p of this.particles) p.draw(ctx);
    /* Draw hold bodies first (behind heads) */
    for (const n of this.notes) if (n.isHold) this._drawHoldBody(ctx, n);
    for (const n of this.notes) this._drawNoteHead(ctx, n);
  }

  _drawHoldBody(ctx, n) {
    if (n.alpha <= 0) return;
    const color  = COL_COLORS[n.col];
    const cx     = n.x + n.width / 2;
    const bodyW  = n.width * 0.55;
    const headY  = n.holdActive ? this.hitZoneY : n.y;
    const tailY  = Math.min(n.tailY, headY);   // tail is always above (lower Y) head

    if (tailY >= headY) return;   // fully off screen or collapsed

    ctx.save();
    ctx.globalAlpha = n.alpha * (n.holdMissed ? 0.25 : 0.7);

    /* Body gradient — glows brighter at head end */
    const grad = ctx.createLinearGradient(cx, tailY, cx, headY);
    grad.addColorStop(0, color + '44');
    grad.addColorStop(1, color + 'cc');
    ctx.fillStyle = grad;

    /* Rounded rect for the body */
    const x = cx - bodyW / 2;
    this._roundRect(ctx, x, tailY, bodyW, headY - tailY, 4);
    ctx.fill();

    /* Glowing edge lines */
    ctx.shadowColor = color;
    ctx.shadowBlur  = n.holdActive ? 14 : 6;
    ctx.strokeStyle = color;
    ctx.lineWidth   = n.holdActive ? 2 : 1;
    ctx.stroke();

    /* Tail cap */
    ctx.beginPath();
    ctx.arc(cx, tailY, bodyW / 2, 0, Math.PI * 2);
    ctx.fillStyle = color;
    ctx.fill();

    ctx.restore();
  }

  _drawNoteHead(ctx, n) {
    if (n.alpha <= 0 || (n.holdActive && n.y > this.hitZoneY + NOTE_H)) return;
    const color = COL_COLORS[n.col];
    ctx.globalAlpha = n.alpha;
    ctx.shadowColor = color;
    ctx.shadowBlur  = n.holdActive ? 28 : 20;
    ctx.fillStyle   = n.holdMissed ? '#444' : color;
    this._roundRect(ctx, n.x, n.y - NOTE_H / 2, n.width, NOTE_H, NOTE_RADIUS);
    ctx.fill();
    ctx.shadowBlur  = 0;
    ctx.fillStyle   = 'rgba(255,255,255,0.35)';
    this._roundRect(ctx, n.x + 4, n.y - NOTE_H / 2 + 3, n.width - 8, 5, 2);
    ctx.fill();
    /* Hold indicator arrow on head */
    if (n.isHold && !n.judged) {
      ctx.fillStyle   = 'rgba(0,0,0,0.5)';
      ctx.font        = 'bold 10px sans-serif';
      ctx.textAlign   = 'center';
      ctx.textBaseline = 'middle';
      ctx.fillText('▲', n.x + n.width / 2, n.y);
    }
    ctx.globalAlpha = 1;
    ctx.shadowBlur  = 0;
  }

  _roundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
  }

  /* ── Helpers ─────────────────────────────────────────────────── */
  _colWidth()      { return this.fieldW / 4; }
  getNearNotes()   { return this.notes.filter(n => !n.judged); }

  reset() {
    this.notes = []; this.particles = [];
    this.combo = 0; this.maxCombo = 0; this.score = 0;
    this.counts = { PERFECT: 0, GOOD: 0, MISS: 0 };
  }

  get accuracy() {
    const total = this.counts.PERFECT + this.counts.GOOD + this.counts.MISS;
    if (total === 0) return 100;
    return Math.round(((this.counts.PERFECT + this.counts.GOOD) / total) * 100);
  }
}

/* ── Demo beatmap ──────────────────────────────────────────────── */
const DEMO_BEATMAP = (() => {
  const b = [];
  const pat = [
    { col:0, beat:0 },    { col:2, beat:0.5 },
    { col:1, beat:1 },    { col:3, beat:1.5 },
    { col:0, beat:2, holdBeats:0.5 }, { col:2, beat:2.5 },
    { col:3, beat:3 },    { col:1, beat:3.5 },
    { col:2, beat:4 },    { col:0, beat:4.5 },
    { col:3, beat:5, holdBeats:1 },
    { col:0, beat:6 },    { col:1, beat:6.5 },
    { col:0, beat:7 },
    { col:0, beat:8 },    { col:1, beat:8.25 },
    { col:2, beat:8.5 },  { col:3, beat:8.75 },
    { col:3, beat:9 },    { col:2, beat:9.25 },
    { col:1, beat:9.5 },  { col:0, beat:9.75 },
    { col:0, beat:10, holdBeats:0.5 }, { col:2, beat:10 },
    { col:1, beat:10.5 }, { col:3, beat:11 },
    { col:0, beat:12 },   { col:3, beat:12 },
    { col:1, beat:12.5 }, { col:2, beat:13, holdBeats:1 },
    { col:0, beat:14 },   { col:1, beat:14.5 },
    { col:0, beat:15 },   { col:1, beat:15 },
    { col:2, beat:15 },   { col:3, beat:15 },
  ];
  for (let r = 0; r < 4; r++) {
    const off = r * 16;
    for (const n of pat) b.push({ ...n, beat: n.beat + off });
  }
  return b;
})();

window.NoteManager  = NoteManager;
window.DEMO_BEATMAP = DEMO_BEATMAP;
window.TIMING       = TIMING;
window.COL_COLORS   = COL_COLORS;