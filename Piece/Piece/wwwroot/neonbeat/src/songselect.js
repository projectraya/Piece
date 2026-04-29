
'use strict';

class SongSelectManager {
  constructor() {
    this.songs       = [];
    this.selectedIdx = -1;
    this._decodeCtx  = null;
    this._activeTab  = 'auto';  // 'auto' | 'recordings'
    this._bindDOM();
  }

  /* ── DOM Binding ─────────────────────────────────────────────── */
  _bindDOM() {
    /* Upload */
    const zone      = document.getElementById('upload-zone');
    const fileInput = document.getElementById('file-input');
    zone.addEventListener('click', () => fileInput.click());
    fileInput.addEventListener('change', (e) => {
      this._handleFiles(Array.from(e.target.files));
      fileInput.value = '';
    });
    zone.addEventListener('dragover',  (e) => { e.preventDefault(); zone.classList.add('drag-over'); });
    zone.addEventListener('dragleave', ()  => zone.classList.remove('drag-over'));
    zone.addEventListener('drop', (e) => {
      e.preventDefault(); zone.classList.remove('drag-over');
      this._handleFiles(Array.from(e.dataTransfer.files).filter(f => f.type.startsWith('audio/')));
    });

    /* BPM */
    document.getElementById('bpm-half').addEventListener('click',   () => this._adjustBPM(0.5));
    document.getElementById('bpm-double').addEventListener('click', () => this._adjustBPM(2));

    /* Offset slider */
    document.getElementById('offset-slider').addEventListener('input', (e) => {
      if (this.selectedIdx < 0) return;
      this.songs[this.selectedIdx].offset = parseInt(e.target.value);
      document.getElementById('offset-val').textContent = e.target.value + 'ms';
    });

    /* Difficulty */
    document.querySelectorAll('.diff-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('.diff-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById('difficulty-select').value = btn.dataset.diff;
      });
    });

    /* Launch */
    document.getElementById('btn-launch-play').addEventListener('click',   () => this._launch(false));
    document.getElementById('btn-launch-bot').addEventListener('click',    () => this._launch(true, true));
    document.getElementById('btn-launch-record').addEventListener('click', () => this._launchRecord());

    /* Mode tabs */
    document.querySelectorAll('.mode-tab').forEach(tab => {
      tab.addEventListener('click', () => {
        this._activeTab = tab.dataset.tab;
        document.querySelectorAll('.mode-tab').forEach(t => t.classList.remove('active'));
        tab.classList.add('active');
        document.getElementById('panel-auto').classList.toggle('hidden', this._activeTab !== 'auto');
        document.getElementById('panel-recordings').classList.toggle('hidden', this._activeTab !== 'recordings');
        if (this._activeTab === 'recordings') this._renderRecSlots();
      });
    });

    /* Back */
    document.getElementById('btn-back-songselect').addEventListener('click', () => {
      window._game.showScreen('menu');
    });

    /* Save slot screen */
    document.getElementById('btn-saveslot-skip').addEventListener('click', () => {
      window._game.showScreen('songselect');
    });
  }

  onShow() { this._renderList(); }

  /* ── File handling ───────────────────────────────────────────── */
  async _handleFiles(files) {
    for (const file of files) {
      if (!file.type.startsWith('audio/')) continue;
      const title = file.name.replace(/\.[^.]+$/, '');
      const entry = { title, file, bpm: null, offset: 0, audioBuffer: null, analysing: true };
      this.songs.push(entry);
      const idx = this.songs.length - 1;
      this._renderList();
      if (this.selectedIdx === -1) this._select(idx);
      try {
        const ab  = await file.arrayBuffer();
        const ctx = this._getDecodeCtx();
        const buf = await ctx.decodeAudioData(ab);
        entry.audioBuffer = buf;
        entry.bpm         = window.detectBPM(buf);
        entry.offset      = window.detectOffset ? window.detectOffset(buf, entry.bpm) : 0;
        entry.analysing   = false;
      } catch(err) {
        console.warn('Decode failed', file.name, err);
        entry.analysing = false; entry.bpm = 128;
      }
      this._renderList();
      if (this.selectedIdx === idx) this._updateFooter();
    }
  }

  _getDecodeCtx() {
    if (!this._decodeCtx || this._decodeCtx.state === 'closed')
      this._decodeCtx = new (window.AudioContext || window.webkitAudioContext)();
    return this._decodeCtx;
  }

  /* ── Selection ───────────────────────────────────────────────── */
  _select(idx) {
    this.selectedIdx = idx;
    this._renderList();
    this._updateFooter();
    if (this._activeTab === 'recordings') this._renderRecSlots();
  }

  _adjustBPM(factor) {
    if (this.selectedIdx < 0) return;
    const song = this.songs[this.selectedIdx];
    if (!song) return;
    song.bpm = Math.round((song.bpm || 128) * factor);
    this._updateFooter(); this._renderList();
  }

  /* ── Render song list ────────────────────────────────────────── */
  _renderList() {
    const list  = document.getElementById('song-list');
    const empty = document.getElementById('song-list-empty');
    empty.style.display = this.songs.length === 0 ? 'block' : 'none';
    list.querySelectorAll('.song-item').forEach(el => el.remove());

    this.songs.forEach((song, i) => {
      const item = document.createElement('div');
      item.className = 'song-item' + (i === this.selectedIdx ? ' selected' : '');
      const bpmText = song.analysing ? '…' : (song.bpm ? `${song.bpm} BPM` : '—');

      /* Check if this song has any saved recordings */
      const recs      = window._saves.getRecordings(song.title);
      const recCount  = recs.filter(Boolean).length;
      const recBadge  = recCount > 0 ? `<span class="song-rec-badge">${recCount} rec</span>` : '';

      item.innerHTML = `
        <span class="song-item-index">${String(i+1).padStart(2,'0')}</span>
        <div class="song-item-info">
          <div class="song-item-name">${this._esc(song.title)}${recBadge}</div>
          <div class="song-item-meta">${song.file ? this._formatSize(song.file.size) : ''}${song.audioBuffer ? ' · ' + this._formatDuration(song.audioBuffer.duration) : ''}</div>
        </div>
        <span class="song-item-bpm ${song.analysing ? 'analysing' : ''}">${bpmText}</span>
        <button class="song-item-remove" data-remove="${i}">✕</button>
      `;
      item.addEventListener('click', (e) => {
        if (e.target.dataset.remove !== undefined) this._removeSong(parseInt(e.target.dataset.remove));
        else this._select(i);
      });
      list.appendChild(item);
    });
  }

  /* ── Footer update ───────────────────────────────────────────── */
  _updateFooter() {
    const footer  = document.getElementById('songselect-footer');
    const bpmVal  = document.getElementById('bpm-display-val');
    const bpmStat = document.getElementById('bpm-status');

    if (this.selectedIdx < 0 || !this.songs.length) { footer.style.display = 'none'; return; }
    footer.style.display = 'flex';
    const song = this.songs[this.selectedIdx];

    if (song.analysing) {
      bpmVal.textContent  = '…'; bpmStat.textContent = 'Analysing audio…';
    } else {
      bpmVal.textContent  = song.bpm ? String(song.bpm) : '—';
      bpmStat.textContent = song.bpm ? `offset: ${song.offset||0}ms  ·  adjust slider if off` : 'Could not detect';
      const slider = document.getElementById('offset-slider');
      const offVal = document.getElementById('offset-val');
      if (slider) slider.value = song.offset || 0;
      if (offVal)  offVal.textContent = (song.offset || 0) + 'ms';
    }
  }

  /* ── Recording slots panel ───────────────────────────────────── */
  _renderRecSlots() {
    const container = document.getElementById('rec-slots');
    if (!container) return;
    container.innerHTML = '';

    if (this.selectedIdx < 0) return;
    const song = this.songs[this.selectedIdx];
    const recs = window._saves.getRecordings(song.title);

    recs.forEach((rec, slot) => {
      const el = document.createElement('div');
      el.className = 'rec-slot ' + (rec ? 'filled' : 'empty');

      if (rec) {
        const rankColor = SaveManager.rankColor(rec.bestRank || '—');
        el.innerHTML = `
          <div class="rec-slot-info">
            <div class="rec-slot-name">${this._esc(rec.name)}</div>
            <div class="rec-slot-meta">${SaveManager.formatDate(rec.savedAt)} · ${rec.chart?.length || 0} notes</div>
            ${rec.bestRank ? `<div class="rec-slot-rank" style="color:${rankColor}">Best: ${rec.bestRank} · ${(rec.bestScore||0).toLocaleString()}</div>` : ''}
          </div>
          <div class="rec-slot-actions">
            <button class="rec-btn play"  data-slot="${slot}">▶</button>
            <button class="rec-btn del"   data-slot="${slot}" data-action="del">✕</button>
          </div>
        `;
      } else {
        el.innerHTML = `
          <div class="rec-slot-empty-label">Slot ${slot + 1} — empty</div>
        `;
      }

      /* Events */
      el.querySelectorAll('.rec-btn.play').forEach(btn => {
        btn.addEventListener('click', () => this._launchRecording(parseInt(btn.dataset.slot)));
      });
      el.querySelectorAll('.rec-btn.del').forEach(btn => {
        btn.addEventListener('click', () => {
          if (confirm(`Delete recording in slot ${parseInt(btn.dataset.slot)+1}?`)) {
            window._saves.deleteRecording(song.title, parseInt(btn.dataset.slot));
            this._renderRecSlots(); this._renderList();
          }
        });
      });

      container.appendChild(el);
    });
  }

  /* ── Launch helpers ──────────────────────────────────────────── */
  _songObj() {
    const song = this.songs[this.selectedIdx];
    return {
      title:       song.title,
      bpm:         song.bpm || 128,
      offset:      song.offset || 0,
      audioBuffer: song.audioBuffer,
      durationMs:  song.audioBuffer ? song.audioBuffer.duration * 1000 : null,
      difficulty:  document.getElementById('difficulty-select')?.value || 'medium',
    };
  }

  _launch(botEnabled, isBot = false) {
    if (this.selectedIdx < 0) return;
    const song = this.songs[this.selectedIdx];
    if (!song || song.analysing) return;
    const diff = document.getElementById('difficulty-select')?.value || 'medium';
    window._game.startGame({ botEnabled, song: { ...this._songObj(), difficulty: diff }, playMode: `auto-${diff}` });
  }

  _launchRecording(slot) {
    if (this.selectedIdx < 0) return;
    const song = this.songs[this.selectedIdx];
    const recs = window._saves.getRecordings(song.title);
    const rec  = recs[slot];
    if (!rec) return;
    window._game.startGame({
      botEnabled: false,
      song: this._songObj(),
      playMode: `rec-${slot}`,
      savedChart: rec.chart,
    });
  }

  _launchRecord() {
    if (this.selectedIdx < 0) return;
    const song = this.songs[this.selectedIdx];
    if (!song || song.analysing) return;
    window._game.startGame({ botEnabled: false, song: this._songObj(), recordMode: true });
  }

  /* ── Save slot screen (called after recording) ───────────────── */
  showSaveSlotScreen(songTitle, bpm, chart) {
    const container = document.getElementById('save-slots');
    if (!container) return;
    container.innerHTML = '';

    const recs = window._saves.getRecordings(songTitle);
    recs.forEach((rec, slot) => {
      const el = document.createElement('div');
      el.className = 'save-slot-item ' + (rec ? 'filled' : 'empty');

      if (rec) {
        el.innerHTML = `
          <div class="save-slot-info">
            <div class="save-slot-name">${this._esc(rec.name)}</div>
            <div class="save-slot-meta">${SaveManager.formatDate(rec.savedAt)} · ${rec.chart?.length||0} notes</div>
          </div>
          <button class="menu-btn secondary" data-slot="${slot}" style="min-width:90px;padding:.45rem 1rem;font-size:.75rem">OVERWRITE</button>
        `;
      } else {
        el.innerHTML = `
          <div class="save-slot-info">
            <div class="save-slot-name" style="color:rgba(255,255,255,.35)">Slot ${slot+1} — empty</div>
          </div>
          <button class="menu-btn" data-slot="${slot}" style="min-width:90px;padding:.45rem 1rem;font-size:.75rem">SAVE HERE</button>
        `;
      }

      el.querySelector('button').addEventListener('click', () => {
        const name = rec ? rec.name : `Recording ${slot+1}`;
        window._saves.saveRecording(songTitle, bpm, slot, name, chart);
        window._game.showScreen('results');
      });

      container.appendChild(el);
    });

    window._game.showScreen('saveslot');
  }

  /* ── Utils ───────────────────────────────────────────────────── */
  _removeSong(idx) {
    this.songs.splice(idx, 1);
    if (this.selectedIdx >= this.songs.length) this.selectedIdx = this.songs.length - 1;
    this._renderList(); this._updateFooter();
  }
  _esc(s) { return (s||'').replace(/[&<>"']/g, c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])); }
  _formatSize(b) { return b < 1048576 ? `${(b/1024).toFixed(0)} KB` : `${(b/1048576).toFixed(1)} MB`; }
  _formatDuration(s) { const m=Math.floor(s/60); return `${m}:${String(Math.floor(s%60)).padStart(2,'0')}`; }
}

window.addEventListener('DOMContentLoaded', () => { window._songSelect = new SongSelectManager(); });