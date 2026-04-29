

'use strict';

class SaveManager {
  constructor() {
    this.MAX_RECORDINGS = 3;
    this.MAX_HISTORY    = 20;
  }

  /* ── Key helpers ──────────────────────────────────────────────── */

  _songKey(title)  { return 'nb_song_' + this._hash(title); }
  _globalKey()     { return 'nb_global_stats'; }

  /** Simple djb2 hash — turns a song title into a short safe key */
  _hash(str) {
    let h = 5381;
    for (let i = 0; i < str.length; i++) {
      h = ((h << 5) + h) + str.charCodeAt(i);
      h |= 0;
    }
    return Math.abs(h).toString(36);
  }

  /* ── Low-level read/write ────────────────────────────────────── */

  _read(key)        { try { return JSON.parse(localStorage.getItem(key)); } catch { return null; } }
  _write(key, data) { try { localStorage.setItem(key, JSON.stringify(data)); return true; } catch { return false; } }

  /* ── Song data ───────────────────────────────────────────────── */

  getSongData(title) {
    return this._read(this._songKey(title)) || {
      title,
      bpm:        128,
      recordings: [null, null, null],
      history:    [],
    };
  }

  _saveSongData(data) {
    return this._write(this._songKey(data.title), data);
  }

  /* ── Recordings ──────────────────────────────────────────────── */

  /**
   * saveRecording(title, bpm, slot, name, chart)
   * Saves a recorded chart into one of 3 slots.
   * Overwrites any existing recording in that slot.
   */
  saveRecording(title, bpm, slot, name, chart) {
    if (slot < 0 || slot >= this.MAX_RECORDINGS) return false;
    const data = this.getSongData(title);
    data.bpm   = bpm;
    data.recordings[slot] = {
      slot,
      name:      name || `Recording ${slot + 1}`,
      savedAt:   new Date().toISOString(),
      chart,
      bestScore: 0,
      bestRank:  null,
    };
    return this._saveSongData(data);
  }

  getRecordings(title) {
    return this.getSongData(title).recordings;  // array of 3, nulls for empty slots
  }

  deleteRecording(title, slot) {
    const data = this.getSongData(title);
    data.recordings[slot] = null;
    return this._saveSongData(data);
  }

  renameRecording(title, slot, newName) {
    const data = this.getSongData(title);
    if (data.recordings[slot]) {
      data.recordings[slot].name = newName;
      return this._saveSongData(data);
    }
    return false;
  }

  /* ── Result logging ──────────────────────────────────────────── */

  /**
   * logResult({ title, bpm, score, rank, accuracy, mode, perfect, good, miss, maxCombo })
   * Appends a result to the song's history and updates global stats.
   * Also updates bestScore/bestRank on the recording slot if mode is 'rec-X'.
   */
  logResult({ title, bpm, score, rank, accuracy, mode, perfect, good, miss, maxCombo }) {
    const entry = {
      date:     new Date().toISOString(),
      score, rank, accuracy, mode,
      perfect, good, miss, maxCombo,
    };

    /* Song-level history */
    const data = this.getSongData(title);
    data.bpm   = bpm || data.bpm;
    data.history.unshift(entry);
    if (data.history.length > this.MAX_HISTORY) data.history.length = this.MAX_HISTORY;

    /* Update best score on recording slot */
    if (mode && mode.startsWith('rec-')) {
      const slot = parseInt(mode.split('-')[1]);
      if (data.recordings[slot]) {
        if (score > (data.recordings[slot].bestScore || 0)) {
          data.recordings[slot].bestScore = score;
          data.recordings[slot].bestRank  = rank;
        }
      }
    }

    this._saveSongData(data);

    /* Global stats */
    this._updateGlobal(title, score, rank);
  }

  _updateGlobal(title, score, rank) {
    const g = this._read(this._globalKey()) || {
      totalPlays: 0, totalScore: 0, bestRanks: {}, dailyLog: {},
    };

    g.totalPlays++;
    g.totalScore += score;

    const hash = this._hash(title);
    const RANK_ORDER = ['D','C','B','A','S','SS'];
    const prev = g.bestRanks[hash];
    if (!prev || RANK_ORDER.indexOf(rank) > RANK_ORDER.indexOf(prev)) {
      g.bestRanks[hash] = rank;
    }

    const today = new Date().toISOString().slice(0, 10);
    g.dailyLog[today] = g.dailyLog[today] || { plays: 0, score: 0 };
    g.dailyLog[today].plays++;
    g.dailyLog[today].score += score;

    /* Keep only last 30 days */
    const keys = Object.keys(g.dailyLog).sort();
    while (keys.length > 30) delete g.dailyLog[keys.shift()];

    this._write(this._globalKey(), g);
  }

  /* ── Stats queries ───────────────────────────────────────────── */

  getGlobalStats() {
    return this._read(this._globalKey()) || { totalPlays:0, totalScore:0, bestRanks:{}, dailyLog:{} };
  }

  /** Returns all songs that have any history, sorted by last played */
  getAllSongStats() {
    const results = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (!key.startsWith('nb_song_')) continue;
      try {
        const data = JSON.parse(localStorage.getItem(key));
        if (data && data.history && data.history.length > 0) results.push(data);
      } catch {}
    }
    results.sort((a, b) => {
      const aDate = a.history[0]?.date || '';
      const bDate = b.history[0]?.date || '';
      return bDate.localeCompare(aDate);
    });
    return results;
  }

  /** Rank colour helper used by both stats screen and results screen */
  static rankColor(rank) {
    return { SS:'#00f5ff', S:'#ffe600', A:'#39ff14', B:'#ff00cc', C:'#ff6600', D:'#ff2244' }[rank] || '#fff';
  }

  /** Returns today's date as YYYY-MM-DD */
  static today() { return new Date().toISOString().slice(0, 10); }

  /** Format a date ISO string as "Mar 26" */
  static formatDate(iso) {
    if (!iso) return '';
    const d = new Date(iso);
    return d.toLocaleDateString('en-US', { month:'short', day:'numeric' });
  }
}

window.SaveManager = SaveManager;
window._saves      = new SaveManager();