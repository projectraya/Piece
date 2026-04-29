

'use strict';

class StatsManager {
  constructor() {
    this._expanded = new Set();  // set of title hashes that are expanded
  }

  /* Called by game.js whenever the stats screen becomes visible */
  render() {
    const container = document.getElementById('stats-content');
    if (!container) return;

    const saves  = window._saves;
    const global = saves.getGlobalStats();
    const songs  = saves.getAllSongStats();

    container.innerHTML = '';

    /* ── Today's summary ── */
    const today    = SaveManager.today();
    const todayLog = global.dailyLog?.[today] || { plays: 0, score: 0 };

    const summary = document.createElement('div');
    summary.className = 'stats-summary';
    summary.innerHTML = `
      <div class="stats-summary-row">
        <div class="stats-kv">
          <span class="stats-k">TODAY</span>
          <span class="stats-v">${todayLog.plays} play${todayLog.plays !== 1 ? 's' : ''}</span>
        </div>
        <div class="stats-kv">
          <span class="stats-k">TODAY SCORE</span>
          <span class="stats-v">${todayLog.score.toLocaleString()}</span>
        </div>
        <div class="stats-kv">
          <span class="stats-k">ALL TIME</span>
          <span class="stats-v">${global.totalPlays} plays</span>
        </div>
        <div class="stats-kv">
          <span class="stats-k">TOTAL SCORE</span>
          <span class="stats-v">${global.totalScore.toLocaleString()}</span>
        </div>
      </div>
    `;
    container.appendChild(summary);

    /* ── No data state ── */
    if (songs.length === 0) {
      const empty = document.createElement('div');
      empty.className = 'stats-empty';
      empty.textContent = 'No plays recorded yet. Play some songs to see your stats here.';
      container.appendChild(empty);
      return;
    }

    /* ── Daily activity bar chart (last 7 days) ── */
    container.appendChild(this._renderActivityChart(global.dailyLog));

    /* ── Song cards ── */
    const heading = document.createElement('div');
    heading.className = 'stats-section-label';
    heading.textContent = 'PER SONG';
    container.appendChild(heading);

    for (const songData of songs) {
      container.appendChild(this._renderSongCard(songData, global));
    }
  }

  /* ── Activity chart ─────────────────────────────────────────── */
  _renderActivityChart(dailyLog) {
    const section = document.createElement('div');
    section.className = 'stats-activity';

    const label = document.createElement('div');
    label.className = 'stats-section-label';
    label.textContent = 'LAST 7 DAYS';
    section.appendChild(label);

    const bars = document.createElement('div');
    bars.className = 'activity-bars';

    /* Build last 7 days */
    const days = [];
    for (let i = 6; i >= 0; i--) {
      const d = new Date();
      d.setDate(d.getDate() - i);
      const key = d.toISOString().slice(0, 10);
      days.push({ key, plays: dailyLog[key]?.plays || 0, label: d.toLocaleDateString('en-US',{weekday:'short'}) });
    }

    const maxPlays = Math.max(1, ...days.map(d => d.plays));

    for (const day of days) {
      const col = document.createElement('div');
      col.className = 'activity-col';

      /* MATH: bar height = (plays / maxPlays) * 100% — normalised scale */
      const pct = (day.plays / maxPlays) * 100;
      const isToday = day.key === SaveManager.today();

      col.innerHTML = `
        <div class="activity-bar-wrap">
          <div class="activity-bar ${isToday ? 'today' : ''}" style="height:${Math.max(pct, 4)}%"></div>
        </div>
        <div class="activity-day ${isToday ? 'today' : ''}">${day.label}</div>
        <div class="activity-count">${day.plays || ''}</div>
      `;
      bars.appendChild(col);
    }
    section.appendChild(bars);
    return section;
  }

  /* ── Song card ───────────────────────────────────────────────── */
  _renderSongCard(songData, global) {
    const card = document.createElement('div');
    card.className = 'stats-card';

    const hash  = window._saves._hash(songData.title);
    const plays = songData.history.length;

    /* ── Compute best rank per mode group ──────────────────────────
       Groups:
         auto-easy / auto-medium / auto-hard  → labelled by difficulty
         rec-0 / rec-1 / rec-2               → labelled by recording name
       We find the best rank (highest in RANK_ORDER) for each group.  */
    const RANK_ORDER = ['D','C','B','A','S','SS'];
    const bestByMode = {};   // mode-key → { rank, score }

    for (const h of songData.history) {
      const key = h.mode || 'auto-medium';
      const cur = bestByMode[key];
      if (!cur || RANK_ORDER.indexOf(h.rank) > RANK_ORDER.indexOf(cur.rank) ||
         (h.rank === cur.rank && h.score > cur.score)) {
        bestByMode[key] = { rank: h.rank, score: h.score };
      }
    }

    /* Get recording names from saves */
    const recordings = window._saves.getRecordings(songData.title);

    /* Build mode-label → best rank rows */
    const modeKeys = Object.keys(bestByMode).sort();
    const modeBadgesHtml = modeKeys.map(key => {
      const { rank, score } = bestByMode[key];
      const color = SaveManager.rankColor(rank);
      const label = this._modeLabel(key, recordings);
      return `
        <div class="mode-best-row">
          <span class="mode-best-label">${label}</span>
          <span class="mode-best-rank" style="color:${color};border-color:${color}">${rank}</span>
          <span class="mode-best-score">${score.toLocaleString()}</span>
        </div>`;
    }).join('');

    /* Sparkline — all plays, oldest→newest */
    const sparkScores = songData.history.slice(0, 8).reverse().map(h => h.score);

    const lastEntry = songData.history[0];
    const lastMode  = lastEntry ? this._modeLabel(lastEntry.mode, recordings) : '—';
    const lastRank  = lastEntry?.rank || '—';

    card.innerHTML = `
      <div class="stats-card-header">
        <div class="stats-card-title">${this._esc(songData.title)}</div>
        <span class="plays-badge">${plays} play${plays !== 1 ? 's' : ''}</span>
      </div>

      <div class="mode-bests">
        ${modeBadgesHtml || '<span style="font-family:var(--font-mono);font-size:.65rem;color:rgba(255,255,255,.2)">No results yet</span>'}
      </div>

      <div class="stats-card-bottom">
        <div class="stats-card-spark"></div>
        <div class="stats-last-play">
          <span class="stats-k">LAST PLAY</span>
          <span class="stats-last-mode">${lastMode}</span>
          <span class="stats-last-rank" style="color:${SaveManager.rankColor(lastRank)}">${lastRank}</span>
        </div>
      </div>

      <button class="stats-expand-btn">
        ${this._expanded.has(hash) ? '▲ HIDE HISTORY' : '▼ SHOW HISTORY'}
      </button>
      <div class="stats-history ${this._expanded.has(hash) ? '' : 'hidden'}" id="hist-${hash}"></div>
    `;

    card.querySelector('.stats-card-spark').appendChild(this._renderSparkline(sparkScores));

    card.querySelector('.stats-expand-btn').addEventListener('click', () => {
      if (this._expanded.has(hash)) {
        this._expanded.delete(hash);
      } else {
        this._expanded.add(hash);
        this._renderHistory(card.querySelector(`#hist-${hash}`), songData.history.slice(0, 10), recordings);
      }
      this.render();
    });

    if (this._expanded.has(hash)) {
      this._renderHistory(card.querySelector(`#hist-${hash}`), songData.history.slice(0, 10), recordings);
    }

    return card;
  }

  /* ── Sparkline (SVG) ─────────────────────────────────────────── */
  _renderSparkline(scores) {
    const W = 200, H = 32, PAD = 4;
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
    svg.setAttribute('width', W);
    svg.setAttribute('height', H);
    svg.style.overflow = 'visible';

    if (scores.length < 2) {
      const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      text.setAttribute('x', W/2); text.setAttribute('y', H/2);
      text.setAttribute('text-anchor', 'middle');
      text.setAttribute('dominant-baseline', 'central');
      text.setAttribute('fill', 'rgba(255,255,255,0.15)');
      text.setAttribute('font-size', '10');
      text.setAttribute('font-family', 'Share Tech Mono');
      text.textContent = 'play more to see trend';
      svg.appendChild(text);
      return svg;
    }

    const minS = Math.min(...scores);
    const maxS = Math.max(...scores);
    const range = maxS - minS || 1;

    /* MATH: map score to Y — inverted because SVG Y grows downward
       y = PAD + (1 - normalised) * (H - 2*PAD)
       normalised = (score - min) / range  */
    const pts = scores.map((s, i) => {
      const x = PAD + (i / (scores.length - 1)) * (W - 2 * PAD);
      const y = PAD + (1 - (s - minS) / range) * (H - 2 * PAD);
      return { x, y };
    });

    /* Area fill */
    const area = document.createElementNS('http://www.w3.org/2000/svg', 'path');
    const areaD = `M ${pts[0].x} ${H} ` +
      pts.map(p => `L ${p.x} ${p.y}`).join(' ') +
      ` L ${pts[pts.length-1].x} ${H} Z`;
    area.setAttribute('d', areaD);
    area.setAttribute('fill', 'rgba(0,245,255,0.08)');
    svg.appendChild(area);

    /* Line */
    const line = document.createElementNS('http://www.w3.org/2000/svg', 'polyline');
    line.setAttribute('points', pts.map(p => `${p.x},${p.y}`).join(' '));
    line.setAttribute('fill', 'none');
    line.setAttribute('stroke', '#00f5ff');
    line.setAttribute('stroke-width', '1.5');
    line.setAttribute('stroke-linejoin', 'round');
    svg.appendChild(line);

    /* Dots */
    for (const p of pts) {
      const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
      circle.setAttribute('cx', p.x); circle.setAttribute('cy', p.y);
      circle.setAttribute('r', 2.5);
      circle.setAttribute('fill', '#00f5ff');
      svg.appendChild(circle);
    }

    return svg;
  }

  /* ── History rows ────────────────────────────────────────────── */
  _renderHistory(el, history, recordings = []) {
    if (!el) return;
    el.innerHTML = '';
    for (const h of history) {
      const row = document.createElement('div');
      row.className = 'hist-row';
      const modeLabel = this._modeLabel(h.mode, recordings);
      const color = SaveManager.rankColor(h.rank);
      row.innerHTML = `
        <span class="hist-date">${SaveManager.formatDate(h.date)}</span>
        <span class="hist-mode">${modeLabel}</span>
        <span class="hist-rank" style="color:${color};border:1px solid ${color};padding:0 4px;border-radius:2px">${h.rank}</span>
        <span class="hist-score">${h.score.toLocaleString()}</span>
        <span class="hist-acc">${h.accuracy}%</span>
      `;
      el.appendChild(row);
    }
    el.classList.remove('hidden');
  }

  _modeLabel(mode, recordings = []) {
    if (!mode) return 'AUTO · MEDIUM';
    if (mode === 'auto-easy')   return 'AUTO · EASY';
    if (mode === 'auto-medium') return 'AUTO · MEDIUM';
    if (mode === 'auto-hard')   return 'AUTO · HARD';
    if (mode.startsWith('rec-')) {
      const slot = parseInt(mode.split('-')[1]);
      const name = recordings[slot]?.name || `Recording ${slot + 1}`;
      return `REC ${slot + 1} · ${name}`;
    }
    return mode.toUpperCase();
  }

  _esc(str) {
    return (str||'').replace(/[&<>"']/g, c =>
      ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c])
    );
  }
}

window.StatsManager = StatsManager;
window._stats = new StatsManager();