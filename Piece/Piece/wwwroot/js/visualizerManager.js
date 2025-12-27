class VisualizerManager {
    constructor() {
        this.audioContext = null;
        this.analyser = null;
        this.dataArray = null;
        this.currentAudioElement = null;
        this.isInitialized = false;

        this.miniCanvas = null;
        this.miniCtx = null;
        this.fullCanvas = null;
        this.fullCtx = null;

        this.animationId = null;
        this.currentMode = 'bars';

        this.drawMini = true;
        this.drawFull = false;
    }

    async initialize(audioElementId) {
        if (this.isInitialized) {
            console.log('[VisualizerManager] Already initialized');
            return true;
        }

        try {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 2048;
            this.analyser.smoothingTimeConstant = 0.65;

            const bufferLength = this.analyser.frequencyBinCount;
            this.dataArray = new Uint8Array(bufferLength);

            this.isInitialized = true;
            console.log('✓ VisualizerManager initialized');

            this.startAudioMonitoring(audioElementId);

            this.animate();

            return true;
        } catch (error) {
            console.error('✗ VisualizerManager error:', error);
            return false;
        }
    }

    startAudioMonitoring(audioElementId) {
        setInterval(() => {
            const audioElement = document.getElementById(audioElementId);

            if (audioElement && audioElement !== this.currentAudioElement) {
                try {
                    console.log('[VisualizerManager] Connecting to audio element...');
                    const stream = audioElement.captureStream();
                    const source = this.audioContext.createMediaStreamSource(stream);
                    source.connect(this.analyser);

                    this.currentAudioElement = audioElement;
                    console.log('✓ Connected to audio element');
                } catch (e) {
                    console.error('[VisualizerManager] Connection error:', e);
                }
            }
        }, 500);
    }

    registerMiniCanvas(canvasId) {
        this.miniCanvas = document.getElementById(canvasId);
        if (this.miniCanvas) {
            this.miniCtx = this.miniCanvas.getContext('2d');
            console.log('✓ Mini canvas registered');
        }
    }

    registerFullCanvas(canvasId) {
        this.fullCanvas = document.getElementById(canvasId);
        if (this.fullCanvas) {
            this.fullCanvas.width = window.innerWidth;
            this.fullCanvas.height = window.innerHeight;
            this.fullCtx = this.fullCanvas.getContext('2d');

            // Handle resize
            window.addEventListener('resize', () => {
                if (this.fullCanvas) {
                    this.fullCanvas.width = window.innerWidth;
                    this.fullCanvas.height = window.innerHeight;
                }
            });

            console.log('✓ Full canvas registered');
        }
    }

    setDrawMini(enabled) {
        this.drawMini = enabled;
        console.log(`[VisualizerManager] Draw mini: ${enabled}`);
    }

    setDrawFull(enabled) {
        this.drawFull = enabled;
        console.log(`[VisualizerManager] Draw full: ${enabled}`);
    }

    setMode(mode) {
        this.currentMode = mode;
        console.log(`[VisualizerManager] Mode: ${mode}`);
    }

    animate() {
        this.animationId = requestAnimationFrame(() => this.animate());

        if (!this.analyser || !this.dataArray) return;

        this.analyser.getByteFrequencyData(this.dataArray);

        if (this.drawMini && this.miniCanvas && this.miniCtx) {
            this.drawMiniVisualizer();
        }

        if (this.drawFull && this.fullCanvas && this.fullCtx) {
            this.drawFullVisualizer();
        }
    }

    drawMiniVisualizer() {
        this.miniCtx.clearRect(0, 0, this.miniCanvas.width, this.miniCanvas.height);

        const barCount = 24;
        const barWidth = this.miniCanvas.width / barCount;

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;
            const heightPercent = 0.15 + (value / 255) * 0.85;
            const height = heightPercent * this.miniCanvas.height;

            const x = i * barWidth + 1;
            const y = this.miniCanvas.height - height;

            const t = i / barCount;
            const hue = 240 + (t * 60);
            const saturation = 70 + (t * 30);
            const lightness = 60 + (Math.sin(t * Math.PI * 4) * 20);

            const gradient = this.miniCtx.createLinearGradient(0, y, 0, this.miniCanvas.height);
            const color = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
            gradient.addColorStop(0, color);
            gradient.addColorStop(1, `hsla(${hue}, ${saturation}%, ${lightness}%, 0.6)`);

            this.miniCtx.fillStyle = gradient;
            this.miniCtx.shadowBlur = 12;
            this.miniCtx.shadowColor = color;
            this.miniCtx.fillRect(x, y, barWidth - 2, height);
            this.miniCtx.shadowBlur = 0;
        }
    }

    drawFullVisualizer() {
        this.fullCtx.fillStyle = 'rgba(0, 0, 0, 0.15)';
        this.fullCtx.fillRect(0, 0, this.fullCanvas.width, this.fullCanvas.height);

        switch (this.currentMode) {
            case 'bars':
                this.drawBars();
                break;
            case 'circle':
                this.drawCircle();
                break;
            case 'waveform':
                this.drawWaveform();
                break;
        }
    }

    drawBars() {
        const barCount = 128;
        const barWidth = this.fullCanvas.width / barCount;

        const gradient = this.fullCtx.createLinearGradient(0, 0, this.fullCanvas.width, 0);
        gradient.addColorStop(0, '#0066ff');
        gradient.addColorStop(0.25, '#00aaff');
        gradient.addColorStop(0.5, '#9966ff');
        gradient.addColorStop(0.75, '#ff00ff');
        gradient.addColorStop(1, '#ffff00');

        for (let i = 0; i < barCount; i++) {
            const logIndex = Math.pow(i / barCount, 1.5);
            const dataIndex = Math.floor(logIndex * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const height = (value / 255) * this.fullCanvas.height * 0.8;
            const x = i * barWidth;
            const y = this.fullCanvas.height - height;

            this.fullCtx.fillStyle = gradient;
            this.fullCtx.fillRect(x, y, barWidth - 2, height);
        }
    }

    drawCircle() {
        const centerX = this.fullCanvas.width / 2;
        const centerY = this.fullCanvas.height / 2;
        const minRadius = 100;
        const barCount = 180;

        for (let i = 0; i < barCount; i++) {
            const angle = (i / barCount) * Math.PI * 2;
            const dataIndex = Math.floor((i / barCount) * this.dataArray.length);
            const value = this.dataArray[dataIndex] || 0;

            const barLength = (value / 255) * 300;
            const innerRadius = minRadius;
            const outerRadius = minRadius + barLength;

            const t = i / barCount;
            const hue = 240 + (t * 60);
            const saturation = 70 + (t * 30);
            const lightness = 60;

            this.fullCtx.strokeStyle = `hsl(${hue}, ${saturation}%, ${lightness}%)`;
            this.fullCtx.lineWidth = 3;

            for (let mirror = 0; mirror < 2; mirror++) {
                const mirrorAngle = angle + (mirror * Math.PI);

                const x1 = centerX + Math.cos(mirrorAngle) * innerRadius;
                const y1 = centerY + Math.sin(mirrorAngle) * innerRadius;
                const x2 = centerX + Math.cos(mirrorAngle) * outerRadius;
                const y2 = centerY + Math.sin(mirrorAngle) * outerRadius;

                this.fullCtx.beginPath();
                this.fullCtx.moveTo(x1, y1);
                this.fullCtx.lineTo(x2, y2);
                this.fullCtx.stroke();
            }
        }
    }

    drawWaveform() {
        const centerY = this.fullCanvas.height / 2;

       
        const gradient = this.fullCtx.createLinearGradient(0, 0, this.fullCanvas.width, 0);
        gradient.addColorStop(0, '#0066ff');
        gradient.addColorStop(0.5, '#9966ff');
        gradient.addColorStop(1, '#ff00ff');

        this.fullCtx.strokeStyle = gradient;
        this.fullCtx.lineWidth = 4;
        this.fullCtx.lineCap = 'round';
        this.fullCtx.beginPath();

        const pointCount = Math.floor(this.fullCanvas.width / 5);
        const usableDataLength = Math.floor(this.dataArray.length * 0.8);

        for (let i = 0; i <= pointCount; i++) {
            const dataIndex = Math.floor((i / pointCount) * usableDataLength);
            const value = this.dataArray[dataIndex] || 0;

            const amplitude = (value / 255) * (this.fullCanvas.height * 0.4);

            const x = (i / pointCount) * this.fullCanvas.width;

            const wavePosition = (i / pointCount) * Math.PI * 2;
            const y = centerY + (Math.sin(wavePosition) * amplitude);

            if (i === 0) {
                this.fullCtx.moveTo(x, y);
            } else {
                this.fullCtx.lineTo(x, y);
            }
        }

        this.fullCtx.shadowBlur = 25;
        this.fullCtx.shadowColor = '#9966ff';
        this.fullCtx.stroke();
        this.fullCtx.shadowBlur = 0;
    }
}

window.visualizerManager = new VisualizerManager();